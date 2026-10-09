using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Chão realista: Terrain nativo do Unity com duas camadas PBR da Poly Haven (CC0).
    ///
    ///   - dentro das áreas de jogo: grama rasteira fotografada DE CIMA (aerial_grass_rock),
    ///     que é exatamente o ângulo da câmera;
    ///   - fora: grama cheia (leafy_grass), com manchas da outra camada misturadas por ruído.
    ///
    /// A mistura por ruído é o que tira a cara de "chão falso": uma textura só, ladrilhada
    /// num plano grande e vista de cima, denuncia a repetição na hora.
    ///
    /// Nas áreas de jogo (e numa margem em volta) o chão é plano em y=0 EXATO — a simulação,
    /// o raycast do mouse e as sobreposições (fronteira, grid, fantasma) assumem esse plano.
    /// Fora, uma ondulação leve dá volume sem nunca subir por cima da lane.
    ///
    /// Se algum arquivo de arte faltar, devolve null e quem chamou mantém o chão antigo:
    /// o jogo nunca deixa de rodar por causa de asset.
    /// </summary>
    public static class GroundBuilder
    {
        const string LaneTex = "Art/Ground/aerial_grass_rock";
        const string FieldTex = "Art/Ground/leafy_grass";

        /// <summary>
        /// Lado do terreno. Com a câmera inclinada a 55°, o chão visível vai a ~80 unidades
        /// do centro no zoom máximo; a névoa esconde a borda antes disso.
        /// </summary>
        public const float WorldSize = 220f;

        const int HeightRes = 129;  // mínimo razoável; o relevo é suave
        const int AlphaRes = 512;   // ~0,43 unidade por texel: borda da lane nítida
        const float MaxHeight = 2f;
        const float BumpHeight = 0.35f; // ondulação máxima fora das lanes, em unidades
        // Plano garantido em volta de cada área de jogo. Tem que passar da diagonal do
        // triângulo do terreno no LOD 2 (6,9 × √2 ≈ 9,8): com margem menor, um triângulo
        // grosso que cruza a borda da lane herda altura de fora e cobre o giz (y=0,012).
        const float FlatMargin = 10f;
        const float BumpRamp = 5f;      // distância em que a ondulação chega ao máximo

        public static Terrain Build(IReadOnlyList<Rect> playAreas, int seed = 7)
        {
            var laneDiff = Resources.Load<Texture2D>(LaneTex + "_diff_2k");
            var laneNor = Resources.Load<Texture2D>(LaneTex + "_nor_gl_2k");
            var fieldDiff = Resources.Load<Texture2D>(FieldTex + "_diff_2k");
            var fieldNor = Resources.Load<Texture2D>(FieldTex + "_nor_gl_2k");
            if (laneDiff == null || fieldDiff == null)
            {
                Debug.LogWarning("[TDFende] texturas de chão não encontradas em Resources/Art/Ground; " +
                                 "usando o chão antigo.");
                return null;
            }

            var data = new TerrainData();
            // resolução ANTES do tamanho: mudar a resolução reescala o terreno
            data.heightmapResolution = HeightRes;
            data.alphamapResolution = AlphaRes;
            data.size = new Vector3(WorldSize, MaxHeight, WorldSize);

            var origin = new Vector3(-WorldSize * 0.5f, 0f, -WorldSize * 0.5f);
            float noiseOffX = seed * 13.1f, noiseOffZ = seed * 7.7f;

            // ---- relevo ----
            var heights = new float[HeightRes, HeightRes];
            for (int iz = 0; iz < HeightRes; iz++)
            for (int ix = 0; ix < HeightRes; ix++)
            {
                float wx = origin.x + ix / (float)(HeightRes - 1) * WorldSize;
                float wz = origin.z + iz / (float)(HeightRes - 1) * WorldSize;
                float d = DistanceToAreas(playAreas, wx, wz);
                float ramp = Mathf.Clamp01((d - FlatMargin) / BumpRamp);
                if (ramp <= 0f) continue; // plano exato perto das lanes

                float n = Mathf.PerlinNoise(wx * 0.045f + noiseOffX, wz * 0.045f + noiseOffZ) * 0.7f
                        + Mathf.PerlinNoise(wx * 0.13f + noiseOffX, wz * 0.13f + noiseOffZ) * 0.3f;
                heights[iz, ix] = n * ramp * (BumpHeight / MaxHeight);
            }
            data.SetHeights(0, 0, heights);

            // ---- camadas ----
            data.terrainLayers = new[]
            {
                new TerrainLayer
                {
                    diffuseTexture = laneDiff, normalMapTexture = laneNor,
                    tileSize = new Vector2(4f, 4f), smoothness = 0.05f, metallic = 0f
                },
                new TerrainLayer
                {
                    diffuseTexture = fieldDiff, normalMapTexture = fieldNor,
                    tileSize = new Vector2(3f, 3f), smoothness = 0.08f, metallic = 0f
                }
            };

            var alpha = new float[AlphaRes, AlphaRes, 2];
            for (int iz = 0; iz < AlphaRes; iz++)
            for (int ix = 0; ix < AlphaRes; ix++)
            {
                float wx = origin.x + (ix + 0.5f) / AlphaRes * WorldSize;
                float wz = origin.z + (iz + 0.5f) / AlphaRes * WorldSize;
                float d = DistanceToAreas(playAreas, wx, wz);

                // dentro da lane: grama rasteira; borda suave de ~0,6 unidade
                float lane = 1f - Mathf.SmoothStep(0f, 0.6f, d);

                // fora: manchas de grama rasteira no meio da cheia, para quebrar a repetição
                float patch = Mathf.PerlinNoise(wx * 0.07f + noiseOffZ, wz * 0.07f + noiseOffX);
                float patchW = Mathf.SmoothStep(0.55f, 0.75f, patch) * 0.65f;

                float w0 = Mathf.Max(lane, patchW);
                alpha[iz, ix, 0] = w0;
                alpha[iz, ix, 1] = 1f - w0;
            }
            data.SetAlphamaps(0, 0, alpha);

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terreno";
            go.transform.position = origin;

            // ninguém usa física: o clique é raio contra plano matemático
            var col = go.GetComponent<TerrainCollider>();
            if (col != null) col.enabled = false;

            var terrain = go.GetComponent<Terrain>();
            var terrainShader = ShaderRefs.TerrainLit;
            if (terrainShader != null)
            {
                var mat = new Material(terrainShader);
                // Com drawInstanced, sem esta keyword a luz usa a normal POR VÉRTICE do
                // heightmap (1 vértice a cada 1,7 unidade). Quem liga é o inspector do
                // material (TerrainLitShaderGUI), que material criado em código não passa.
                mat.EnableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
                terrain.materialTemplate = mat;
            }
            // Sem instancing: no executável o terreno instanciado sai todo PRETO (no editor não),
            // e nenhuma variante guardada no build resolveu (BuildJogo/ShaderKeep). Visto com o
            // print do "-captura" (SmokeCapture), 06/10/2026. O custo é a luz pela normal por
            // vértice, que só aparece no relevo longe das lanes — perto delas o chão é plano.
            terrain.drawInstanced = false;
            terrain.basemapDistance = 1000f;   // textura cheia em todo o campo visível
            terrain.heightmapPixelError = 2f;
            return terrain;
        }

        /// <summary>Distância (XZ) do ponto até a área de jogo mais próxima; 0 dentro de uma.</summary>
        public static float DistanceToAreas(IReadOnlyList<Rect> areas, float x, float z)
        {
            float best = float.MaxValue;
            for (int i = 0; i < areas.Count; i++)
            {
                var r = areas[i];
                float dx = Mathf.Max(r.xMin - x, 0f, x - r.xMax);
                float dz = Mathf.Max(r.yMin - z, 0f, z - r.yMax); // Rect.y guarda o Z do mundo
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < best) best = d;
            }
            return best;
        }
    }
}
