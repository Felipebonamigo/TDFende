using UnityEngine;

namespace TDFende
{
    /// <summary>Torre: mira no inimigo mais próximo dentro do alcance e atira projéteis do pool.</summary>
    public class Tower : MonoBehaviour
    {
        ModelRig _rig;
        SimplePool<Projectile> _projectiles;
        System.Action<Projectile> _release;
        float _cooldown;

        public void Init(ModelRig rig, SimplePool<Projectile> projectiles, System.Action<Projectile> release)
        {
            _rig = rig;
            _projectiles = projectiles;
            _release = release;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _cooldown -= dt;
            // O coice tem que voltar mesmo sem alvo: com o decaimento depois do early-return,
            // o cano congelava recuado no último tiro da onda até o próximo inimigo aparecer.
            _rig.TickTower(dt);

            // alvo: inimigo mais próximo dentro do alcance (varredura simples no registro global)
            Enemy target = null;
            float best = GameConfig.TowerRange * GameConfig.TowerRange;
            var pos = transform.position;
            for (int i = 0; i < Enemy.Alive.Count; i++)
            {
                var e = Enemy.Alive[i];
                var d = e.transform.position - pos;
                d.y = 0f;
                float sq = d.sqrMagnitude;
                if (sq < best)
                {
                    best = sq;
                    target = e;
                }
            }
            if (target == null) return;

            _rig.AimAt(target.transform.position, dt, 12f);

            if (_cooldown <= 0f)
            {
                _cooldown = GameConfig.TowerCooldown;
                var muzzle = _rig.MuzzleWorld;
                var p = _projectiles.Get();
                p.transform.position = muzzle;
                p.Init(target, GameConfig.TowerDamage, _release);
                Vfx.Instance?.Muzzle(muzzle);
                _rig.Kick(); // coice: o cano recua e volta — o tiro ganha peso
            }
        }
    }
}
