using System.Collections.Generic;
using UnityEngine;

namespace FrontierTD
{
    /// <summary>
    /// Inimigo: segue o flow field até a base. Sem physics, sem NavMesh —
    /// movimento puro guiado pelo campo compartilhado.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        /// <summary>Registro global de inimigos ativos — as torres consultam isto para mirar.</summary>
        public static readonly List<Enemy> Alive = new List<Enemy>();

        static readonly Color FullHpColor = new Color(0.85f, 0.20f, 0.20f);
        static readonly Color LowHpColor = new Color(0.25f, 0.05f, 0.05f);

        public float MaxHp { get; private set; }
        public float Hp { get; private set; }

        float _speed;
        FlowField _flow;
        Vector3 _goal;
        System.Action<Enemy, bool> _onDespawn; // (inimigo, morreuEmCombate)
        Renderer _renderer;
        MaterialPropertyBlock _mpb;

        public void Init(FlowField flow, Vector3 goal, float hp, float speed, System.Action<Enemy, bool> onDespawn)
        {
            _flow = flow;
            _goal = goal;
            MaxHp = Hp = hp;
            _speed = speed;
            _onDespawn = onDespawn;
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
                _mpb = new MaterialPropertyBlock();
            }
            UpdateTint();
        }

        void OnEnable() => Alive.Add(this);
        void OnDisable() => Alive.Remove(this);

        void Update()
        {
            var dir = _flow.SampleDirection(transform.position);
            transform.position += dir * (_speed * Time.deltaTime);
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);

            var flat = transform.position;
            flat.y = 0f;
            if ((flat - _goal).sqrMagnitude < 0.4f * 0.4f)
                _onDespawn(this, false); // chegou na base
        }

        public void TakeDamage(float dmg)
        {
            if (Hp <= 0f) return; // já morreu neste frame
            Hp -= dmg;
            if (Hp <= 0f)
            {
                _onDespawn(this, true);
                return;
            }
            UpdateTint();
        }

        // cor vai escurecendo com o dano — barra de vida placeholder
        void UpdateTint()
        {
            _mpb.SetColor(MaterialFactory.ColorProperty, Color.Lerp(LowHpColor, FullHpColor, Hp / MaxHp));
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
