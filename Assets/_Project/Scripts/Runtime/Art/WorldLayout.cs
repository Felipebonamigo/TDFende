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
            /// <summary>Cor de quem defende (muralha) e de quem ataca (acampamento). Alfa 0 = padrão.</summary>
            public Color Owner, Attacker;
            /// <summary>Sentido da marcha no plano XZ, do acampamento para a fortaleza. Zero = +X.</summary>
            public Vector2 MarchDir;

            public Vector2 Center => (Min + Max) * 0.5f;
            public Vector2 Forward => MarchDir.sqrMagnitude > 0.0001f ? MarchDir.normalized : Vector2.right;
            /// <summary>Meio comprimento da lane no sentido da marcha e meia largura de través.</summary>
            public float HalfLength => Mathf.Abs(Vector2.Dot(Max - Min, Forward)) * 0.5f;
            public float HalfWidth => Mathf.Abs(Vector2.Dot(Max - Min, new Vector2(-Forward.y, Forward.x))) * 0.5f;
            /// <summary>Giro em Y que leva +X local para o sentido da marcha.</summary>
            public float Yaw => -Mathf.Atan2(Forward.y, Forward.x) * Mathf.Rad2Deg;
        }

        public readonly List<Area> PlayAreas = new List<Area>();
        /// <summary>Rio correndo em X entre as lanes (Tower Wars). Z do leito.</summary>
        public bool River;
        /// <summary>Coordenada de través do leito: Z se o rio corre em X, X se corre em Z.</summary>
        public float RiverZ;
        /// <summary>Rio correndo em Z (lanes lado a lado em X, marcha de cima para baixo).</summary>
        public bool RiverAlongZ;

        /// <summary>Distância com sinal até o eixo do rio, em qualquer das duas orientações.</summary>
        public float RiverOffset(float x, float z) =>
            RiverAlongZ ? x - RiverCenter(z) : z - RiverCenter(x);

        Vector3 RiverPoint(float along, float across, float y) =>
            RiverAlongZ ? new Vector3(across, y, along) : new Vector3(along, y, across);
        public int Seed = 7;

        /// <summary>
        /// Altura do chão onde o cenário assenta. Nulo = o relevo deste layout; com o
        /// terreno do GroundBuilder por baixo, é a altura DELE (Terrain.SampleHeight).
        /// </summary>
        public System.Func<float, float, float> GroundHeight;

        float Ground(float x, float z) => GroundHeight != null ? GroundHeight(x, z) : Height(x, z);

        public const float WaterY = -0.16f;
        const float Extent = 80f;

        public void AddPlayArea(Vector3 center, Vector3 size, Color owner = default, Color attacker = default,
            Vector2 marchDir = default)
        {
            PlayAreas.Add(new Area
            {
                Owner = owner, Attacker = attacker, MarchDir = marchDir,
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
                float d = Mathf.Abs(RiverOffset(x, z));
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
                if (RiverAlongZ) mb.GroundQuad(ArtMat.Water, RiverPoint(x + 2f, zc, WaterY), 2.5f, 2.02f);
                else mb.GroundQuad(ArtMat.Water, RiverPoint(x + 2f, zc, WaterY), 2.02f, 2.5f);
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
        public enum PropKind { Pine, Oak, Bush, Boulder, Stump }

        /// <summary>Um item de cenário: onde, qual tipo, que tamanho, e a semente da variação.</summary>
        public struct Prop
        {
            public PropKind Kind;
            public Vector3 Pos;
            public float Size;
            public int Seed;
        }

        /// <summary>
        /// Onde vai cada árvore, arbusto e pedra. Separado de COMO desenhar: a vista usa
        /// modelo baixado quando existe (pedra, toco da Poly Haven) e procedural no resto.
        /// Mais denso longe — emoldura o tabuleiro sem tapar a jogada; perto da borda,
        /// só coisa baixa.
        /// </summary>
        public List<Prop> ScatterScenery()
        {
            var props = new List<Prop>();
            void Add(PropKind k, Vector3 p, float size, int seed) =>
                props.Add(new Prop { Kind = k, Pos = p, Size = size, Seed = seed });

            for (int i = 0; i < 560; i++)
            {
                float x = (ProcNoise.Hash(i, 0, Seed) * 2f - 1f) * (Extent - 6f);
                float z = (ProcNoise.Hash(i, 1, Seed) * 2f - 1f) * (Extent - 6f);
                float edge = DistanceToPlay(x, z);
                if (edge < 1.2f) continue;
                if (River && Mathf.Abs(RiverOffset(x, z)) < 2.9f) continue;
                // chance de existir cresce com a distância: clareira perto, mata fechada longe
                float chance = Mathf.Min(0.85f, 0.06f + edge / 30f);
                float roll = ProcNoise.Hash(i, 2, Seed);
                if (roll > chance) continue;

                float y = Ground(x, z) - 0.02f;
                float kind = ProcNoise.Hash(i, 3, Seed);
                float size = 0.8f + 0.7f * ProcNoise.Hash(i, 4, Seed);
                var at = new Vector3(x, y, z);
                bool nearBoard = edge < 4f;
                if (nearBoard || kind < 0.18f)
                {
                    if (ProcNoise.Hash(i, 5, Seed) < 0.5f) Add(PropKind.Bush, at, size * 0.9f, i);
                    else Add(PropKind.Boulder, at, 0.18f + 0.22f * size * (nearBoard ? 0.6f : 1f), i);
                }
                else if (kind < 0.22f) Add(PropKind.Stump, at, 0.2f + 0.1f * size, i);
                else if (kind < 0.62f) Add(PropKind.Pine, at, size * 1.35f, i);
                else Add(PropKind.Oak, at, size * 1.2f, i);
            }
            // pedras soltas nas margens do rio
            if (River)
            {
                for (int i = 0; i < 90; i++)
                {
                    float along = (ProcNoise.Hash(i, 10, Seed) * 2f - 1f) * (Extent - 10f);
                    float side = ProcNoise.Hash(i, 11, Seed) < 0.5f ? -1f : 1f;
                    var p = RiverPoint(along, RiverCenter(along) + side * (1.7f + 0.8f * ProcNoise.Hash(i, 12, Seed)), 0f);
                    float x = p.x, z = p.z;
                    if (DistanceToPlay(x, z) < 0.6f) continue;
                    Add(PropKind.Boulder, new Vector3(x, Ground(x, z) - 0.03f, z), 0.1f + 0.14f * ProcNoise.Hash(i, 13, Seed), i + 500);
                }
            }
            return props;
        }

        /// <summary>Desenha em código os itens que não ganharam modelo baixado.</summary>
        public static MeshBuilder BuildScenery(IEnumerable<Prop> props)
        {
            var mb = new MeshBuilder();
            foreach (var p in props)
            {
                switch (p.Kind)
                {
                    case PropKind.Pine: ModelLib.Pine(mb, p.Pos, p.Size, p.Seed); break;
                    case PropKind.Oak: ModelLib.Oak(mb, p.Pos, p.Size, p.Seed); break;
                    case PropKind.Bush: ModelLib.Bush(mb, p.Pos, p.Size, p.Seed); break;
                    case PropKind.Boulder: ModelLib.Boulder(mb, p.Pos, p.Size, p.Seed); break;
                    // toco procedural: tronco curto cortado
                    case PropKind.Stump:
                        mb.SetTransform(p.Pos, Quaternion.Euler(0f, p.Seed * 37f, 0f));
                        mb.Cylinder(ArtMat.Bark, Vector3.zero, p.Size * 0.55f, p.Size * 0.45f, p.Size * 0.7f, 9, capBottom: false);
                        mb.ResetTransform();
                        break;
                }
            }
            return mb;
        }

        /// <summary>Tudo procedural (o preview fora do Unity usa esta).</summary>
        public MeshBuilder BuildScenery() => BuildScenery(ScatterScenery());
    }
}
