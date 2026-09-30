using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Relógio de desenho de uma lane: quanto do próximo tique já passou (0..1) e o tamanho
    /// do tique. Quem escreve é a LaneView (uma vez por frame, ANTES dos Update das vistas —
    /// o TowerWarsController roda primeiro, ver DefaultExecutionOrder); quem lê são as vistas.
    /// </summary>
    public sealed class LaneClock
    {
        public float Alpha;
        public float TickSeconds = TowerWarsConfig.FixedStep;
    }

    /// <summary>
    /// Vista de UM <see cref="LaneSim.SimEnemy"/>: guarda a posição do tique anterior e a do
    /// tique atual e, a cada frame, desenha entre as duas. A simulação anda a 30 Hz; sem
    /// isto o boneco pularia em degraus numa tela de 60+ quadros.
    ///
    /// Estritamente vista: recebe CÓPIAS do struct (valor, não referência) e só mexe em
    /// Transform, animação, barra de vida e brilho do material. Não há caminho daqui de
    /// volta para a simulação.
    ///
    /// Desenha o inimigo UM tique atrás do estado mais novo: entre o tique N-1 e o N, com
    /// alpha indo de 0 a 1. É o preço de nunca extrapolar — extrapolar erra toda vez que
    /// o inimigo vira uma esquina ou leva um empurrão, e o erro aparece como tranco.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyView : MonoBehaviour
    {
        // empurrão da torre de Ar é um salto de verdade: acima disto, não desenhar deslizando
        const float TeleportDistance = 0.5f;

        ModelRig _rig;
        LaneClock _clock;
        Camera _cam;
        int _slot;

        Vector3 _prev, _cur;        // posição (local da lane, Y = 0) nos tiques N-1 e N
        LaneSim.SimEnemy _state;    // cópia do tique N
        bool _drained;              // dentro da fronteira inimiga (atrito), no tique N
        bool _snap;                 // primeiro quadro: aparece no lugar, sem virar nem andar
        float _flameTimer, _dustTimer, _chillTimer;

        // casca de gelo por cima do bicho congelado: cresce ao congelar, some ao descongelar
        Transform _crust;
        float _crustScale;          // 0 = sem casca, 1 = cobrindo o bicho
        bool _wasFrozen;

        static float _maxBurn = -1f;

        public int Slot => _slot;
        public int Generation => _state.Generation;
        public bool Flies => _state.IgnoresTerritory;
        public ModelRig Rig => _rig;

        /// <summary>Posição desenhada num alpha qualquer (local da lane). O tiro mira aqui.</summary>
        public Vector3 PositionAt(float alpha) => Vector3.Lerp(_prev, _cur, Mathf.Clamp01(alpha));

        /// <summary>Liga a vista a um boneco recém-tirado do pool, para um inimigo novo.</summary>
        public void Spawn(ModelRig rig, LaneClock clock, int slot, in LaneSim.SimEnemy e, bool drained,
            Quaternion facing)
        {
            _rig = rig;
            _clock = clock;
            _slot = slot;
            _state = e;
            _drained = drained;
            _prev = _cur = Flat(e.Pos);
            _snap = true;
            _flameTimer = _dustTimer = _chillTimer = 0f;
            _wasFrozen = false;
            _crustScale = 0f;
            if (_crust != null) _crust.gameObject.SetActive(false);
            if (_maxBurn < 0f) _maxBurn = Mathf.Max(0.0001f, LaneSim.MaxBurnPct);
            transform.localRotation = facing;
            enabled = true;
        }

        /// <summary>Um tique da simulação passou: o atual vira anterior, o novo vira atual.</summary>
        public void PushTick(in LaneSim.SimEnemy e, bool drained)
        {
            // virou gelo / quebrou o gelo: o estalo marca o momento
            if (e.Frozen && !_wasFrozen) Vfx.Instance?.Freeze(transform.position + Vector3.up * (_rig.Def.Height * 0.5f));
            else if (!e.Frozen && _wasFrozen) Vfx.Instance?.Thaw(transform.position + Vector3.up * (_rig.Def.Height * 0.5f));
            _wasFrozen = e.Frozen;
            _state = e;
            _drained = drained;
            _prev = _cur;
            _cur = Flat(e.Pos);
            if ((_cur - _prev).sqrMagnitude > TeleportDistance * TeleportDistance) _prev = _cur;
        }

        /// <summary>O inimigo saiu da simulação: a LaneView assume o boneco (queda, pool).</summary>
        public void Stop()
        {
            enabled = false;
            if (_crust != null) _crust.gameObject.SetActive(false);
        }

        void Update()
        {
            if (_rig == null || _clock == null) return;
            float dt = Time.deltaTime;
            if (_cam == null) _cam = Camera.main;

            // movimento: interpolação pura entre dois tiques; ModelRig.Follow vira o corpo
            // para onde andou e anima a perna pela distância percorrida
            _rig.Follow(PositionAt(_clock.Alpha), dt, _snap);
            _snap = false;

            float frac = _state.MaxHp > 0f ? _state.Hp / _state.MaxHp : 1f;
            _rig.SetHealth(frac, _cam);

            // bicho grande levanta poeira do chão (rato e cachorro não)
            if ((_rig.Def.Anim == AnimKind.Horse || _rig.Def.Anim == AnimKind.Wheels) && _rig.Def.Height > 0.5f)
            {
                _dustTimer -= dt;
                if (_dustTimer <= 0f)
                {
                    _dustTimer = 0.28f;
                    Vfx.Instance?.Footstep(transform.position + Vector3.up * 0.05f);
                }
            }

            UpdateGlow(dt);
            UpdateCrust(dt);
        }

        /// <summary>
        /// Congelado: casca de cristais de gelo sobre o bicho, do tamanho dele. Cresce rápido
        /// ao congelar (estala) e encolhe ao descongelar — e as patas param, porque a
        /// simulação o deixou parado no lugar.
        /// </summary>
        void UpdateCrust(float dt)
        {
            float want = _state.Frozen ? 1f : 0f;
            _crustScale = Mathf.MoveTowards(_crustScale, want, dt * (want > _crustScale ? 9f : 5f));
            if (_crustScale <= 0.001f)
            {
                if (_crust != null && _crust.gameObject.activeSelf) _crust.gameObject.SetActive(false);
                return;
            }
            if (_crust == null)
            {
                var rig = ArtFactory.Spawn(ModelLib.IceCrust(), Color.white, transform, "CascaDeGelo");
                _crust = rig.transform;
                foreach (var r in _crust.GetComponentsInChildren<Renderer>())
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (!_crust.gameObject.activeSelf) _crust.gameObject.SetActive(true);
            // voador congela no ar: a casca sobe com ele
            float h = _rig.Def.Height;
            float size = Flies ? 0.35f : Mathf.Max(0.18f, h * 0.9f);
            _crust.localPosition = Flies ? new Vector3(0f, h - 0.2f, 0f) : Vector3.zero;
            _crust.localRotation = Quaternion.identity;
            _crust.localScale = Vector3.one * (size * (0.3f + 0.7f * _crustScale));
        }

        /// <summary>
        /// Fogo: brilho laranja PROPORCIONAL ao BurnPct (uma camada brilha pouco, três camadas
        /// incandescem) e apagando nos últimos instantes da queima. Sem fogo, o azul-gelo do
        /// atrito. A cor vai por MaterialPropertyBlock (ModelRig.SetGlow): o material
        /// compartilhado não é tocado nem instanciado, então o batching continua valendo.
        /// </summary>
        void UpdateGlow(float dt)
        {
            // congelado vence tudo: branco-azulado forte, o bicho vira estátua de gelo
            if (_state.Frozen)
            {
                _rig.SetGlow(Palette.Frost * 0.85f);
                return;
            }

            if (_state.Burning && _state.BurnPct > 0f)
            {
                float intensity = Mathf.Clamp01(_state.BurnPct / _maxBurn);
                float fade = Mathf.Clamp01(_state.BurnLeft / 0.4f);
                float flicker = 0.75f + 0.25f * Mathf.Sin(Time.time * 17f + _slot * 3f);
                _rig.SetGlow(Palette.BurnGlow * ((0.35f + 0.9f * intensity) * fade * flicker));

                // Pegando fogo de verdade: chama sai de vários pontos do corpo, mais amiúde
                // quanto mais camadas — e bicho grande queima em mais lugares ao mesmo tempo.
                _flameTimer -= dt;
                if (_flameTimer <= 0f)
                {
                    _flameTimer = Mathf.Lerp(0.14f, 0.04f, intensity);
                    float h = _rig.Def.Height;
                    int spots = 1 + Mathf.RoundToInt(intensity * 2f) + (h > 0.7f ? 1 : 0);
                    for (int k = 0; k < spots; k++)
                    {
                        float u = Random.value, v = Random.value;
                        var local = new Vector3((u - 0.5f) * h * 0.5f, h * (0.35f + 0.4f * v), (v - 0.5f) * h * 0.9f);
                        Vfx.Instance?.Burn(transform.TransformPoint(local));
                    }
                }
                return;
            }

            // gelado (lento): azul frio e geada soltando, mais forte quanto mais lento
            if (_state.SlowLeft > 0f && _state.SlowFactor < 1f)
            {
                float cold = Mathf.Clamp01((1f - _state.SlowFactor) / 0.65f);
                _rig.SetGlow(Palette.Frost * (0.18f + 0.3f * cold));
                _chillTimer -= dt;
                if (_chillTimer <= 0f)
                {
                    _chillTimer = Mathf.Lerp(0.25f, 0.1f, cold);
                    Vfx.Instance?.Chill(transform.position + Vector3.up * (_rig.Def.Height * (0.3f + 0.5f * Random.value)));
                }
                return;
            }

            _rig.SetGlow(_drained
                ? Palette.AttritionGlow * (0.75f + 0.25f * Mathf.Sin(Time.time * 6f + _slot))
                : Color.black);
        }

        static Vector3 Flat(Vector3 p) => new Vector3(p.x, 0f, p.z);
    }
}
