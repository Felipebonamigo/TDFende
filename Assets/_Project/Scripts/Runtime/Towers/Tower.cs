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
        float _recoil;
        Vector3 _headRest;

        public void Init(Transform head, SimplePool<Projectile> projectiles, System.Action<Projectile> release)
        {
            _head = head;
            _projectiles = projectiles;
            _release = release;
            _headRest = head.localPosition;
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
                var muzzle = _head.position + _head.forward * 0.35f;
                var p = _projectiles.Get();
                p.transform.position = muzzle;
                p.Init(target, GameConfig.TowerDamage, _release);
                Vfx.Instance?.Muzzle(muzzle);
                _recoil = 1f;
            }

            // coice: a cabeça recua e volta — o tiro ganha peso
            _recoil = Mathf.Max(0f, _recoil - 6f * Time.deltaTime);
            _head.localPosition = _headRest - _head.localRotation * Vector3.forward * (_recoil * 0.12f);
        }
    }
}
