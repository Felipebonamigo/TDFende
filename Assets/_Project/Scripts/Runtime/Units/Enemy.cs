using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    public enum DespawnReason
    {
        Leaked,             // chegou na base
        KilledByTower,
        KilledByAttrition   // morreu dentro do território, sem tiro
    }

    /// <summary>
    /// Inimigo: segue o flow field até a base. Sem physics, sem NavMesh —
    /// movimento puro guiado pelo campo compartilhado.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        /// <summary>Registro global de inimigos ativos — as torres consultam isto para mirar.</summary>
        public static readonly List<Enemy> Alive = new List<Enemy>();

        const float SpawnPunch = 0.28f; // duração do "pop" ao nascer
        const float FlashTime = 0.12f;

        public float MaxHp { get; private set; }
        public float Hp { get; private set; }

        float _speed;
        FlowField _flow;
        TerritoryField _territory;
        Vector3 _goal;
        System.Action<Enemy, DespawnReason> _onDespawn;
        Renderer _renderer;
        MaterialPropertyBlock _mpb;
        Vector3 _baseScale;
        float _spawnT;
        float _flashT;
        bool _draining; // dentro do território neste frame

        public void Init(FlowField flow, TerritoryField territory, Vector3 goal, float hp, float speed,
            System.Action<Enemy, DespawnReason> onDespawn)
        {
            _flow = flow;
            _territory = territory;
            _goal = goal;
            MaxHp = Hp = hp;
            _speed = speed;
            _onDespawn = onDespawn;
            _spawnT = 0f;
            _flashT = 0f;
            _draining = false;

            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
                _mpb = new MaterialPropertyBlock();
                _baseScale = transform.localScale;
            }
            transform.localScale = Vector3.zero;
            UpdateTint();
        }

        void OnEnable() => Alive.Add(this);
        void OnDisable() => Alive.Remove(this);

        void Update()
        {
            float dt = Time.deltaTime;

            // "pop" ao nascer: cresce do zero com leve overshoot
            if (_spawnT < SpawnPunch)
            {
                _spawnT += dt;
                float t = Mathf.Clamp01(_spawnT / SpawnPunch);
                float s = 1f + 0.22f * Mathf.Sin(t * Mathf.PI); // passa de 1 e volta
                transform.localScale = _baseScale * (t * s);
            }
            else if (transform.localScale != _baseScale)
            {
                transform.localScale = _baseScale;
            }

            var dir = _flow.SampleDirection(transform.position);
            transform.position += dir * (_speed * dt);
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * dt);

            // atrito: dentro do território do jogador, perde vida sem ninguém atirar
            _draining = _territory.Contains(transform.position);
            if (_draining)
            {
                TakeDamage(GameConfig.AttritionDps * dt, DespawnReason.KilledByAttrition);
                if (Hp <= 0f) return; // morreu de atrito neste frame
            }

            if (_flashT > 0f) _flashT -= dt;
            UpdateTint();

            var flat = transform.position;
            flat.y = 0f;
            if ((flat - _goal).sqrMagnitude < 0.4f * 0.4f)
                _onDespawn(this, DespawnReason.Leaked);
        }

        public void TakeDamage(float dmg, DespawnReason source)
        {
            if (Hp <= 0f) return; // já morreu neste frame
            Hp -= dmg;
            if (source == DespawnReason.KilledByTower) _flashT = FlashTime;
            if (Hp <= 0f)
            {
                _onDespawn(this, source);
                return;
            }
            UpdateTint();
        }

        void UpdateTint()
        {
            // vermelho vivo -> escuro conforme perde vida
            var c = Color.Lerp(Palette.EnemyHurt, Palette.EnemyFull, Hp / MaxHp);
            // sob atrito, puxa pro ciano do território: dá pra VER quem está sendo drenado
            if (_draining) c = Color.Lerp(c, Palette.EnemyDrained, 0.55f);
            // flash branco no impacto do projétil
            if (_flashT > 0f) c = Color.Lerp(c, Palette.HitFlash, _flashT / FlashTime);

            _mpb.SetColor(MaterialFactory.ColorProperty, c);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
