using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Cria o jogo em QUALQUER cena ao dar Play — sem prefabs, sem cena montada à mão.
    /// Abrir o projeto e apertar Play é tudo que precisa.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<GameController>() != null) return;
            new GameObject("== TDFende ==").AddComponent<GameController>();
        }
    }
}
