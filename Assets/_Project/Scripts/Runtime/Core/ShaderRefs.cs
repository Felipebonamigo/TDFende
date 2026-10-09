using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TDFende
{
    /// <summary>
    /// Os shaders que o código acha por nome — a lista única. O jogo monta os materiais em
    /// código, então nenhum material de cena referencia estes shaders e o build os descartaria.
    /// O Editor/BuildJogo cria um material do ShaderKeep para cada nome daqui (com cada
    /// combinação de keywords que o código liga), e o código pega o shader daqui em vez de
    /// espalhar Shader.Find. Shader novo: acrescente aqui e em BuildJogo.Shaders.
    /// </summary>
    public static class ShaderRefs
    {
        public const string LitName = "Universal Render Pipeline/Lit";
        public const string TerrainLitName = "Universal Render Pipeline/Terrain/Lit";
        public const string SkyboxPanoramicName = "Skybox/Panoramic";
        public const string SkyboxProceduralName = "Skybox/Procedural";
        public const string SoftParticleName = "TDFende/SoftParticle";
        public const string TerritoryOverlayName = "TDFende/TerritoryOverlay";

        /// <summary>Lit do URP; "Standard" só se o projeto rodar sem pipeline (pipeline embutido).</summary>
        public static Shader Lit => Get(GraphicsSettings.currentRenderPipeline != null ? LitName : "Standard");
        public static Shader TerrainLit => Get(TerrainLitName);
        public static Shader SkyboxPanoramic => Get(SkyboxPanoramicName);
        public static Shader SkyboxProcedural => Get(SkyboxProceduralName);
        public static Shader SoftParticle => Get(SoftParticleName);
        public static Shader TerritoryOverlay => Get(TerritoryOverlayName);

        static readonly Dictionary<string, Shader> Cache = new Dictionary<string, Shader>();

        static Shader Get(string name)
        {
            if (Cache.TryGetValue(name, out var shader) && shader != null) return shader;
            shader = Shader.Find(name);
            if (shader == null)
                Debug.LogError($"[TDFende] shader '{name}' fora do build: falta material dele no ShaderKeep (BuildJogo.Shaders)");
            Cache[name] = shader;
            return shader;
        }
    }
}
