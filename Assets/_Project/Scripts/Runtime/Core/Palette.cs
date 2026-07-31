using UnityEngine;

namespace FrontierTD
{
    /// <summary>
    /// Paleta única do jogo — fonte da verdade para TODA cor.
    /// Estilo "minimalista deliberado": sem asset comprado, o visual vem de
    /// paleta coerente + luz + juice. Mudar uma cor aqui muda o jogo inteiro.
    /// </summary>
    public static class Palette
    {
        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        // Cenário — azul-acinzentado escuro, para os acentos saltarem
        public static readonly Color GroundDark = Hex("#252C3A");
        public static readonly Color GroundLight = Hex("#2D3547");
        public static readonly Color Background = Hex("#151A24");
        public static readonly Color Ambient = Hex("#3A4358");
        public static readonly Color SunColor = Hex("#FFE9C4");
        public static readonly Color FillLight = Hex("#4A6FA5");

        // Entidades
        public static readonly Color BaseGold = Hex("#F2B33D");
        public static readonly Color SpawnMagenta = Hex("#C2418F");
        public static readonly Color TowerBody = Hex("#3E5C8A");
        public static readonly Color TowerHead = Hex("#7FB2E8");
        public static readonly Color Projectile = Hex("#FFE066");

        // Inimigos — cor vai de EnemyFull a EnemyHurt conforme perde vida
        public static readonly Color EnemyFull = Hex("#E8503A");
        public static readonly Color EnemyHurt = Hex("#4A1510");
        public static readonly Color EnemyDrained = Hex("#2E8FA8"); // tingimento sob atrito

        // Território / fronteira
        public static readonly Color TerritoryFill = new Color(0.31f, 0.76f, 0.97f, 0.13f);
        public static readonly Color TerritoryEdge = new Color(0.45f, 0.90f, 1f, 0.70f);

        // Feedback
        public static readonly Color GhostValid = Hex("#4FD98A");
        public static readonly Color GhostInvalid = Hex("#E8503A");
        public static readonly Color TextGold = Hex("#F2B33D");
        public static readonly Color TextDanger = Hex("#FF6B5A");
        public static readonly Color HitFlash = Color.white;
    }
}
