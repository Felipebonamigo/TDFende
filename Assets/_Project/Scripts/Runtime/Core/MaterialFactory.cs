using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TDFende
{
    /// <summary>
    /// Materiais placeholder por cor, criados em runtime.
    /// Detecta o pipeline ativo (URP ou Built-in) e escolhe o shader certo —
    /// nada de material rosa em nenhum dos dois.
    /// </summary>
    public static class MaterialFactory
    {
        static readonly Dictionary<Color, Material> Cache = new Dictionary<Color, Material>();
        static Shader _shader;
        static int _colorProp = -1;

        /// <summary>Id da propriedade de cor do shader ativo (_BaseColor no URP, _Color no Built-in).</summary>
        public static int ColorProperty
        {
            get
            {
                Ensure();
                return _colorProp;
            }
        }

        static void Ensure()
        {
            if (_shader != null) return;
            bool urp = GraphicsSettings.currentRenderPipeline != null;
            _shader = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
            _colorProp = Shader.PropertyToID(urp ? "_BaseColor" : "_Color");
        }

        public static Material Get(Color color)
        {
            Ensure();
            if (Cache.TryGetValue(color, out var mat) && mat != null) return mat;
            mat = new Material(_shader) { enableInstancing = true };
            mat.SetColor(_colorProp, color);
            Cache[color] = mat;
            return mat;
        }

        /// <summary>Material do chão com textura quadriculada gerada em código (1 célula por quadrado).</summary>
        public static Material GetGround(Color a, Color b, int tilesX, int tilesY)
        {
            Ensure();
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            tex.SetPixels(new[] { a, b, b, a });
            tex.Apply();

            return new Material(_shader)
            {
                mainTexture = tex,
                mainTextureScale = new Vector2(tilesX / 2f, tilesY / 2f)
            };
        }
    }
}
