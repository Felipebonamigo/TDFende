using UnityEngine;

namespace TDFende
{
    /// <summary>Projétil teleguiado simples. Vive no pool; nunca é destruído.</summary>
    public class Projectile : MonoBehaviour
    {
        Enemy _target;
        float _damage;
        System.Action<Projectile> _release;

        public void Init(Enemy target, float damage, System.Action<Projectile> release)
        {
            _target = target;
            _damage = damage;
            _release = release;
        }

        void Update()
        {
            // alvo morreu no caminho: some sem impacto
            if (_target == null || !_target.gameObject.activeSelf)
            {
                _release(this);
                return;
            }

            // mira no peito, não no pé: o modelo tem altura
            var to = _target.transform.position + Vector3.up * 0.28f - transform.position;
            float step = GameConfig.ProjectileSpeed * Time.deltaTime;
            if (to.magnitude <= step + 0.25f)
            {
                Vfx.Instance?.Impact(transform.position);
                _target.TakeDamage(_damage, DespawnReason.KilledByTower);
                _release(this);
                return;
            }
            transform.position += to.normalized * step;
        }
    }
}
