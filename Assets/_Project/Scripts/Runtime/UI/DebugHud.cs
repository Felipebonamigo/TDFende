using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// HUD provisório via OnGUI — zero setup de cena, zero canvas.
    /// Vira UI de verdade (com âncoras e safe area, mobile-ready) na fase 2.
    /// </summary>
    public class DebugHud : MonoBehaviour
    {
        // Medidas do HUD num lugar só, para o clique-no-mundo poder evitá-las.
        static Rect InfoRect => new Rect(12f, 10f, 380f, 84f);
        static Rect HelpRect => new Rect(12f, Screen.height - 128f, 500f, 116f);

        /// <summary>
        /// O clique é lido pelo Input legado, que a IMGUI não consome. Sem esta
        /// checagem, clicar no painel de instruções constrói uma torre na célula
        /// escondida atrás dele — cobrando o ouro e re-roteando a onda.
        /// </summary>
        public static bool PointerOverHud(Vector2 pointerPos)
        {
            // Input.mousePosition tem origem embaixo; Rect de GUI tem origem em cima
            var p = new Vector2(pointerPos.x, Screen.height - pointerPos.y);
            return InfoRect.Contains(p) || HelpRect.Contains(p);
        }

        GameController _gc;
        float _fps;

        public void Init(GameController gc) => _gc = gc;

        void Update() =>
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.05f);

        void OnGUI()
        {
            if (_gc == null || _gc.State == null) return;
            UiSkin.Ensure();

            GUI.Box(InfoRect, GUIContent.none, UiSkin.Panel);
            GUILayout.BeginArea(new Rect(InfoRect.x + 10f, InfoRect.y + 6f, InfoRect.width - 20f, InfoRect.height - 12f));
            GUILayout.Label($"Vidas: {_gc.State.Lives}    Ouro: <color=#E8C15A>{_gc.State.Gold}</color>", UiSkin.Label);
            GUILayout.Label($"Onda: {_gc.Wave}    Inimigos: {_gc.EnemiesAlive}    FPS: {_fps:0}", UiSkin.Label);
            if (_gc.Phase == WavePhase.Building)
                GUILayout.Label($"Próxima onda em {_gc.PhaseTimer:0.0}s  (ESPAÇO adianta)", UiSkin.LabelSmall);
            else if (_gc.Phase != WavePhase.GameOver)
                GUILayout.Label("Onda em andamento!", UiSkin.LabelSmall);
            GUILayout.EndArea();

            GUI.Box(HelpRect,
                $"Clique esquerdo: construir torre ({GameConfig.TowerCost} de ouro)  |  " +
                $"botão direito numa torre: vender ({(int)(GameConfig.TowerCost * TowerWarsConfig.SellRefund)} de ouro)\n" +
                $"Torres projetam FRONTEIRA: inimigos dentro dela sofrem {GameConfig.AttritionDps:0} de dano/s\n" +
                "Inimigo com brilho GELADO = sendo drenado.  Número azul = morreu de atrito, dourado = de tiro\n" +
                "WASD/setas: mover câmera  |  Scroll: zoom  |  Botão do meio: arrastar\n" +
                "ESPAÇO: chamar próxima onda  |  R: reiniciar", UiSkin.Panel);

            if (_gc.Phase == WavePhase.GameOver)
            {
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, UiSkin.Panel);
                UiSkin.Shadowed(new Rect(0, Screen.height * 0.35f, Screen.width, 50),
                    $"A fortaleza caiu na onda {_gc.Wave}", UiSkin.Big, Palette.UiAccent);
                UiSkin.Shadowed(new Rect(0, Screen.height * 0.35f + 55, Screen.width, 30),
                    "Aperte R para reiniciar", UiSkin.Subtitle, Palette.UiInk);
            }
        }
    }
}
