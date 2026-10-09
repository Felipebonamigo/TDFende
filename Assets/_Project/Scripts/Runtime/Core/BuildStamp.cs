using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Selo do build no canto da tela e no Player.log: de que commit veio o executável (TEC-03).
    /// O Editor/BuildJogo põe `git describe --always --dirty` na versão do executável
    /// (Application.version) só durante o build; "-dirty" = gerado com mudança sem commit.
    /// </summary>
    public class BuildStamp : MonoBehaviour
    {
        /// <summary>Versão que o BuildJogo não carimbou (a do ProjectSettings).</summary>
        public const string Unstamped = "1.0";

        public static string Text => Application.isEditor ? "editor" : "build " + Application.version;

        GUIStyle _style;

        public static void Create()
        {
            var go = new GameObject("== selo do build ==");
            DontDestroyOnLoad(go);
            go.AddComponent<BuildStamp>();
            Debug.Log("[TDFende] " + Text);
        }

        void OnGUI()
        {
            if (_style == null)
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.LowerRight,
                    normal = { textColor = new Color(1f, 1f, 1f, 0.55f) }
                };
            GUI.Label(new Rect(0f, 0f, Screen.width - 6f, Screen.height - 2f), Text, _style);
        }
    }
}
