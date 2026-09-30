using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    public enum WavePhase
    {
        Building,  // intervalo entre ondas — hora de construir
        Spawning,  // onda entrando no mapa
        Fighting,  // todos spawnaram; esperando limpar
        GameOver
    }

    /// <summary>
    /// Cérebro do jogo. Constrói o mundo inteiro em código (sem prefabs, sem cena montada à mão)
    /// e dirige explicitamente os sistemas por frame — ordem de update determinística.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        public GridMap Map { get; private set; }
        public FlowField Flow { get; private set; }
        public TerritoryField Territory { get; private set; }
        public PlayerState State { get; private set; }

        public int Wave { get; private set; }
        public WavePhase Phase { get; private set; }
        public float PhaseTimer { get; private set; }
        public int EnemiesAlive => Enemy.Alive.Count;

        Vector2Int _goalCell;
        Vector3 _goalWorld;
        readonly List<Vector2Int> _spawnCells = new List<Vector2Int>();
        readonly List<GameObject> _towers = new List<GameObject>();
        readonly List<Vector2Int> _towerCells = new List<Vector2Int>();

        IGameInput _input;
        CameraRigDriver _cameraRig;
        TowerPlacer _placer;
        TerritoryRenderer _territoryRenderer;
        FloatingText _floatingText;
        Transform _baseFlag;

        SimplePool<Enemy> _enemyPool;
        SimplePool<Projectile> _projectilePool;
        System.Action<Projectile> _releaseProjectile;
        System.Action<Enemy, DespawnReason> _onEnemyDespawn;

        int _toSpawn;
        float _spawnTimer;

        void Start()
        {
            Map = new GridMap(GameConfig.GridWidth, GameConfig.GridHeight, GameConfig.CellSize);
            Flow = new FlowField(Map);
            Territory = new TerritoryField(Map);
            State = new PlayerState(GameConfig.StartLives, GameConfig.StartGold);

            _goalCell = new Vector2Int(Map.Width - 3, Map.Height / 2);
            _spawnCells.Add(new Vector2Int(2, Map.Height / 2));
            _goalWorld = Map.CellToWorld(_goalCell);
            Flow.Rebuild(_goalCell);

            _enemyPool = new SimplePool<Enemy>(CreateEnemy);
            _projectilePool = new SimplePool<Projectile>(CreateProjectile);
            _releaseProjectile = _projectilePool.Release; // delegates cacheados: nada de alocar por tiro
            _onEnemyDespawn = OnEnemyDespawn;

            // texturas procedurais geradas de uma vez, em paralelo, antes de qualquer modelo
            ArtFactory.Preload();
            var cam = FindOrCreateCamera();
            SceneAmbience.Apply(cam);
            BuildWorld();
            new Vfx();

            _input = new DesktopInput();
            _cameraRig = new CameraRigDriver(cam, Vector3.zero, Map.WorldSize);
            _placer = new TowerPlacer(this);
            _territoryRenderer = new TerritoryRenderer(Palette.TeamPlayer);
            gameObject.AddComponent<DebugHud>().Init(this);
            _floatingText = gameObject.AddComponent<FloatingText>();
            _floatingText.Init(cam);

            Phase = WavePhase.Building;
            PhaseTimer = GameConfig.FirstWaveDelay;
        }

        void Update()
        {
            _input.Tick();
            float dt = Time.deltaTime;

            // Câmera e shake em tempo NÃO-escalado: o fim de jogo congela a simulação
            // com timeScale = 0, e com dt escalado o tremor ficaria travado na tela e a
            // câmera pararia de responder justamente quando dá vontade de olhar o mapa.
            float uiDt = Time.unscaledDeltaTime;
            Juice.Tick(uiDt);
            _cameraRig.Tick(_input, uiDt);
            WaveBanner();

            if (_input.RestartPressed)
            {
                ResetGame();
                return;
            }
            if (Phase == WavePhase.GameOver)
            {
                _placer.Hide();
                return;
            }

            _placer.Tick(_input, _cameraRig.Camera);

            switch (Phase)
            {
                case WavePhase.Building:
                    PhaseTimer -= dt;
                    if (PhaseTimer <= 0f || _input.CallWavePressed)
                        StartWave();
                    break;

                case WavePhase.Spawning:
                    _spawnTimer -= dt;
                    if (_spawnTimer <= 0f)
                    {
                        SpawnEnemy();
                        _spawnTimer = GameConfig.SpawnInterval;
                        if (--_toSpawn <= 0)
                            Phase = WavePhase.Fighting;
                    }
                    break;

                case WavePhase.Fighting:
                    if (Enemy.Alive.Count == 0)
                    {
                        State.AddGold(GameConfig.WaveClearBonus);
                        _floatingText.Show(_goalWorld + Vector3.up * 1.5f,
                            $"Onda {Wave} limpa!  +{GameConfig.WaveClearBonus}", Palette.TextGold);
                        Phase = WavePhase.Building;
                        PhaseTimer = GameConfig.TimeBetweenWaves;
                    }
                    break;
            }
        }

        void StartWave()
        {
            Wave++;
            _toSpawn = GameConfig.EnemiesInWave(Wave);
            _spawnTimer = 0f;
            Phase = WavePhase.Spawning;
        }

        void SpawnEnemy()
        {
            var e = _enemyPool.Get();
            var spawn = _spawnCells[Random.Range(0, _spawnCells.Count)];
            var pos = Map.CellToWorld(spawn)
                      + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            pos.y = 0f;
            e.transform.position = pos;
            // nasce olhando para a base: sem isto o primeiro passo gira o corpo no lugar
            var toGoal = _goalWorld - pos;
            toGoal.y = 0f;
            if (toGoal.sqrMagnitude > 0.01f) e.transform.rotation = Quaternion.LookRotation(toGoal);

            float hp = GameConfig.EnemyBaseHp * Mathf.Pow(GameConfig.EnemyHpGrowth, Wave - 1);
            float speed = Mathf.Min(GameConfig.EnemyBaseSpeed + GameConfig.EnemySpeedPerWave * Wave, GameConfig.EnemyMaxSpeed)
                          * Random.Range(0.92f, 1.08f); // variação leve pra não andarem em fila indiana
            e.Init(Flow, Territory, _goalWorld, hp, speed, _onEnemyDespawn);
        }

        /// <summary>
        /// Congela a partida. Enemy e Tower são MonoBehaviours com Update próprio: sem
        /// isto, a onda continuava andando por baixo do "FIM DE JOGO" — a tela tremia a
        /// cada vazamento, chovia "-1 vida" com o contador já em zero, e o ouro subia.
        /// </summary>
        void EndGame()
        {
            Phase = WavePhase.GameOver;
            Time.timeScale = 0f;
        }

        // Time.timeScale é estado GLOBAL: se este objeto morrer congelado, leva o
        // próximo modo junto. Sempre devolver ao sair.
        void OnDisable() => Time.timeScale = 1f;

        void OnEnemyDespawn(Enemy e, DespawnReason reason)
        {
            var pos = e.transform.position;
            _enemyPool.Release(e);

            if (reason == DespawnReason.Leaked)
            {
                State.LoseLife();
                Vfx.Instance?.Leak(pos);
                Juice.Shake(0.55f); // só aqui: o evento que dói merece tremer a tela
                _floatingText.Show(pos + Vector3.up * 0.8f, "-1 vida", Palette.TextDanger);
                if (State.GameOver) EndGame();
                return;
            }

            bool byAttrition = reason == DespawnReason.KilledByAttrition;
            State.AddGold(GameConfig.KillReward);
            Vfx.Instance?.KillBurst(pos + Vector3.up * 0.25f, byAttrition);
            // cor do número diz o que matou: fronteira (azul-gelo) ou torre (dourado)
            _floatingText.Show(pos + Vector3.up * 0.8f, $"+{GameConfig.KillReward}",
                byAttrition ? Palette.TextFrost : Palette.TextGold);
        }

        public bool CanPlaceTower(Vector2Int cell)
        {
            if (Phase == WavePhase.GameOver) return false;
            if (!Map.InBounds(cell.x, cell.y) || Map.IsBlocked(cell)) return false;
            if (cell == _goalCell) return false;
            for (int i = 0; i < _spawnCells.Count; i++)
                if (cell == _spawnCells[i]) return false;
            if (State.Gold < GameConfig.TowerCost) return false;

            // não construir em cima de inimigo
            var center = Map.CellToWorld(cell);
            for (int i = 0; i < Enemy.Alive.Count; i++)
            {
                var d = Enemy.Alive[i].transform.position - center;
                d.y = 0f;
                if (d.sqrMagnitude < 0.8f * 0.8f) return false;
            }

            // nunca deixar o jogador murar o caminho por completo
            if (Flow.PlacementBlocksPath(cell, _spawnCells)) return false;
            return true;
        }

        public void PlaceTower(Vector2Int cell)
        {
            if (!CanPlaceTower(cell) || !State.TrySpend(GameConfig.TowerCost)) return;
            Map.SetBlocked(cell, true);
            Flow.Rebuild(_goalCell); // todos os inimigos redirecionam na hora
            _towers.Add(CreateTowerVisual(cell));

            // a fronteira empurra: cada torre nova expande o território
            _towerCells.Add(cell);
            Territory.Rebuild(_towerCells, GameConfig.BorderRadius);
            _territoryRenderer.Rebuild(Territory, Map);

            Vfx.Instance?.Build(Map.CellToWorld(cell));
            Juice.Shake(0.12f);
        }

        /// <summary>Quanto a torre da célula devolve se vendida, ou -1 se não há torre.</summary>
        public int SellValueAt(Vector2Int cell) =>
            Phase != WavePhase.GameOver && _towerCells.Contains(cell)
                ? (int)(GameConfig.TowerCost * TowerWarsConfig.SellRefund) : -1;

        /// <summary>
        /// Vende a torre da célula: devolve parte do ouro, libera o caminho (os inimigos
        /// pegam o atalho na hora) e encolhe a fronteira.
        /// </summary>
        public void SellTower(Vector2Int cell)
        {
            int refund = SellValueAt(cell);
            int i = _towerCells.IndexOf(cell);
            if (refund < 0 || i < 0) return;

            Destroy(_towers[i]);
            _towers.RemoveAt(i);
            _towerCells.RemoveAt(i);
            Map.SetBlocked(cell, false);
            Flow.Rebuild(_goalCell);
            Territory.Rebuild(_towerCells, GameConfig.BorderRadius);
            _territoryRenderer.Rebuild(Territory, Map);
            State.AddGold(refund);

            var pos = Map.CellToWorld(cell);
            Vfx.Instance?.Build(pos);
            _floatingText.Show(pos + Vector3.up * 1.2f, $"+{refund}", Palette.TextGold);
            Juice.Shake(0.08f);
        }

        public void ResetGame()
        {
            for (int i = Enemy.Alive.Count - 1; i >= 0; i--)
                _enemyPool.Release(Enemy.Alive[i]);
            foreach (var t in _towers)
                Destroy(t);
            _towers.Clear();

            Map.ClearAllBlocked();
            Flow.Rebuild(_goalCell);
            _towerCells.Clear();
            Territory.Rebuild(_towerCells, GameConfig.BorderRadius);
            _territoryRenderer.Rebuild(Territory, Map);
            State = new PlayerState(GameConfig.StartLives, GameConfig.StartGold);
            Juice.Reset();
            _floatingText.Clear();
            Wave = 0;
            Phase = WavePhase.Building;
            PhaseTimer = GameConfig.FirstWaveDelay;
            Time.timeScale = 1f; // sai do congelamento do fim de jogo
        }

        // estandarte da fortaleza ao vento: mostra que a base está viva sem custar nada
        void WaveBanner()
        {
            if (_baseFlag == null) return;
            _baseFlag.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.unscaledTime * 1.3f) * 12f, 0f);
        }

        // ---------- construção do mundo (modelos procedurais, tudo em código) ----------

        void BuildWorld()
        {
            // chão de verdade (grama fotográfica + tufos 3D) quando a arte existe;
            // senão, relevo procedural
            var size = Map.WorldSize;
            var area = new[] { new Rect(-size.x * 0.5f, -size.z * 0.5f, size.x, size.z) };
            var terrain = GroundBuilder.Build(area);
            if (terrain != null)
            {
                gameObject.AddComponent<GrassField>().Init(terrain, area);
                GridOverlay.Build(Map, transform);
            }
            else Overlays.Grid(Map, null);

            var layout = new WorldLayout();
            layout.AddPlayArea(Vector3.zero, size);
            WorldView.Build(layout, terrain);

            // fortaleza com o portão virado para o acampamento de onde o inimigo sai
            var keep = ArtFactory.Spawn(ModelLib.Keep(), Palette.TeamPlayer, null, "Base");
            keep.transform.position = _goalWorld;
            var toSpawn = Map.CellToWorld(_spawnCells[0]) - _goalWorld;
            toSpawn.y = 0f;
            keep.transform.rotation = Quaternion.LookRotation(toSpawn.sqrMagnitude > 0.01f ? toSpawn : Vector3.back);
            _baseFlag = keep.transform.Find(ModelLib.Body + "/" + ModelLib.Flag);

            foreach (var s in _spawnCells)
            {
                var camp = ArtFactory.Spawn(ModelLib.Camp(), Palette.TeamFoe, null, "Spawn");
                camp.transform.position = Map.CellToWorld(s);
                camp.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // portal atravessado no sentido da marcha
            }
        }

        Enemy CreateEnemy()
        {
            // o modo clássico tem um inimigo só: o lanceiro, nas cores do atacante
            var rig = ArtFactory.Spawn(ModelLib.Enemy(0), Palette.TeamFoe, null, "Inimigo");
            return rig.gameObject.AddComponent<Enemy>();
        }

        Projectile CreateProjectile()
        {
            var rig = ArtFactory.Spawn(ModelLib.Projectile(0), Palette.TeamPlayer, null, "Projetil");
            foreach (var mr in rig.GetComponentsInChildren<MeshRenderer>())
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return rig.gameObject.AddComponent<Projectile>();
        }

        GameObject CreateTowerVisual(Vector2Int cell)
        {
            var rig = ArtFactory.Spawn(ModelLib.Tower(0), Palette.TeamPlayer, null, "Torre");
            rig.transform.position = Map.CellToWorld(cell);
            // canhão nasce virado para o acampamento: é de lá que o inimigo vem
            rig.AimAt(Map.CellToWorld(_spawnCells[0]), 1f, 1f);
            rig.gameObject.AddComponent<Tower>().Init(rig, _projectilePool, _releaseProjectile);
            return rig.gameObject;
        }

        Camera FindOrCreateCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            return cam;
        }
    }
}
