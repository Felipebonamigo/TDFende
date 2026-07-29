using UnityEngine;

namespace FrontierTD
{
    /// <summary>
    /// Abstração de input: o jogo consome intenções, nunca cliques ou teclas diretamente.
    /// É o que mantém a porta do mobile aberta — na fase 5 entra uma TouchInput
    /// implementando esta mesma interface (pinça = ZoomDelta, arrastar = DragPanDelta etc.)
    /// e nenhum outro arquivo do jogo muda.
    /// </summary>
    public interface IGameInput
    {
        /// <summary>Chamado uma vez por frame pelo GameController, antes de qualquer leitura.</summary>
        void Tick();

        Vector2 PanAxis { get; }      // -1..1 em cada eixo
        Vector2 DragPanDelta { get; } // pixels desde o último frame
        float ZoomDelta { get; }
        Vector2 PointerPos { get; }   // posição em tela
        bool PlacePressed { get; }
        bool CallWavePressed { get; }
        bool RestartPressed { get; }
    }
}
