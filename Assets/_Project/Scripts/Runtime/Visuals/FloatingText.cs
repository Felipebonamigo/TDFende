using System.Collections.Generic;
using UnityEngine;

namespace FrontierTD
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
        GUIStyle _style;

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
            if (_style == null)
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

            var prev = GUI.color;
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                var sp = _cam.WorldToScreenPoint(e.World);
                if (sp.z <= 0f) continue; // atrás da câmera

                float t = e.Age / Life;
                var c = e.Color;
                c.a = 1f - t * t; // segura opaco e some no fim
                GUI.color = c;
                GUI.Label(new Rect(sp.x - 60f, Screen.height - sp.y - 12f, 120f, 24f), e.Text, _style);
            }
            GUI.color = prev;
        }
    }
}
