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
        float _flameTimer, _dustTimer;

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
            _flameTimer = _dustTimer = 0f;
            if (_maxBurn < 0f) _maxBurn = Mathf.Max(0.0001f, LaneSim.MaxBurnPct);
            transform.localRotation = facing;
            enabled = true;
        }

        /// <summary>Um tique da simulação passou: o atual vira anterior, o novo vira atual.</summary>
        public void PushTick(in LaneSim.SimEnemy e, bool drained)
        {
            _state = e;
            _drained = drained;
            _prev = _cur;
            _cur = Flat(e.Pos);
            if ((_cur - _prev).sqrMagnitude > TeleportDistance * TeleportDistance) _prev = _cur;
        }

        /// <summary>O inimigo saiu da simulação: a LaneView assume o boneco (queda, pool).</summary>
        public void Stop() => enabled = false;

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

            // cavalo e torre de cerco levantam poeira do chão
            if (_rig.Def.Anim == AnimKind.Horse || _rig.Def.Anim == AnimKind.Wheels)
            {
                _dustTimer -= dt;
                if (_dustTimer <= 0f)
                {
                    _dustTimer = 0.28f;
                    Vfx.Instance?.Footstep(transform.position + Vector3.up * 0.05f);
                }
            }

            UpdateGlow(dt);
        }

        /// <summary>
        /// Fogo: brilho laranja PROPORCIONAL ao BurnPct (uma camada brilha pouco, três camadas
        /// incandescem) e apagando nos últimos instantes da queima. Sem fogo, o azul-gelo do
        /// atrito. A cor vai por MaterialPropertyBlock (ModelRig.SetGlow): o material
        /// compartilhado não é tocado nem instanciado, então o batching continua valendo.
        /// </summary>
        void UpdateGlow(float dt)
        {
            if (_state.Burning && _state.BurnPct > 0f)
            {
                float intensity = Mathf.Clamp01(_state.BurnPct / _maxBurn);
                float fade = Mathf.Clamp01(_state.BurnLeft / 0.4f);
                float flicker = 0.75f + 0.25f * Mathf.Sin(Time.time * 17f + _slot * 3f);
                _rig.SetGlow(Palette.BurnGlow * ((0.35f + 0.9f * intensity) * fade * flicker));

                // chama sai mais amiúde quanto mais camadas
                _flameTimer -= dt;
                if (_flameTimer <= 0f)
                {
                    _flameTimer = Mathf.Lerp(0.16f, 0.05f, intensity);
                    Vfx.Instance?.Burn(transform.position + Vector3.up * (_rig.Def.Height * 0.45f));
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
