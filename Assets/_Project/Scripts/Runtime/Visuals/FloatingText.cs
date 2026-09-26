using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Números que sobem e somem (ouro ganho, vida perdida).
    /// Só em eventos com significado — nunca a cada tique de dano, senão vira poluição.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        public static FloatingText Instance { get; private set; }

        const float Life = 1.1f;
        const float RiseSpeed = 1.4f;

        struct Entry
        {
            public Vector3 World;
            public string Text;
            public Color Color;
            public float Age;
        }

        readonly List<Entry> _entries = new List<Entry>(32);
        Camera _cam;

        void Awake() => Instance = this;

        public void Init(Camera cam) => _cam = cam;

        public void Show(Vector3 world, string text, Color color)
        {
            if (_entries.Count >= 32) _entries.RemoveAt(0); // teto rígido: nunca vira enxurrada
            _entries.Add(new Entry { World = world, Text = text, Color = color, Age = 0f });
        }

        public void Clear() => _entries.Clear();

        void Update()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                e.Age += Time.deltaTime;
                if (e.Age >= Life)
                {
                    _entries.RemoveAt(i);
                    continue;
                }
                e.World += Vector3.up * (RiseSpeed * Time.deltaTime);
                _entries[i] = e;
            }
        }

        void OnGUI()
        {
            if (_cam == null || _entries.Count == 0) return;
            UiSkin.Ensure();

            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                var sp = _cam.WorldToScreenPoint(e.World);
                if (sp.z <= 0f) continue; // atrás da câmera

                float t = e.Age / Life;
                var c = e.Color;
                c.a = 1f - t * t; // segura opaco e some no fim
                // 300 de largura, não 120: a mensagem mais longa ("Onda N limpa!  +25")
                // não cabia em 120 px a 17 bold e saía cortada com reticências.
                // Sombra: sem ela o número dourado some em cima da relva clara.
                UiSkin.Shadowed(new Rect(sp.x - 150f, Screen.height - sp.y - 12f, 300f, 24f), e.Text, UiSkin.Floating, c);
            }
        }
    }
}
