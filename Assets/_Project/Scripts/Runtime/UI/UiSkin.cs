using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Pele única do HUD em OnGUI: painel escuro translúcido, texto cor de pergaminho,
    /// botão de madeira escura com acento dourado no selecionado. Tudo gerado em código
    /// (textura de 1 pixel por cor) — o HUD continua provisório, mas não destoa mais do
    /// mundo realista atrás dele.
    /// </summary>
    public static class UiSkin
    {
        public static GUIStyle Label, LabelSmall, Plain, Big, Title, Subtitle, Panel, Button, ButtonSelected, Floating;

        public static void Ensure()
        {
            if (Label != null) return;

            Label = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, richText = true };
            Ink(Label, Palette.UiInk);

            LabelSmall = new GUIStyle(Label) { fontSize = 12, fontStyle = FontStyle.Normal };
            Ink(LabelSmall, Palette.UiInkDim);

            // tinta branca: estes recebem a cor pelo GUI.color do Shadowed
            Plain = new GUIStyle(Label);
            Ink(Plain, Color.white);
            Big = new GUIStyle(Plain) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
            Title = new GUIStyle(Plain) { fontSize = 44, alignment = TextAnchor.MiddleCenter };
            Subtitle = new GUIStyle(Plain) { fontSize = 14, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter };

            Panel = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13, alignment = TextAnchor.UpperLeft, wordWrap = true, richText = true,
                padding = new RectOffset(10, 10, 8, 8),
            };
            Panel.normal.background = Solid(Palette.UiPanel);
            Ink(Panel, Palette.UiInk);

            Button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13, fontStyle = FontStyle.Bold, richText = true, wordWrap = true,
                padding = new RectOffset(6, 6, 4, 4), border = new RectOffset(2, 2, 2, 2),
            };
            Button.normal.background = Framed(Palette.UiButton, Palette.UiButtonHover);
            Button.hover.background = Framed(Palette.UiButtonHover, Palette.UiAccent);
            Button.active.background = Framed(Palette.UiButtonSelected, Palette.UiAccent);
            Button.focused.background = Button.normal.background;
            Ink(Button, Palette.UiInk);

            ButtonSelected = new GUIStyle(Button);
            ButtonSelected.normal.background = Framed(Palette.UiButtonSelected, Palette.UiAccent);
            ButtonSelected.hover.background = ButtonSelected.normal.background;
            Ink(ButtonSelected, Color.white);

            Floating = new GUIStyle(Plain) { fontSize = 17, alignment = TextAnchor.MiddleCenter };
        }

        /// <summary>Texto com sombra: legível sobre relva clara e sobre pedra escura. Use estilo de tinta branca.</summary>
        public static void Shadowed(Rect r, string text, GUIStyle style, Color color)
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, color.a * 0.75f);
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), text, style);
            GUI.color = color;
            GUI.Label(r, text, style);
            GUI.color = prev;
        }

        static void Ink(GUIStyle s, Color c)
        {
            s.normal.textColor = s.hover.textColor = s.active.textColor = s.focused.textColor = c;
        }

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        /// <summary>Fundo 6x6 com moldura de 1 px — com <c>border</c> 2, o Unity estica só o miolo.</summary>
        static Texture2D Framed(Color fill, Color edge)
        {
            const int s = 6;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false)
            { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point };
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                t.SetPixel(x, y, x == 0 || y == 0 || x == s - 1 || y == s - 1 ? edge : fill);
            t.Apply();
            return t;
        }
    }
}
