using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Desenho de UMA lane. Não guarda estado de jogo: lê o LaneSim todo frame e
    /// espelha na tela. A simulação é a fonte da verdade única — assim a lane do
    /// adversário usa exatamente o mesmo código de desenho da sua, e no
    /// multiplayer ela vira só um LaneSim alimentado pela rede.
    ///
    /// Tudo vive sob um pai deslocado e usa coordenadas LOCAIS do grid, para as
    /// duas lanes não se desenharem uma em cima da outra.
    ///
    /// Inimigo e tiro são desenhados POR SLOT da simulação, não por ordem de chegada:
    /// o mesmo boneco segue o mesmo inimigo do nascimento à morte, então a perna anda
    /// pelo chão percorrido e o corpo vira para onde de fato caminha.
    /// </summary>
    public class LaneView
    {
        readonly LaneSim _sim;
        readonly Transform _root;
        readonly Transform _enemyRoot;
        readonly Transform _towerRoot;
        readonly Transform _projectileRoot;
        readonly TerritoryRenderer _territory;
        readonly Color _owner;    // quem defende esta lane
        readonly Color _attacker; // quem manda os inimigos para cá
        readonly bool _isPlayer;
        Camera _cam;

        // inimigos: um rig por slot, com pool por tipo (cada tipo é um modelo diferente)
        ModelRig[] _enemyBySlot = new ModelRig[0];
        int[] _enemyGen = new int[0];
        readonly Dictionary<ModelDef, Stack<ModelRig>> _pool = new Dictionary<ModelDef, Stack<ModelRig>>();

        // tiros: idem, por tipo de torre (bala, bomba, estilhaço de gelo, virote)
        ModelRig[] _shotBySlot = new ModelRig[0];

        readonly List<ModelRig> _towers = new List<ModelRig>(32);
        ModelRig _keep;
        Transform _keepFlag;
        int _drawnTowers;
        int _lastTerritoryStamp = -1;

        public Transform Root => _root;
        public LaneSim Sim => _sim;

        /// <param name="realisticGround">
        /// Há terreno de verdade por baixo (GroundBuilder): a grade vira linhas de giz dele.
        /// </param>
        public LaneView(LaneSim sim, Vector3 offset, string name, Color owner, Color attacker, bool isPlayer,
            bool realisticGround = false)
        {
            _sim = sim;
            _owner = owner;
            _attacker = attacker;
            _isPlayer = isPlayer;
            _root = new GameObject(name).transform;
            _root.position = offset;

            BuildGround(realisticGround);
            _enemyRoot = new GameObject("Inimigos").transform;
            _enemyRoot.SetParent(_root, false);
            _towerRoot = new GameObject("Torres").transform;
            _towerRoot.SetParent(_root, false);
            _projectileRoot = new GameObject("Projeteis").transform;
            _projectileRoot.SetParent(_root, false);
            _territory = new TerritoryRenderer(owner, _root);

            // existe uma vista: vale pagar a busca de alvo por tique para o cano acompanhar
            _sim.TrackAim = true;
            _sim.EnemyDespawned += OnEnemyDespawned;
            _sim.TowerChanged += OnTowerChanged;
            _sim.TowerFired += OnTowerFired;
        }

        /// <summary>Solta os eventos. Sem isso, uma lane descartada continuaria desenhando.</summary>
        public void Dispose()
        {
            _sim.EnemyDespawned -= OnEnemyDespawned;
            _sim.TowerChanged -= OnTowerChanged;
            _sim.TowerFired -= OnTowerFired;
        }

        void OnTowerFired(Vector3 localMuzzle)
        {
            // o evento traz a boca "da simulação"; o clarão sai da boca do cano de verdade
            int idx = _sim.TowerIndexAt(_sim.Map.WorldToCell(localMuzzle));
            if (idx >= 0 && idx < _towers.Count)
            {
                _towers[idx].Kick();
                Vfx.Instance?.Muzzle(_towers[idx].MuzzleWorld);
                return;
            }
            Vfx.Instance?.Muzzle(_root.TransformPoint(localMuzzle));
        }

        void OnEnemyDespawned(Vector3 localPos, DespawnReason reason)
        {
            var world = _root.TransformPoint(localPos);
            if (reason == DespawnReason.Leaked)
            {
                Vfx.Instance?.Leak(world);
                // só treme a tela quando o vazamento é SEU: doeu em você, não no outro
                if (_isPlayer) Juice.Shake(0.55f);
                FloatingText.Instance?.Show(world + Vector3.up, "-1", Palette.TextDanger);
                return;
            }
            Vfx.Instance?.KillBurst(world + Vector3.up * 0.25f, reason == DespawnReason.KilledByAttrition);
        }

        void OnTowerChanged(Vector3 localPos, int level)
        {
            var world = _root.TransformPoint(localPos);
            Vfx.Instance?.Build(world);
            if (_isPlayer) Juice.Shake(0.10f);
            if (level > 1)
                FloatingText.Instance?.Show(world + Vector3.up * 1.6f, $"nível {level}", Palette.TextGold);
        }

        void BuildGround(bool realisticGround)
        {
            // o chão é o terreno do mundo; a lane só desenha o que é dela
            if (realisticGround) GridOverlay.Build(_sim.Map, _root);
            else Overlays.Grid(_sim.Map, _root);

            // fortaleza de quem defende, com o portão virado para o acampamento inimigo
            _keep = ArtFactory.Spawn(ModelLib.Keep(), _owner, _root, "Base");
            _keep.transform.localPosition = _sim.Map.CellToWorld(_sim.GoalCell);
            _keep.transform.localRotation = Quaternion.LookRotation(DirToSpawn());
            _keepFlag = _keep.transform.Find(ModelLib.Body + "/" + ModelLib.Flag);

            foreach (var s in _sim.SpawnCells)
            {
                var camp = ArtFactory.Spawn(ModelLib.Camp(), _attacker, _root, "Spawn");
                camp.transform.localPosition = _sim.Map.CellToWorld(s);
                camp.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // portal atravessado no sentido da marcha
            }
        }

        Vector3 DirToSpawn()
        {
            if (_sim.SpawnCells.Count == 0) return Vector3.back;
            var d = _sim.Map.CellToWorld(_sim.SpawnCells[0]) - _sim.Map.CellToWorld(_sim.GoalCell);
            d.y = 0f;
            return d.sqrMagnitude > 0.001f ? d.normalized : Vector3.back;
        }

        /// <summary>Espelha o estado da simulação. Chamar uma vez por frame, depois do Tick.</summary>
        public void Sync()
        {
            if (_cam == null) _cam = Camera.main;
            float dt = Time.deltaTime;
            SyncTowers(dt);
            SyncEnemies(dt);
            SyncProjectiles();
            AnimateKeep();
        }

        void AnimateKeep()
        {
            // estandarte ao vento: balanço lento, nunca parado
            if (_keepFlag != null) _keepFlag.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 1.3f) * 12f, 0f);
        }

        // ------------------------------------------------------------------ torres

        void SyncTowers(float dt)
        {
            // torres só nascem, nunca somem: basta criar as que faltam
            for (int i = _drawnTowers; i < _sim.TowerCount; i++)
            {
                int type = _sim.TowerTypeId(i);
                var rig = ArtFactory.Spawn(ModelLib.Tower(type), _owner, _towerRoot,
                    $"Torre_{TowerCatalog.Get(type).Name}");
                rig.transform.localPosition = _sim.Map.CellToWorld(_sim.TowerCell(i));
                // torreta nasce virada para o acampamento: é de lá que o inimigo vem
                rig.AimAt(_root.TransformPoint(_sim.Map.CellToWorld(_sim.SpawnCells.Count > 0 ? _sim.SpawnCells[0] : _sim.GoalCell)), 1f, 1f);
                _towers.Add(rig);
            }
            _drawnTowers = _sim.TowerCount;

            for (int i = 0; i < _towers.Count; i++)
            {
                var rig = _towers[i];
                // o nível se lê na própria torre: ela cresce e ganha estandarte
                rig.SetLevel(_sim.TowerLevel(i));
                rig.TickTower(dt);
                // canhão acompanha o alvo. Sem isto todos apontam para o mesmo lado para
                // sempre, e a torre parece desligada mesmo enquanto mata.
                if (_sim.TryGetTowerAim(i, out var aim)) rig.AimAt(_root.TransformPoint(aim), dt);
            }

            // o território muda junto com as torres; reconstruir só quando isso acontece
            if (_lastTerritoryStamp != _sim.TowerCount)
            {
                _lastTerritoryStamp = _sim.TowerCount;
                _territory.Rebuild(_sim.Territory, _sim.Map);
            }
        }

        // ---------------------------------------------------------------- inimigos

        void SyncEnemies(float dt)
        {
            int slots = _sim.EnemySlotCount;
            if (_enemyBySlot.Length != slots)
            {
                System.Array.Resize(ref _enemyBySlot, slots);
                System.Array.Resize(ref _enemyGen, slots);
            }

            for (int s = 0; s < slots; s++)
            {
                if (!_sim.TryGetEnemy(s, out var e))
                {
                    ReleaseEnemy(s);
                    continue;
                }

                // slot reciclado (outro inimigo, talvez outro tipo): boneco novo do tipo certo
                var rig = _enemyBySlot[s];
                bool fresh = rig == null || _enemyGen[s] != e.Generation || rig.Def != ModelLib.Enemy(e.TypeId);
                if (fresh)
                {
                    ReleaseEnemy(s);
                    rig = _enemyBySlot[s] = RentEnemy(e.TypeId);
                    _enemyGen[s] = e.Generation;
                    // nasce olhando para a base: sem isto o primeiro passo gira o corpo no lugar
                    rig.transform.localRotation = Quaternion.LookRotation(-DirToSpawn());
                }

                rig.Follow(new Vector3(e.Pos.x, 0f, e.Pos.z), dt, snap: fresh);

                // dano se lê na barra; atrito, no brilho gelado que pulsa sobre a armadura
                float frac = e.MaxHp > 0f ? e.Hp / e.MaxHp : 1f;
                rig.SetHealth(frac, _cam);
                bool drained = e.AttritionScale > 0f && _sim.Territory.Contains(e.Pos);
                rig.SetGlow(drained
                    ? Palette.AttritionGlow * (0.75f + 0.25f * Mathf.Sin(Time.time * 6f + s))
                    : Color.black);
            }
        }

        ModelRig RentEnemy(int type) =>
            Rent(ModelLib.Enemy(type)) ?? ArtFactory.Spawn(ModelLib.Enemy(type), _attacker, _enemyRoot,
                $"Inimigo_{SendCatalog.Get(type).Name}");

        void ReleaseEnemy(int slot)
        {
            Return(_enemyBySlot[slot]);
            _enemyBySlot[slot] = null;
        }

        // pool por MODELO: um catálogo carregado de arquivo pode ter mais tipos que
        // modelos, e tipos sem modelo próprio dividem o boneco padrão sem confusão
        ModelRig Rent(ModelDef def)
        {
            if (!_pool.TryGetValue(def, out var stack) || stack.Count == 0) return null;
            var r = stack.Pop();
            r.gameObject.SetActive(true);
            return r;
        }

        void Return(ModelRig rig)
        {
            if (rig == null) return;
            rig.ResetState();
            rig.gameObject.SetActive(false);
            if (!_pool.TryGetValue(rig.Def, out var stack)) _pool[rig.Def] = stack = new Stack<ModelRig>();
            stack.Push(rig);
        }

        // ------------------------------------------------------------------ tiros

        void SyncProjectiles()
        {
            int slots = _sim.ProjectileSlotCount;
            if (_shotBySlot.Length != slots) System.Array.Resize(ref _shotBySlot, slots);

            for (int s = 0; s < slots; s++)
            {
                if (!_sim.TryGetProjectile(s, out var p, out float t, out int towerType, out bool flies))
                {
                    ReleaseShot(s);
                    continue;
                }

                var rig = _shotBySlot[s];
                bool fresh = rig == null || rig.Def != ModelLib.Projectile(towerType);
                if (fresh)
                {
                    ReleaseShot(s);
                    rig = _shotBySlot[s] = RentShot(towerType);
                }

                // a simulação voa em linha reta da altura 0,95 ao pé do alvo; a vista sai
                // da boca do cano de verdade e chega no peito (ou no planador lá em cima)
                float muzzleY = ModelLib.Tower(towerType).Muzzle.y;
                float targetY = flies ? 1.1f : 0.28f;
                float y = Mathf.Lerp(muzzleY, targetY, t);
                // bomba de morteiro sobe em arco — é o que faz o morteiro parecer morteiro
                if (towerType == 1) y += 4f * t * (1f - t) * 1.3f;
                var pos = new Vector3(p.x, y, p.z);

                // virote e estilhaço apontam para onde voam; no primeiro frame não há
                // "de onde veio" (a posição antiga é de outro tiro, do pool)
                var d = pos - rig.transform.localPosition;
                rig.transform.localPosition = pos;
                if (!fresh && d.sqrMagnitude > 1e-6f) rig.transform.localRotation = Quaternion.LookRotation(d);
            }
        }

        ModelRig RentShot(int towerType)
        {
            var pooled = Rent(ModelLib.Projectile(towerType));
            if (pooled != null) return pooled;
            var rig = ArtFactory.Spawn(ModelLib.Projectile(towerType), _owner, _projectileRoot, "Projetil");
            foreach (var mr in rig.GetComponentsInChildren<MeshRenderer>())
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return rig;
        }

        void ReleaseShot(int slot)
        {
            Return(_shotBySlot[slot]);
            _shotBySlot[slot] = null;
        }

        /// <summary>Converte ponto do mundo para célula desta lane (desfaz o deslocamento do pai).</summary>
        public Vector2Int WorldToCell(Vector3 world) => _sim.Map.WorldToCell(_root.InverseTransformPoint(world));

        public Vector3 CellToWorld(Vector2Int cell) => _root.TransformPoint(_sim.Map.CellToWorld(cell));
    }
}
