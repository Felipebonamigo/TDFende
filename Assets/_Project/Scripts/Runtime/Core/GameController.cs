using System.Collections.Generic;
using UnityEngine;

namespace FrontierTD
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
        public PlayerState State { get; private set; }

        public int Wave { get; private set; }
        public WavePhase Phase { get; private set; }
        public float PhaseTimer { get; private set; }
        public int EnemiesAlive => Enemy.Alive.Count;

        Vector2Int _goalCell;
        Vector3 _goalWorld;
        readonly List<Vector2Int> _spawnCells = new List<Vector2Int>();
        readonly List<GameObject> _towers = new List<GameObject>();

        IGameInput _input;
        CameraRigDriver _cameraRig;
        TowerPlacer _placer;

        SimplePool<Enemy> _enemyPool;
        SimplePool<Projectile> _projectilePool;
        System.Action<Projectile> _releaseProjectile;
        System.Action<Enemy, bool> _onEnemyDespawn;

        int _toSpawn;
        float _spawnTimer;

        void Start()
        {
            Map = new GridMap(GameConfig.GridWidth, GameConfig.GridHeight, GameConfig.CellSize);
            Flow = new FlowField(Map);
            State = new PlayerState(GameConfig.StartLives, GameConfig.StartGold);

            _goalCell = new Vector2Int(Map.Width - 3, Map.Height / 2);
            _spawnCells.Add(new Vector2Int(2, Map.Height / 2));
            _goalWorld = Map.CellToWorld(_goalCell);
            Flow.Rebuild(_goalCell);

            _enemyPool = new SimplePool<Enemy>(CreateEnemy);
            _projectilePool = new SimplePool<Projectile>(CreateProjectile);
            _releaseProjectile = _projectilePool.Release; // delegates cacheados: nada de alocar por tiro
            _onEnemyDespawn = OnEnemyDespawn;

            BuildWorld();

            _input = new DesktopInput();
            _cameraRig = new CameraRigDriver(FindOrCreateCamera(), Vector3.zero, Map.WorldSize);
            _placer = new TowerPlacer(this);
            gameObject.AddComponent<DebugHud>().Init(this);

            Phase = WavePhase.Building;
            PhaseTimer = GameConfig.FirstWaveDelay;
        }

        void Update()
        {
            _input.Tick();
            float dt = Time.deltaTime;
            _cameraRig.Tick(_input, dt);

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
            pos.y = 0.5f;
            e.transform.position = pos;

            float hp = GameConfig.EnemyBaseHp * Mathf.Pow(GameConfig.EnemyHpGrowth, Wave - 1);
            float speed = Mathf.Min(GameConfig.EnemyBaseSpeed + GameConfig.EnemySpeedPerWave * Wave, GameConfig.EnemyMaxSpeed)
                          * Random.Range(0.92f, 1.08f); // variação leve pra não andarem em fila indiana
            e.Init(Flow, _goalWorld, hp, speed, _onEnemyDespawn);
        }

        void OnEnemyDespawn(Enemy e, bool killed)
        {
            _enemyPool.Release(e);
            if (killed)
            {
                State.AddGold(GameConfig.KillReward);
            }
            else
            {
                State.LoseLife();
                if (State.GameOver)
                    Phase = WavePhase.GameOver;
            }
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
            State = new PlayerState(GameConfig.StartLives, GameConfig.StartGold);
            Wave = 0;
            Phase = WavePhase.Building;
            PhaseTimer = GameConfig.FirstWaveDelay;
        }

        // ---------- construção do mundo (tudo primitivas, tudo em código) ----------

        void BuildWorld()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Chao";
            Destroy(ground.GetComponent<Collider>());
            var size = Map.WorldSize;
            ground.transform.localScale = new Vector3(size.x, 0.1f, size.z);
            ground.transform.position = new Vector3(0f, -0.05f, 0f); // topo do cubo em Y=0
            ground.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGround(
                new Color(0.22f, 0.30f, 0.22f), new Color(0.19f, 0.26f, 0.19f), Map.Width, Map.Height);

            var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseGo.name = "Base";
            Destroy(baseGo.GetComponent<Collider>());
            baseGo.transform.position = _goalWorld + Vector3.up * 0.6f;
            baseGo.transform.localScale = new Vector3(1.1f, 1.2f, 1.1f);
            baseGo.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(new Color(0.95f, 0.75f, 0.15f));

            foreach (var s in _spawnCells)
            {
                var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
                m.name = "Spawn";
                Destroy(m.GetComponent<Collider>());
                m.transform.position = Map.CellToWorld(s) + Vector3.up * 0.05f;
                m.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);
                m.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(new Color(0.6f, 0.15f, 0.5f));
            }

            bool hasDirLight = false;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { hasDirLight = true; break; }
            if (!hasDirLight)
            {
                var lightGo = new GameObject("Sol");
                lightGo.AddComponent<Light>().type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            }
        }

        Enemy CreateEnemy()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Inimigo";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(0.55f, 0.5f, 0.55f); // altura 1, centro em y=0.5
            go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(new Color(0.85f, 0.2f, 0.2f));
            return go.AddComponent<Enemy>();
        }

        Projectile CreateProjectile()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projetil";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.22f;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(new Color(1f, 0.9f, 0.3f));
            return go.AddComponent<Projectile>();
        }

        GameObject CreateTowerVisual(Vector2Int cell)
        {
            var root = new GameObject("Torre");
            root.transform.position = Map.CellToWorld(cell);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            body.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            body.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(new Color(0.25f, 0.45f, 0.85f));

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            head.transform.localScale = new Vector3(0.35f, 0.25f, 0.6f);
            head.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(new Color(0.5f, 0.7f, 1f));

            root.AddComponent<Tower>().Init(head.transform, _projectilePool, _releaseProjectile);
            return root;
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
