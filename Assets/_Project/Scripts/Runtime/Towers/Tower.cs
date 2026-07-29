using UnityEngine;

namespace FrontierTD
{
    /// <summary>Torre: mira no inimigo mais próximo dentro do alcance e atira projéteis do pool.</summary>
    public class Tower : MonoBehaviour
    {
        Transform _head;
        SimplePool<Projectile> _projectiles;
        System.Action<Projectile> _release;
        float _cooldown;

        public void Init(Transform head, SimplePool<Projectile> projectiles, System.Action<Projectile> release)
        {
            _head = head;
            _projectiles = projectiles;
            _release = release;
        }

        void Update()
        {
            _cooldown -= Time.deltaTime;

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

            var look = target.transform.position - _head.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                _head.rotation = Quaternion.Slerp(_head.rotation, Quaternion.LookRotation(look), 12f * Time.deltaTime);

            if (_cooldown <= 0f)
            {
                _cooldown = GameConfig.TowerCooldown;
                var p = _projectiles.Get();
                p.transform.position = _head.position;
                p.Init(target, GameConfig.TowerDamage, _release);
            }
        }
    }
}
