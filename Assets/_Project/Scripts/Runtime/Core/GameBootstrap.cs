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
            if (Object.FindFirstObjectByType<TowerWarsController>() != null) return;
            if (Object.FindFirstObjectByType<ModeSelect>() != null) return;

            // Antes de qualquer partida: a primeira lane tranca os catálogos, então
            // este é o único momento em que um arquivo de balanceamento ainda vale.
            CatalogLoader.LoadIfPresent();

            new GameObject("== TDFende ==").AddComponent<ModeSelect>();
        }
    }
}
