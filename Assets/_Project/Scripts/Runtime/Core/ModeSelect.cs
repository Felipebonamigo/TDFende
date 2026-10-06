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
        bool _started;

        void Start()
        {
            // teste de fumaça do executável: entra direto, tira print e fecha
            var capture = SmokeCapture.RequestedPath();
            if (capture == null) return;
            SmokeCapture.Begin(capture);
            LaunchWars(TowerWarsAi.Personality.Normal);
        }

        void OnGUI()
        {
            if (_started) return;
            UiSkin.Ensure();

            float w = 460f, h = 44f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.22f;

            // o menu é um painel escuro por cima do céu, não texto solto na tela
            GUI.Box(new Rect(x - 30f, y - 110f, w + 60f, 330f), GUIContent.none, UiSkin.Panel);
            UiSkin.Shadowed(new Rect(0, y - 96f, Screen.width, 50f), "TDFende", UiSkin.Title, Palette.UiAccent);
            UiSkin.Shadowed(new Rect(0, y - 46f, Screen.width, 30f),
                "tower defense com fronteira territorial e atrito", UiSkin.Subtitle, Palette.UiInkDim);

            if (GUI.Button(new Rect(x, y, w, h), "TD clássico — uma lane, ondas infinitas", UiSkin.Button))
                Launch<GameController>();

            y += h + 26f;
            UiSkin.Shadowed(new Rect(x, y - 22f, w, 20f),
                "Tower Wars — defenda a sua lane e envie inimigos na do adversário", UiSkin.Subtitle, Palette.UiInk);

            float bw = (w - 16f) / 3f;
            if (GUI.Button(new Rect(x, y, bw, h), "Fácil", UiSkin.Button))
                LaunchWars(TowerWarsAi.Personality.Easy);
            if (GUI.Button(new Rect(x + bw + 8f, y, bw, h), "Normal", UiSkin.Button))
                LaunchWars(TowerWarsAi.Personality.Normal);
            if (GUI.Button(new Rect(x + (bw + 8f) * 2f, y, bw, h), "Difícil", UiSkin.Button))
                LaunchWars(TowerWarsAi.Personality.Hard);

            // Atalho para quem quer mexer nos números: gera os arquivos de balanceamento
            // já preenchidos com os valores atuais, prontos para editar.
            y += h + 30f;
            if (GUI.Button(new Rect(x + bw + 8f, y, bw, 30f), "Exportar balanceamento", UiSkin.Button))
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
                UiSkin.Shadowed(new Rect(0, y + 34f, Screen.width, 22f), _exportMessage, UiSkin.Subtitle, Palette.UiInk);
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
    }
}
