using UnityEngine;

namespace FrontierTD
{
    /// <summary>
    /// HUD provisório via OnGUI — zero setup de cena, zero canvas.
    /// Vira UI de verdade (com âncoras e safe area, mobile-ready) na fase 2.
    /// </summary>
    public class DebugHud : MonoBehaviour
    {
        GameController _gc;
        GUIStyle _label;
        GUIStyle _big;
        GUIStyle _box;
        float _fps;

        public void Init(GameController gc) => _gc = gc;

        void Update() =>
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.05f);

        void OnGUI()
        {
            if (_gc == null || _gc.State == null) return;
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
                _big = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _box = new GUIStyle(GUI.skin.box) { fontSize = 13, alignment = TextAnchor.UpperLeft };
            }

            GUILayout.BeginArea(new Rect(12, 10, 360, 120));
            GUILayout.Label($"Vidas: {_gc.State.Lives}    Ouro: {_gc.State.Gold}", _label);
            GUILayout.Label($"Onda: {_gc.Wave}    Inimigos: {_gc.EnemiesAlive}    FPS: {_fps:0}", _label);
            if (_gc.Phase == WavePhase.Building)
                GUILayout.Label($"Próxima onda em {_gc.PhaseTimer:0.0}s  (ESPAÇO adianta)", _label);
            else if (_gc.Phase != WavePhase.GameOver)
                GUILayout.Label("Onda em andamento!", _label);
            GUILayout.EndArea();

            GUI.Box(new Rect(12, Screen.height - 112, 470, 100),
                $"Clique esquerdo: construir torre ({GameConfig.TowerCost} de ouro)\n" +
                $"Torres projetam FRONTEIRA: inimigos dentro dela sofrem {GameConfig.AttritionDps:0} de dano/s\n" +
                "WASD/setas: mover câmera  |  Scroll: zoom  |  Botão do meio: arrastar\n" +
                "ESPAÇO: chamar próxima onda  |  R: reiniciar", _box);

            if (_gc.Phase == WavePhase.GameOver)
            {
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
                GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 50),
                    $"FIM DE JOGO — você chegou à onda {_gc.Wave}", _big);
                GUI.Label(new Rect(0, Screen.height * 0.35f + 55, Screen.width, 40),
                    "Aperte R para reiniciar", _big);
            }
        }
    }
}
