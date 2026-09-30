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

        // Interpolação: a simulação anda a 30 tiques/s e a tela a centenas de quadros.
        // Desenhar a posição crua fazia tudo andar em degraus (e a perna "parava" nos
        // quadros sem tique). Guardamos o tique anterior e o atual e desenhamos entre eles.
        Vector3[] _enemyPrev = new Vector3[0], _enemyCur = new Vector3[0];
        float[] _burnFx = new float[0];
        Vector3[] _shotPrev = new Vector3[0], _shotCur = new Vector3[0];
        int[] _shotType = new int[0];
        float _lastSimTime = -1f;
        bool _ticked;
        readonly List<float> _brazierFx = new List<float>();

        readonly List<ModelRig> _towers = new List<ModelRig>(32);
        readonly List<float> _towerAge = new List<float>(32); // construção: a torre sobe do chão

        /// <summary>Inimigo que acabou de morrer: tomba, fica um instante e afunda antes de voltar ao pool.</summary>
        struct Dying
        {
            public ModelRig Rig;
            public float T;
            public float Side;      // tomba para a esquerda ou para a direita
            public Quaternion From;
        }
        readonly List<Dying> _dying = new List<Dying>();
        const float FallTime = 0.45f, LieTime = 1.1f, SinkTime = 0.8f, BuildTime = 0.7f;
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
            _sim.TowerSold += OnTowerSold;
        }

        /// <summary>Solta os eventos. Sem isso, uma lane descartada continuaria desenhando.</summary>
        public void Dispose()
        {
            _sim.EnemyDespawned -= OnEnemyDespawned;
            _sim.TowerChanged -= OnTowerChanged;
            _sim.TowerFired -= OnTowerFired;
            _sim.TowerSold -= OnTowerSold;
        }

        /// <summary>
        /// Torre vendida: o rig sai da lista NA MESMA POSIÇÃO que a simulação tirou a torre
        /// (as de depois descem uma casa nos dois lados) e desmorona afundando no chão.
        /// </summary>
        void OnTowerSold(Vector3 localPos, int index, int refund)
        {
            var world = _root.TransformPoint(localPos);
            Vfx.Instance?.Build(world);
            if (_isPlayer)
            {
                Juice.Shake(0.08f);
                FloatingText.Instance?.Show(world + Vector3.up * 1.2f, $"+{refund}", Palette.TextGold);
            }
            // vendida no mesmo quadro em que foi construída: ainda não tinha rig
            if (index >= _drawnTowers) return;

            var rig = _towers[index];
            _towers.RemoveAt(index);
            _towerAge.RemoveAt(index);
            if (index < _brazierFx.Count) _brazierFx.RemoveAt(index);
            _drawnTowers--;
            _razing.Add(new Razing
            {
                Rig = rig, T = 0f, Tilt = (index & 1) == 0 ? 1f : -1f, Yaw = rig.transform.localEulerAngles.y
            });
        }

        /// <summary>Torre vendida desmontando: inclina um pouco e afunda, depois some.</summary>
        struct Razing
        {
            public ModelRig Rig;
            public float T;
            public float Tilt;
            public float Yaw;
        }
        readonly List<Razing> _razing = new List<Razing>();
        const float RazeTime = 0.6f;

        void AnimateRazing(float dt)
        {
            for (int i = _razing.Count - 1; i >= 0; i--)
            {
                var r = _razing[i];
                r.T += dt;
                if (r.Rig == null || r.T >= RazeTime)
                {
                    if (r.Rig != null) Object.Destroy(r.Rig.gameObject);
                    _razing.RemoveAt(i);
                    continue;
                }
                float u = r.T / RazeTime;
                var tr = r.Rig.transform;
                tr.localPosition += Vector3.down * (dt / RazeTime * r.Rig.Def.Height * 1.1f * (0.4f + 1.2f * u));
                tr.localRotation = Quaternion.Euler(r.Tilt * 9f * u, r.Yaw, r.Tilt * 6f * u);
                // poeira levantando no pé da torre enquanto ela afunda
                if ((i + Time.frameCount) % 3 == 0)
                {
                    var foot = tr.localPosition;
                    foot.y = 0.05f;
                    Vfx.Instance?.Footstep(_root.TransformPoint(foot));
                }
                _razing[i] = r;
            }
        }

        void OnTowerFired(Vector3 localMuzzle)
        {
            // o evento traz a boca "da simulação"; o clarão sai da boca do cano de verdade
            int idx = _sim.TowerIndexAt(_sim.Map.WorldToCell(localMuzzle));
            if (idx >= 0 && idx < _towers.Count)
            {
                _towers[idx].Kick();
                Vfx.Instance?.Muzzle(_towers[idx].MuzzleWorld, _sim.TowerTypeId(idx));
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

        /// <summary>
        /// Espelha o estado da simulação. Chamar uma vez por frame, depois do Tick.
        /// <paramref name="alpha"/> = quanto do próximo tique já passou (acumulador / passo):
        /// é o que deixa o movimento liso entre um tique e outro.
        /// </summary>
        public void Sync(float alpha = 1f)
        {
            if (_cam == null) _cam = Camera.main;
            float dt = Time.deltaTime;
            _ticked = _sim.MatchTime != _lastSimTime;
            _lastSimTime = _sim.MatchTime;
            alpha = Mathf.Clamp01(alpha);
            SyncTowers(dt);
            SyncEnemies(dt, alpha);
            SyncProjectiles(alpha);
            AnimateKeep();
            AnimateDying(dt);
            AnimateRazing(dt);
        }

        void AnimateKeep()
        {
            // estandarte ao vento: balanço lento, nunca parado
            if (_keepFlag != null) _keepFlag.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 1.3f) * 12f, 0f);
        }

        // ------------------------------------------------------------------ torres

        void SyncTowers(float dt)
        {
            // torres novas entram no fim da lista; as vendidas saem em OnTowerSold
            for (int i = _drawnTowers; i < _sim.TowerCount; i++)
            {
                int type = _sim.TowerTypeId(i);
                var rig = ArtFactory.Spawn(ModelLib.Tower(type), _owner, _towerRoot,
                    $"Torre_{TowerCatalog.Get(type).Name}");
                rig.transform.localPosition = _sim.Map.CellToWorld(_sim.TowerCell(i));
                // torreta nasce virada para o acampamento: é de lá que o inimigo vem
                rig.AimAt(_root.TransformPoint(_sim.Map.CellToWorld(_sim.SpawnCells.Count > 0 ? _sim.SpawnCells[0] : _sim.GoalCell)), 1f, 1f);
                _towers.Add(rig);
                _towerAge.Add(0f);
            }
            _drawnTowers = _sim.TowerCount;

            for (int i = 0; i < _towers.Count; i++)
            {
                var rig = _towers[i];
                if (_towerAge[i] < BuildTime)
                {
                    // sobe do chão desacelerando, como quem assenta a última pedra
                    _towerAge[i] = Mathf.Min(BuildTime, _towerAge[i] + dt);
                    float u = 1f - _towerAge[i] / BuildTime;
                    var home = _sim.Map.CellToWorld(_sim.TowerCell(i));
                    rig.transform.localPosition = home + Vector3.down * (u * u * rig.Def.Height);
                }
                // o nível se lê na própria torre: ela cresce e ganha estandarte
                rig.SetLevel(_sim.TowerLevel(i));
                rig.TickTower(dt);
                // canhão acompanha o alvo. Sem isto todos apontam para o mesmo lado para
                // sempre, e a torre parece desligada mesmo enquanto mata.
                if (_sim.TryGetTowerAim(i, out var aim)) rig.AimAt(_root.TransformPoint(aim), dt);

                // braseiro da torre de Fogo: chama viva o tempo todo
                var brazier = rig.BrazierWorld;
                if (brazier.HasValue)
                {
                    while (_brazierFx.Count <= i) _brazierFx.Add(0f);
                    _brazierFx[i] -= dt;
                    if (_brazierFx[i] <= 0f)
                    {
                        _brazierFx[i] = 0.09f;
                        Vfx.Instance?.Brazier(brazier.Value);
                    }
                }
            }

            // o território muda junto com as torres; reconstruir só quando isso acontece
            if (_lastTerritoryStamp != _sim.TowerVersion)
            {
                _lastTerritoryStamp = _sim.TowerVersion;
                _territory.Rebuild(_sim.Territory, _sim.Map);
            }
        }

        // ---------------------------------------------------------------- inimigos

        void SyncEnemies(float dt, float alpha)
        {
            int slots = _sim.EnemySlotCount;
            if (_enemyBySlot.Length != slots)
            {
                System.Array.Resize(ref _enemyBySlot, slots);
                System.Array.Resize(ref _enemyGen, slots);
                System.Array.Resize(ref _enemyPrev, slots);
                System.Array.Resize(ref _enemyCur, slots);
                System.Array.Resize(ref _burnFx, slots);
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

                var simPos = new Vector3(e.Pos.x, 0f, e.Pos.z);
                if (fresh) _enemyPrev[s] = _enemyCur[s] = simPos;
                else if (_ticked)
                {
                    _enemyPrev[s] = _enemyCur[s];
                    _enemyCur[s] = simPos;
                }
                // empurrão da torre de Ar é um salto de verdade: não desenhar deslizando
                if ((_enemyCur[s] - _enemyPrev[s]).sqrMagnitude > 0.5f * 0.5f) _enemyPrev[s] = _enemyCur[s];
                rig.Follow(Vector3.Lerp(_enemyPrev[s], _enemyCur[s], alpha), dt, snap: fresh);

                // cavalo e torre de cerco levantam poeira do chão
                if (rig.Def.Anim == AnimKind.Horse || rig.Def.Anim == AnimKind.Wheels)
                {
                    _burnFx[s] -= dt * 0.35f;
                    if (_burnFx[s] <= 0f && !e.Burning)
                    {
                        _burnFx[s] = 0.1f;
                        Vfx.Instance?.Footstep(rig.transform.position + Vector3.up * 0.05f);
                    }
                }

                // dano se lê na barra; fogo, na chama e no brilho laranja; atrito, no gelado
                float frac = e.MaxHp > 0f ? e.Hp / e.MaxHp : 1f;
                rig.SetHealth(frac, _cam);
                if (e.Burning)
                {
                    _burnFx[s] -= dt;
                    if (_burnFx[s] <= 0f)
                    {
                        _burnFx[s] = 0.1f;
                        Vfx.Instance?.Burn(rig.transform.position + Vector3.up * (rig.Def.Height * 0.45f));
                    }
                    rig.SetGlow(Palette.BurnGlow * (0.7f + 0.3f * Mathf.Sin(Time.time * 17f + s * 3f)));
                    continue;
                }
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
            var rig = _enemyBySlot[slot];
            _enemyBySlot[slot] = null;
            if (rig == null) return;

            // quem chegou à fortaleza some (a explosão do vazamento cobre); quem morreu
            // no caminho tomba — sumir do nada é o que mais denuncia "jogo de protótipo"
            var goal = _sim.Map.CellToWorld(_sim.GoalCell);
            var p = rig.transform.localPosition;
            if (new Vector2(p.x - goal.x, p.z - goal.z).sqrMagnitude < 0.8f * 0.8f)
            {
                Return(rig);
                return;
            }
            rig.ResetState();
            _dying.Add(new Dying
            {
                Rig = rig, T = 0f, From = rig.transform.localRotation,
                Side = ((slot * 7 + _dying.Count) & 1) == 0 ? 1f : -1f,
            });
        }

        void AnimateDying(float dt)
        {
            for (int i = _dying.Count - 1; i >= 0; i--)
            {
                var d = _dying[i];
                d.T += dt;
                var tr = d.Rig.transform;
                // tomba de lado com aceleração (cai, não gira), fica, e afunda no chão
                float fall = Mathf.Clamp01(d.T / FallTime);
                fall *= fall;
                tr.localRotation = d.From * Quaternion.Euler(0f, 0f, d.Side * 88f * fall);
                var pos = tr.localPosition;
                float sink = Mathf.Clamp01((d.T - FallTime - LieTime) / SinkTime);
                pos.y = -sink * d.Rig.Def.Height * 0.6f;
                tr.localPosition = pos;
                if (d.T >= FallTime + LieTime + SinkTime)
                {
                    tr.localRotation = d.From;
                    Return(d.Rig);
                    _dying.RemoveAt(i);
                    continue;
                }
                _dying[i] = d;
            }
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

        void SyncProjectiles(float alpha)
        {
            int slots = _sim.ProjectileSlotCount;
            if (_shotBySlot.Length != slots)
            {
                System.Array.Resize(ref _shotBySlot, slots);
                System.Array.Resize(ref _shotPrev, slots);
                System.Array.Resize(ref _shotCur, slots);
                System.Array.Resize(ref _shotType, slots);
            }

            for (int s = 0; s < slots; s++)
            {
                if (!_sim.TryGetProjectile(s, out var p, out float t, out int towerType, out bool flies))
                {
                    // o tiro acabou de chegar: é aqui que o jogador tem que VER o acerto
                    if (_shotBySlot[s] != null)
                        Vfx.Instance?.Impact(_root.TransformPoint(_shotCur[s]), _shotType[s]);
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
                var target = new Vector3(p.x, y, p.z);
                _shotType[s] = towerType;
                if (fresh) _shotPrev[s] = _shotCur[s] = target;
                else if (_ticked)
                {
                    _shotPrev[s] = _shotCur[s];
                    _shotCur[s] = target;
                }
                var pos = Vector3.Lerp(_shotPrev[s], _shotCur[s], alpha);

                // virote e estilhaço apontam para onde voam; no primeiro frame não há
                // "de onde veio" (a posição antiga é de outro tiro, do pool)
                var d = pos - rig.transform.localPosition;
                rig.transform.localPosition = pos;
                if (fresh)
                {
                    // rastro de tiro reciclado riscaria da posição antiga até aqui
                    var trail = rig.GetComponent<TrailRenderer>();
                    if (trail != null) trail.Clear();
                }
                else if (d.sqrMagnitude > 1e-6f) rig.transform.localRotation = Quaternion.LookRotation(d);
            }
        }

        ModelRig RentShot(int towerType)
        {
            var pooled = Rent(ModelLib.Projectile(towerType));
            if (pooled != null) return pooled;
            var rig = ArtFactory.Spawn(ModelLib.Projectile(towerType), _owner, _projectileRoot, "Projetil");
            foreach (var mr in rig.GetComponentsInChildren<MeshRenderer>())
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // tiro maior que a escala real: no zoom do jogo, bala de verdade some
            rig.transform.localScale = Vector3.one * 1.5f;
            Vfx.AddTrail(rig.gameObject, towerType);
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
