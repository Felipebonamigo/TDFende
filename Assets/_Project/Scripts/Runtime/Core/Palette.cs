using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Cores do jogo que NÃO vêm de material: time, fronteira, efeitos, interface.
    ///
    /// Direção de arte realista (decidida 26/09/2026, trocando o "cartoon colorido"):
    /// a cor das coisas agora mora nas texturas PBR (ArtMat/ProcTex) — pedra, madeira,
    /// bronze, relva. Aqui ficam só as cores de LEITURA de jogo: de quem é cada coisa
    /// (azul você, vermelho a IA, como estandarte medieval) e o que está acontecendo
    /// (fronteira, atrito, dano). Saturação contida: realista não é cinza, mas também
    /// não é brinquedo.
    /// </summary>
    public static class Palette
    {
        static Color Hex(string hex, float a = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            c.a = a;
            return c;
        }

        // Times — tingem estandarte, tabardo, xairel e a linha de fronteira
        public static readonly Color TeamPlayer = Hex("#2F5FA8");
        public static readonly Color TeamFoe = Hex("#A8322F");

        // Território (estilo RoN: preenchimento quase invisível, linha de fronteira firme)
        public static Color TerritoryFill(Color team) => new Color(team.r, team.g, team.b, 0.10f);
        public static Color TerritoryEdge(Color team) =>
            new Color(Mathf.Lerp(team.r, 1f, 0.25f), Mathf.Lerp(team.g, 1f, 0.25f), Mathf.Lerp(team.b, 1f, 0.25f), 0.85f);

        /// <summary>Atrito: o inimigo drenado ganha um brilho gelado (emissão).</summary>
        public static readonly Color AttritionGlow = new Color(0.18f, 0.42f, 0.55f);
        /// <summary>Clarão do impacto no inimigo (emissão).</summary>
        public static readonly Color HitGlow = new Color(0.55f, 0.5f, 0.42f);

        // Construção
        public static readonly Color GhostValid = Hex("#6FCF7A", 0.45f);
        public static readonly Color GhostInvalid = Hex("#E0523C", 0.45f);
        public static readonly Color GhostUpgrade = Hex("#E8C15A", 0.45f);
        public static readonly Color RangeRing = Hex("#F4EBD0", 0.55f);
        public static readonly Color GridLine = new Color(0f, 0f, 0f, 0.09f);

        // Efeitos
        public static readonly Color MuzzleFlash = Hex("#FFD48A");
        public static readonly Color LeakFlash = Hex("#FF8A4C");
        public static readonly Color Smoke = Hex("#9A958D", 0.5f);
        public static readonly Color DarkSmoke = Hex("#3E3A36", 0.6f);
        public static readonly Color Dust = Hex("#8C7A5E", 0.55f);
        public static readonly Color Debris = Hex("#3B332B", 0.95f);
        public static readonly Color Spark = Hex("#FFB050");
        public static readonly Color Frost = Hex("#A8E4FF");
        public static readonly Color FrostMist = Hex("#D8F1FF", 0.35f);

        // Texto flutuante e interface
        public static readonly Color TextGold = Hex("#E8C15A");
        public static readonly Color TextDanger = Hex("#E0523C");
        public static readonly Color TextFrost = Hex("#8FD3F0");
        public static readonly Color UiInk = Hex("#EDE3CF");
        public static readonly Color UiInkDim = Hex("#B9AE98");
        public static readonly Color UiPanel = Hex("#17130F", 0.78f);
        public static readonly Color UiButton = Hex("#3A2C20", 0.92f);
        public static readonly Color UiButtonHover = Hex("#54402C", 0.95f);
        public static readonly Color UiButtonSelected = Hex("#7A5A2E", 0.97f);
        public static readonly Color UiAccent = Hex("#C9A24A");
    }
}
