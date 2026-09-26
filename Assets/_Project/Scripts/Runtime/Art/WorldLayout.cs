using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// O mundo em volta do tabuleiro, em lógica pura: relevo, rio, mureta e cenário.
    /// O tabuleiro jogável fica plano (Y = 0) — o grid e o pathfinding continuam no
    /// plano de sempre; o relevo só começa longe da borda, onde ninguém constrói.
    /// </summary>
    public sealed class WorldLayout
    {
        public struct Area
        {
            public Vector2 Min, Max; // XZ de mundo
        }

        public readonly List<Area> PlayAreas = new List<Area>();
        /// <summary>Rio correndo em X entre as lanes (Tower Wars). Z do leito.</summary>
        public bool River;
        public float RiverZ;
        public int Seed = 7;

        public const float WaterY = -0.16f;
        const float Extent = 80f;

        public void AddPlayArea(Vector3 center, Vector3 size)
        {
            PlayAreas.Add(new Area
            {
                Min = new Vector2(center.x - size.x * 0.5f, center.z - size.z * 0.5f),
                Max = new Vector2(center.x + size.x * 0.5f, center.z + size.z * 0.5f),
            });
        }

        /// <summary>Distância (plana) até a área jogável mais próxima; 0 dentro dela.</summary>
        public float DistanceToPlay(float x, float z)
        {
            float best = float.MaxValue;
            foreach (var a in PlayAreas)
            {
                float dx = Mathf.Max(a.Min.x - x, 0f, x - a.Max.x);
                float dz = Mathf.Max(a.Min.y - z, 0f, z - a.Max.y);
                best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dz * dz));
            }
            return best;
        }

        public float RiverCenter(float x) => RiverZ + 0.35f * Mathf.Sin(x * 0.17f);

        public float Height(float x, float z)
        {
            float h = 0f;
            float riverFade = 1f;
            if (River)
            {
                float d = Mathf.Abs(z - RiverCenter(x));
                h -= 0.42f * (1f - Smooth((d - 1.1f) / 1.4f));
                riverFade = Smooth((d - 3f) / 5f); // morro não nasce dentro do vale do rio
            }
            float edge = DistanceToPlay(x, z);
            float hills = ProcNoise.Value(x * 0.045f + 50f, z * 0.045f + 50f, 1024, Seed) * 0.7f
                          + ProcNoise.Value(x * 0.11f + 50f, z * 0.11f + 50f, 1024, Seed + 1) * 0.3f;
            h += (hills * 3.2f - 0.6f) * Smooth((edge - 5f) / 22f) * riverFade;
            return h;
        }

        static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        public MeshBuilder BuildTerrain()
        {
            var mb = new MeshBuilder();
            mb.Heightfield(ArtMat.Grass, ArtMat.Dirt, -0.1f, new Vector3(-Extent, 0f, -Extent),
                Extent * 2f, Extent * 2f, 160, 160, Height);
            return mb;
        }

        public MeshBuilder BuildWater()
        {
            var mb = new MeshBuilder();
            if (!River) return mb;
            // faixas curtas acompanham a curva do rio; a margem de grama cobre a sobra
            for (float x = -Extent; x < Extent; x += 4f)
            {
                float zc = RiverCenter(x + 2f);
                mb.GroundQuad(ArtMat.Water, new Vector3(x + 2f, WaterY, zc), 2.02f, 2.5f);
            }
            return mb;
        }

        /// <summary>Mureta baixa de pedra seca em volta de cada lane: delimita o tabuleiro sem cerca de brinquedo.</summary>
        public MeshBuilder BuildCurbs()
        {
            var mb = new MeshBuilder();
            int n = 0;
            foreach (var a in PlayAreas)
            {
                const float off = 0.14f;
                var corners = new[]
                {
                    new Vector2(a.Min.x - off, a.Min.y - off), new Vector2(a.Max.x + off, a.Min.y - off),
                    new Vector2(a.Max.x + off, a.Max.y + off), new Vector2(a.Min.x - off, a.Max.y + off),
                };
                for (int side = 0; side < 4; side++)
                {
                    var p0 = corners[side];
                    var p1 = corners[(side + 1) % 4];
                    float len = (p1 - p0).magnitude;
                    int stones = Mathf.Max(1, Mathf.RoundToInt(len / 0.42f));
                    var dir = (p1 - p0) / len;
                    float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                    for (int s = 0; s < stones; s++)
                    {
                        float t = (s + 0.5f) / stones;
                        var p = Vector2.Lerp(p0, p1, t);
                        float h = 0.09f + 0.06f * ProcNoise.Hash(n, s, Seed);
                        float w = 0.16f + 0.05f * ProcNoise.Hash(n, s, Seed + 1);
                        float jitter = (ProcNoise.Hash(n, s, Seed + 2) - 0.5f) * 10f;
                        mb.Box(ArtMat.Rock, new Vector3(p.x, h * 0.5f - 0.01f, p.y),
                            new Vector3(w, h, len / stones * 1.04f), Quaternion.Euler(0f, yaw + jitter, 0f));
                    }
                    n++;
                }
            }
            return mb;
        }

        /// <summary>
        /// Árvores, arbustos e pedras espalhados fora do tabuleiro. Mais denso longe:
        /// emoldura o tabuleiro sem tapar a jogada. Perto da borda, só coisa baixa.
        /// </summary>
        public MeshBuilder BuildScenery()
        {
            var mb = new MeshBuilder();
            for (int i = 0; i < 560; i++)
            {
                float x = (ProcNoise.Hash(i, 0, Seed) * 2f - 1f) * (Extent - 6f);
                float z = (ProcNoise.Hash(i, 1, Seed) * 2f - 1f) * (Extent - 6f);
                float edge = DistanceToPlay(x, z);
                if (edge < 1.2f) continue;
                if (River && Mathf.Abs(z - RiverCenter(x)) < 2.9f) continue;
                // chance de existir cresce com a distância: clareira perto, mata fechada longe
                float chance = Mathf.Min(0.85f, 0.06f + edge / 30f);
                float roll = ProcNoise.Hash(i, 2, Seed);
                if (roll > chance) continue;

                float y = Height(x, z) - 0.02f;
                float kind = ProcNoise.Hash(i, 3, Seed);
                float size = 0.8f + 0.7f * ProcNoise.Hash(i, 4, Seed);
                var at = new Vector3(x, y, z);
                bool nearBoard = edge < 4f;
                if (nearBoard || kind < 0.18f)
                {
                    if (ProcNoise.Hash(i, 5, Seed) < 0.5f) ModelLib.Bush(mb, at, size * 0.9f, i);
                    else ModelLib.Boulder(mb, at, 0.18f + 0.22f * size * (nearBoard ? 0.6f : 1f), i);
                }
                else if (kind < 0.62f) ModelLib.Pine(mb, at, size * 1.35f, i);
                else ModelLib.Oak(mb, at, size * 1.2f, i);
            }
            // pedras soltas nas margens do rio
            if (River)
            {
                for (int i = 0; i < 90; i++)
                {
                    float x = (ProcNoise.Hash(i, 10, Seed) * 2f - 1f) * (Extent - 10f);
                    float side = ProcNoise.Hash(i, 11, Seed) < 0.5f ? -1f : 1f;
                    float z = RiverCenter(x) + side * (1.7f + 0.8f * ProcNoise.Hash(i, 12, Seed));
                    if (DistanceToPlay(x, z) < 0.6f) continue;
                    ModelLib.Boulder(mb, new Vector3(x, Height(x, z) - 0.03f, z), 0.1f + 0.14f * ProcNoise.Hash(i, 13, Seed), i + 500);
                }
            }
            return mb;
        }
    }
}
