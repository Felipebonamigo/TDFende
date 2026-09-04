using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Tela inicial. Existe para os dois modos conviverem sem um atropelar o outro:
    /// o TD de uma lane (fase 0) segue exatamente como estava, e o Tower Wars entra
    /// ao lado em vez de substituí-lo.
    /// </summary>
    public class ModeSelect : MonoBehaviour
    {
        GUIStyle _title, _sub, _button;
        bool _started;

        void OnGUI()
        {
            if (_started) return;
            EnsureStyles();

            float w = 460f, h = 44f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.22f;

            GUI.Label(new Rect(0, y - 90f, Screen.width, 46f), "TDFende", _title);
            GUI.Label(new Rect(0, y - 46f, Screen.width, 30f),
                "tower defense com fronteira territorial e atrito", _sub);

            if (GUI.Button(new Rect(x, y, w, h), "TD clássico — uma lane, ondas infinitas", _button))
                Launch<GameController>();

            y += h + 22f;
            GUI.Label(new Rect(x, y - 20f, w, 20f),
                "Tower Wars — defenda a sua lane e envie inimigos na do adversário", _sub);

            float bw = (w - 16f) / 3f;
            if (GUI.Button(new Rect(x, y, bw, h), "Fácil", _button))
                LaunchWars(TowerWarsAi.Personality.Easy);
            if (GUI.Button(new Rect(x + bw + 8f, y, bw, h), "Normal", _button))
                LaunchWars(TowerWarsAi.Personality.Normal);
            if (GUI.Button(new Rect(x + (bw + 8f) * 2f, y, bw, h), "Difícil", _button))
                LaunchWars(TowerWarsAi.Personality.Hard);

            // Atalho para quem quer mexer nos números: gera os arquivos de balanceamento
            // já preenchidos com os valores atuais, prontos para editar.
            y += h + 30f;
            if (GUI.Button(new Rect(x + bw + 8f, y, bw, 30f), "Exportar balanceamento", _button))
            {
                try
                {
                    _exportMessage = "arquivos em: " + CatalogLoader.ExportDefaults();
                }
                catch (System.Exception e)
                {
                    _exportMessage = "falhou: " + e.Message;
                }
            }
            if (_exportMessage != null)
                GUI.Label(new Rect(0, y + 34f, Screen.width, 22f), _exportMessage, _sub);
        }

        string _exportMessage;

        void Launch<T>() where T : MonoBehaviour
        {
            _started = true;
            new GameObject("== TDFende ==").AddComponent<T>();
            Destroy(gameObject);
        }

        void LaunchWars(TowerWarsAi.Personality difficulty)
        {
            _started = true;
            var go = new GameObject("== TDFende (Tower Wars) ==");
            // dificuldade definida ANTES do Start do controlador rodar
            go.AddComponent<TowerWarsController>().Difficulty = difficulty;
            Destroy(gameObject);
        }

        void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label)
            { fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _sub = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 15 };
        }
    }
}
