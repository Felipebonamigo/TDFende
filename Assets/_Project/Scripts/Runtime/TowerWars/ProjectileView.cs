using System;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Vista de UM <see cref="LaneSim.SimProjectile"/>. A simulação não tem física: o tiro é
    /// só "alvo + tempo de voo", e o acerto acontece quando o tempo acaba. Aqui o voo vira
    /// imagem com matemática pura — sem Rigidbody, sem Collider:
    ///
    ///   progresso = tempo decorrido / TotalTime
    ///   posição   = Lerp(boca do cano, posição DESENHADA do alvo, progresso) (+ arco)
    ///
    /// O alvo é lido da <see cref="EnemyView"/> no mesmo alpha em que ela se desenha, então
    /// o tiro chega exatamente onde o boneco está na tela, não onde a simulação o deixou.
    ///
    /// Estritamente vista: recebe cópias do struct e só mexe no próprio Transform e rastro.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileView : MonoBehaviour
    {
        const float GroundTargetY = 0.28f; // peito de quem anda
        const float FlyingTargetY = 1.1f;  // planador lá em cima

        ModelRig _rig;
        LaneClock _clock;
        Transform _laneRoot;
        Action<ProjectileView> _onDone;

        Vector3 _origin;        // boca do cano (local da lane)
        float _totalTime;
        float _timeLeftPrev;    // TimeLeft no tique N-1 (a vista anda um tique atrás, como o inimigo)
        int _towerType;
        bool _arc;

        EnemyView _target;
        int _targetGeneration;
        Vector3 _lastTarget;    // última posição vista do alvo: se ele morrer, o tiro termina ali
        float _targetY;

        bool _released;         // a simulação já resolveu o acerto; falta só a imagem chegar
        float _landingClock;    // tempo de tela desde o tique N-1, depois que a simulação soltou o tiro
        float _shownProgress;   // nunca volta: com quadros e tiques desencontrados, o tiro não recua

        public ModelRig Rig => _rig;

        /// <summary>Um tiro novo saiu da torre.</summary>
        public void Launch(ModelRig rig, LaneClock clock, Transform laneRoot, in LaneSim.SimProjectile p,
            float muzzleY, EnemyView target, Action<ProjectileView> onDone)
        {
            _rig = rig;
            _clock = clock;
            _laneRoot = laneRoot;
            _onDone = onDone;
            _towerType = p.TowerTypeId;
            _arc = p.TowerTypeId == 1; // bomba de morteiro sobe em arco
            _origin = new Vector3(p.Origin.x, muzzleY, p.Origin.z);
            _totalTime = Mathf.Max(p.TotalTime, 0.0001f);
            // foi disparado DURANTE o tique N: no tique N-1 o voo nem tinha começado
            _timeLeftPrev = Mathf.Min(p.TotalTime, p.TimeLeft + clock.TickSeconds);
            _released = false;
            _landingClock = 0f;
            _shownProgress = 0f;

            _target = target;
            _targetGeneration = target != null ? target.Generation : -1;
            // mira no peito do bicho: rato rente ao chão, elefante lá em cima
            _targetY = target == null ? GroundTargetY
                : target.Flies ? FlyingTargetY
                : Mathf.Clamp(target.Rig.Def.Height * 0.5f, 0.08f, 0.9f);
            _lastTarget = target != null ? target.PositionAt(clock.Alpha) : _origin;

            transform.localPosition = _origin;
            // rastro de tiro reciclado riscaria da posição antiga até a boca do cano
            var trail = GetComponent<TrailRenderer>();
            if (trail != null) trail.Clear();
            enabled = true;
        }

        /// <summary>Um tique passou com o tiro ainda no ar.</summary>
        public void PushTick(in LaneSim.SimProjectile p)
        {
            // o tique N vira o N-1 da próxima conta: guardamos o TimeLeft de ANTES deste passo
            _timeLeftPrev = p.TimeLeft + _clock.TickSeconds;
        }

        /// <summary>
        /// A simulação resolveu o acerto (o slot ficou vazio). A vista está um tique atrás:
        /// deixa o tiro terminar o voo na tela e só então mostra o impacto.
        /// </summary>
        public void Release()
        {
            if (_released) return;
            _released = true;
            // O tique que resolveu o acerto também andou: o "N-1" da conta avança um tique.
            // O relógio de tela começa onde o último quadro desenhou (alpha - 1 tique, que pode
            // ser negativo) e segue em tempo real — sem salto, até chegar ao alvo.
            _timeLeftPrev = Mathf.Max(0f, _timeLeftPrev - _clock.TickSeconds);
            _landingClock = (_clock.Alpha - 1f) * _clock.TickSeconds;
        }

        void Update()
        {
            if (_rig == null || _clock == null) return;

            float progress;
            float elapsed;
            if (!_released)
            {
                // tempo decorrido desenhado = até o tique N-1 + a fração alpha do tique seguinte
                elapsed = (_totalTime - _timeLeftPrev) + _clock.Alpha * _clock.TickSeconds;
            }
            else
            {
                // acerto já decidido: completa o voo no tempo de tela (falta no máximo um tique)
                _landingClock += Time.deltaTime;
                elapsed = (_totalTime - _timeLeftPrev) + _landingClock;
            }
            progress = Mathf.Max(_shownProgress, Mathf.Clamp01(elapsed / _totalTime));
            _shownProgress = progress;

            // alvo vivo e ainda o mesmo (o slot pode ter sido reciclado): segue o boneco
            if (_target != null && _target.isActiveAndEnabled && _target.Generation == _targetGeneration)
                _lastTarget = _target.PositionAt(_clock.Alpha);

            var aim = new Vector3(_lastTarget.x, _targetY, _lastTarget.z);
            var pos = Vector3.Lerp(_origin, aim, progress);
            if (_arc) pos.y += 4f * progress * (1f - progress) * 1.3f;

            // virote e estilhaço apontam para onde voam
            var delta = pos - transform.localPosition;
            transform.localPosition = pos;
            if (delta.sqrMagnitude > 1e-6f) transform.localRotation = Quaternion.LookRotation(delta);

            if (_released && progress >= 1f)
            {
                // é aqui que o jogador tem que VER o acerto
                Vfx.Instance?.Impact(_laneRoot.TransformPoint(pos), _towerType);
                enabled = false;
                var done = _onDone;
                _onDone = null;
                _target = null;
                done?.Invoke(this);
            }
        }
    }
}
