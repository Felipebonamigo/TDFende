using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Paleta única do jogo — fonte da verdade para TODA cor.
    /// Estilo "cartoon colorido" (decidido 25/09/2026, trocando o anterior "minimalista
    /// escuro"): céu e chão claros, cores primárias/secundárias saturadas, sem preto
    /// puro em lugar nenhum — sombra de cartoon é cor escura, nunca cinza morto.
    /// Ainda R$0: só paleta + luz + juice, sem asset comprado. Mudar uma cor aqui muda
    /// o jogo inteiro, porque quase tudo (torres, inimigos, partículas, fronteira,
    /// ghost) lê só daqui.
    /// </summary>
    public static class Palette
    {
        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        /// <summary>
        /// Cor para gravar em vértice de malha (Mesh.SetColors). Material, luz e partícula
        /// o Unity converte sozinho para o espaço Linear; cor de vértice de malha ele NÃO
        /// converte — sem isto, toda sobreposição sai lavada, mais clara que a paleta.
        /// </summary>
        public static Color ForVertex(Color c) =>
            QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;

        // Cenário — céu e grama claros, tipo tabuleiro de brinquedo
        public static readonly Color GroundDark = Hex("#6BC94A");
        public static readonly Color GroundLight = Hex("#7ED957");
        public static readonly Color Background = Hex("#8ED2FF"); // céu
        public static readonly Color Ambient = Hex("#BFE8FF");    // preenchimento frio claro, nunca escuro
        public static readonly Color SunColor = Hex("#FFE38A");
        public static readonly Color FillLight = Hex("#8FD6FF");

        // Entidades — cores de brinquedo, saturadas
        public static readonly Color BaseGold = Hex("#FFC93C");
        public static readonly Color SpawnMagenta = Hex("#FF6FB0");
        public static readonly Color TowerBody = Hex("#3D8BFF");
        public static readonly Color TowerHead = Hex("#FFD23F"); // acento amarelo: "boca" do canhão salta contra o corpo
        public static readonly Color Projectile = Hex("#FFEA70");

        // Inimigos — cor vai de EnemyFull a EnemyHurt conforme perde vida
        public static readonly Color EnemyFull = Hex("#FF5A3C");
        public static readonly Color EnemyHurt = Hex("#8A2A2A"); // machucado = vinho escuro, não cinza morto
        public static readonly Color EnemyDrained = Hex("#4FD6E8"); // tingimento sob atrito

        // Território / fronteira — mesmo ciano do atrito, mais vívido
        public static readonly Color TerritoryFill = new Color(0.31f, 0.87f, 0.91f, 0.16f);
        public static readonly Color TerritoryEdge = new Color(0.24f, 0.88f, 1f, 0.78f);

        // Feedback
        public static readonly Color GhostValid = Hex("#4FD98A");
        public static readonly Color GhostInvalid = Hex("#FF5A3C");
        public static readonly Color TextGold = Hex("#FFC93C");
        public static readonly Color TextDanger = Hex("#FF4C4C");
        public static readonly Color HitFlash = Color.white;
    }
}
