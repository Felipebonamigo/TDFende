using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Todos os materiais do jogo. O modelo diz QUAL material cada face usa; a cor
    /// de time (bandeira, tabardo) é o único que muda por jogador.
    /// </summary>
    public enum ArtMat
    {
        Stone, StoneDark, StoneFrost, Wood, WoodDark, Iron, Bronze, Slate, RoofTile,
        Cloth, ClothTeam, Leather, Skin, HorseCoat, Ice, Foliage, Bark, Grass, Dirt, Rock, Water, Hair,
        Ember,
        // bichos que o atacante manda: pelagem, couro grosso, marfim, pena e bico
        FurGray, FurTan, FurBrown, FurOrange, Hide, Ivory, Feather, Beak,
    }

    /// <summary>Parâmetros PBR de um material. Cor em sRGB, como o Inspector mostraria.</summary>
    public struct MatSpec
    {
        public Color Base;
        public float Smoothness;
        public float Metallic;
        /// <summary>Quantas unidades de mundo uma repetição da textura cobre.</summary>
        public float UnitsPerTile;
        public Color Emission;
        public int Size;

        public static MatSpec Of(ArtMat m)
        {
            switch (m)
            {
                case ArtMat.Stone: return S("#928B7F", 0.18f, 0f, 0.75f);
                case ArtMat.StoneDark: return S("#6E6860", 0.15f, 0f, 0.75f);
                case ArtMat.StoneFrost: return S("#9AA7B0", 0.30f, 0f, 0.75f);
                case ArtMat.Wood: return S("#7C5A3A", 0.22f, 0f, 0.6f);
                case ArtMat.WoodDark: return S("#4E3624", 0.20f, 0f, 0.5f);
                case ArtMat.Iron: return S("#70737A", 0.45f, 0.7f, 0.5f);
                case ArtMat.Bronze: return S("#9A7440", 0.58f, 0.9f, 0.5f);
                case ArtMat.Slate: return S("#4E5A66", 0.35f, 0f, 0.6f);
                case ArtMat.RoofTile: return S("#9C4E36", 0.25f, 0f, 0.6f);
                case ArtMat.Cloth: return S("#D9D0BC", 0.10f, 0f, 0.3f);
                case ArtMat.ClothTeam: return S("#E8E4DA", 0.10f, 0f, 0.3f); // tingido pela cor do time
                case ArtMat.Leather: return S("#6E4B2E", 0.35f, 0f, 0.3f);
                case ArtMat.Skin: return S("#C99B7A", 0.30f, 0f, 0.3f);
                case ArtMat.HorseCoat: return S("#5C3B25", 0.30f, 0f, 0.4f);
                case ArtMat.Ice:
                    var ice = S("#BFE6F2", 0.92f, 0f, 0.5f);
                    ice.Emission = new Color(0.10f, 0.22f, 0.28f);
                    return ice;
                case ArtMat.Foliage: return S("#3F5B2B", 0.12f, 0f, 0.8f);
                case ArtMat.Bark: return S("#4B3B2D", 0.12f, 0f, 0.5f);
                case ArtMat.Grass:
                    var grass = S("#5E7A3C", 0.08f, 0f, 13f);
                    grass.Size = 512;
                    return grass;
                case ArtMat.Dirt: return S("#735E44", 0.10f, 0f, 2.5f);
                case ArtMat.Rock: return S("#77736B", 0.20f, 0f, 1.2f);
                case ArtMat.Water: return S("#1E3B42", 0.94f, 0f, 3f);
                case ArtMat.Hair: return S("#3A2A1E", 0.25f, 0f, 0.2f);
                case ArtMat.FurGray: return S("#77716A", 0.22f, 0f, 0.25f);
                case ArtMat.FurTan: return S("#A57A4C", 0.22f, 0f, 0.3f);
                case ArtMat.FurBrown: return S("#4A3222", 0.20f, 0f, 0.35f);
                case ArtMat.FurOrange: return S("#C9702A", 0.24f, 0f, 0.35f);
                case ArtMat.Hide: return S("#7A7671", 0.18f, 0f, 0.5f);
                case ArtMat.Ivory: return S("#E6DCC4", 0.45f, 0f, 0.2f);
                case ArtMat.Feather: return S("#5A4230", 0.20f, 0f, 0.3f);
                case ArtMat.Beak: return S("#D9A62E", 0.45f, 0f, 0.15f);
                case ArtMat.Ember:
                    // brasa: casca escura com rachaduras incandescentes. Emissão acima de 1
                    // de propósito — é o que o bloom pega e faz parecer fogo, não laranja
                    var ember = S("#2A1810", 0.2f, 0f, 0.3f);
                    ember.Emission = new Color(2.4f, 0.8f, 0.18f);
                    return ember;
                default: return S("#FF00FF", 0.5f, 0f, 1f);
            }
        }

        /// <summary>
        /// Textura fotográfica baixada (Assets/Resources/TDFende/Textures, ver THIRD_PARTY.md):
        /// nome da cor, nome da normal e quantas unidades de mundo uma repetição cobre.
        /// null = este material continua procedural (ProcTex). Se o arquivo sumir, o jogo
        /// cai no procedural sozinho — nunca fica rosa.
        /// </summary>
        public static (string albedo, string normal, float unitsPerTile)? External(ArtMat m)
        {
            switch (m)
            {
                // castle_brick_02 (Poly Haven, CC0): ~22 fiadas por repetição
                case ArtMat.Stone: return ("Stone_albedo", "Stone_normal", 1.4f);
                case ArtMat.StoneDark: return ("StoneDark_albedo", "Stone_normal", 1.4f);
                case ArtMat.StoneFrost: return ("StoneFrost_albedo", "Stone_normal", 1.4f);
                // tábuas (O3DE, MIT): 5 tábuas por repetição
                case ArtMat.Wood: return ("Wood_albedo", "Wood_normal", 0.4f);
                case ArtMat.WoodDark: return ("WoodDark_albedo", "Wood_normal", 0.4f);
                case ArtMat.Iron: return ("Iron_albedo", "Iron_normal", 0.5f);
                case ArtMat.Leather: return ("Leather_albedo", "Leather_normal", 0.4f);
                case ArtMat.Cloth: return ("Cloth_albedo", "Cloth_normal", 0.35f);
                case ArtMat.ClothTeam: return ("ClothTeam_albedo", "Cloth_normal", 0.35f);
                case ArtMat.Bark: return ("Bark_albedo", "Bark_normal", 0.6f);
                case ArtMat.Rock: return ("Rock_albedo", "Rock_normal", 1.4f);
                case ArtMat.Dirt: return ("Dirt_albedo", "Dirt_normal", 2.5f);
                // baixadas pelo Tools/BaixarArte.ps1 (Poly Haven, CC0), se o script rodou
                case ArtMat.RoofTile: return ("RoofTile_albedo", "RoofTile_normal", 0.7f);
                case ArtMat.Slate: return ("Slate_albedo", "Slate_normal", 0.7f);
                default: return null;
            }
        }

        static MatSpec S(string hex, float smooth, float metal, float unitsPerTile)
        {
            return new MatSpec { Base = ProcTex.Hex(hex), Smoothness = smooth, Metallic = metal, UnitsPerTile = unitsPerTile, Size = 256 };
        }
    }

    /// <summary>Ruído de valor com repetição exata: toda textura gerada emenda sem costura.</summary>
    public static class ProcNoise
    {
        public static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);

        static int Wrap(int i, int p) => ((i % p) + p) % p;

        /// <summary>Ruído 2D em [0,1] que repete a cada <paramref name="period"/> unidades.</summary>
        public static float Value(float x, float y, int period, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = Smooth(x - xi), fy = Smooth(y - yi);
            int x0 = Wrap(xi, period), x1 = Wrap(xi + 1, period);
            int y0 = Wrap(yi, period), y1 = Wrap(yi + 1, period);
            float a = Hash(x0, y0, seed), b = Hash(x1, y0, seed);
            float c = Hash(x0, y1, seed), d = Hash(x1, y1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>fBm tileável: u,v em [0,1), frequência base inteira.</summary>
        public static float Fbm(float u, float v, int freq, int octaves, int seed, float gain = 0.5f)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += amp * Value(u * freq, v * freq, freq, seed + o * 31);
                norm += amp;
                amp *= gain;
                freq *= 2;
            }
            return sum / norm;
        }

        /// <summary>fBm com eixos de frequência diferentes (veio de madeira, estria de casca).</summary>
        public static float FbmAniso(float u, float v, int fu, int fv, int octaves, int seed)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                // período por eixo = frequência do eixo; usa o maior para a grade e reescala
                sum += amp * Value2(u * fu, v * fv, fu, fv, seed + o * 17);
                norm += amp;
                amp *= 0.5f;
                fu *= 2;
                fv *= 2;
            }
            return sum / norm;
        }

        static float Value2(float x, float y, int px, int py, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = Smooth(x - xi), fy = Smooth(y - yi);
            int x0 = Wrap(xi, px), x1 = Wrap(xi + 1, px);
            int y0 = Wrap(yi, py), y1 = Wrap(yi + 1, py);
            float a = Hash(x0, y0, seed), b = Hash(x1, y0, seed);
            float c = Hash(x0, y1, seed), d = Hash(x1, y1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>
        /// Células (Worley) tileáveis: distância à semente mais próxima (f1), à segunda
        /// (f2) e um id estável da célula. Seixo, rachadura, folhagem, couro.
        /// </summary>
        public static void Cells(float u, float v, int cells, int seed, out float f1, out float f2, out float id)
        {
            float x = u * cells, y = v * cells;
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            f1 = f2 = 9f;
            id = 0f;
            for (int oy = -1; oy <= 1; oy++)
            for (int ox = -1; ox <= 1; ox++)
            {
                int cx = xi + ox, cy = yi + oy;
                int wx = Wrap(cx, cells), wy = Wrap(cy, cells);
                float px = cx + Hash(wx, wy, seed), py = cy + Hash(wx, wy, seed + 101);
                float d = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                if (d < f1)
                {
                    f2 = f1;
                    f1 = d;
                    id = Hash(wx, wy, seed + 202);
                }
                else if (d < f2) f2 = d;
            }
        }

        /// <summary>Ruído 3D de valor (não tileável) — deforma pedra e copa de árvore.</summary>
        public static float Value3(Vector3 p, int seed)
        {
            int xi = Mathf.FloorToInt(p.x), yi = Mathf.FloorToInt(p.y), zi = Mathf.FloorToInt(p.z);
            float fx = Smooth(p.x - xi), fy = Smooth(p.y - yi), fz = Smooth(p.z - zi);
            float Layer(int z) => Mathf.Lerp(
                Mathf.Lerp(Hash(xi, yi, seed + z * 7919), Hash(xi + 1, yi, seed + z * 7919), fx),
                Mathf.Lerp(Hash(xi, yi + 1, seed + z * 7919), Hash(xi + 1, yi + 1, seed + z * 7919), fx), fy);
            return Mathf.Lerp(Layer(zi), Layer(zi + 1), fz);
        }
    }

    /// <summary>Albedo (sRGB) + normal map (linear) prontos para virar Texture2D.</summary>
    public sealed class TexData
    {
        public int Size;
        public Color32[] Albedo;
        public Color32[] Normal;
    }

    /// <summary>
    /// Texturas PBR geradas em código — o "realista" sem comprar nem baixar asset.
    /// Cada gerador devolve cor e altura por pixel; a normal sai da altura.
    /// Linha 0 = base da textura (V = 0), como o Texture2D.SetPixels32 espera.
    /// </summary>
    public static class ProcTex
    {
        public static TexData Generate(ArtMat m)
        {
            var spec = MatSpec.Of(m);
            int s = spec.Size;
            var col = new Color[s * s];
            var h = new float[s * s];
            float strength;
            var b = spec.Base;
            int seed = (int)m * 97 + 13;

            switch (m)
            {
                case ArtMat.Stone:
                case ArtMat.StoneDark:
                case ArtMat.StoneFrost:
                    strength = Ashlar(s, b, seed, col, h);
                    break;
                case ArtMat.Wood:
                    strength = Planks(s, b, seed, col, h, true);
                    break;
                case ArtMat.WoodDark:
                    strength = Planks(s, b, seed, col, h, false);
                    break;
                case ArtMat.Iron:
                    strength = Metal(s, b, seed, col, h, Hex("#6B3A1E"), 0.64f);
                    break;
                case ArtMat.Bronze:
                    strength = Metal(s, b, seed, col, h, Hex("#4E7A68"), 0.70f);
                    break;
                case ArtMat.Slate:
                case ArtMat.RoofTile:
                    strength = Shingles(s, b, seed, col, h);
                    break;
                case ArtMat.Cloth:
                case ArtMat.ClothTeam:
                    strength = Weave(s, b, seed, col, h);
                    break;
                case ArtMat.Leather:
                    strength = Leather(s, b, seed, col, h);
                    break;
                case ArtMat.Skin:
                case ArtMat.Hair:
                    strength = Soft(s, b, seed, col, h, 0.05f);
                    break;
                case ArtMat.HorseCoat:
                case ArtMat.FurGray:
                case ArtMat.FurTan:
                case ArtMat.FurBrown:
                case ArtMat.FurOrange:
                case ArtMat.Feather:
                    strength = Coat(s, b, seed, col, h);
                    break;
                case ArtMat.Hide:
                    strength = Leather(s, b, seed, col, h);
                    break;
                case ArtMat.Ivory:
                case ArtMat.Beak:
                    strength = Soft(s, b, seed, col, h, 0.03f);
                    break;
                case ArtMat.Ice:
                    strength = Ice(s, b, seed, col, h);
                    break;
                case ArtMat.Foliage:
                    strength = Foliage(s, b, seed, col, h);
                    break;
                case ArtMat.Bark:
                    strength = Bark(s, b, seed, col, h);
                    break;
                case ArtMat.Grass:
                    strength = Grass(s, b, seed, col, h);
                    break;
                case ArtMat.Dirt:
                    strength = Dirt(s, b, seed, col, h);
                    break;
                case ArtMat.Rock:
                    strength = Rock(s, b, seed, col, h);
                    break;
                case ArtMat.Water:
                    strength = Water(s, b, seed, col, h);
                    break;
                case ArtMat.Ember:
                    strength = Ember(s, b, seed, col, h);
                    break;
                default:
                    strength = Soft(s, b, seed, col, h, 0.02f);
                    break;
            }

            return new TexData { Size = s, Albedo = ToColor32(col), Normal = NormalFromHeight(h, s, strength) };
        }

        // ------------------------------------------------------------ geradores

        /// <summary>Cantaria: fiadas de blocos desencontradas, argamassa funda, quina gasta.</summary>
        static float Ashlar(int s, Color b, int seed, Color[] col, float[] h)
        {
            const int courses = 6;
            const float mortar = 0.011f;
            var mortarCol = Color.Lerp(b, new Color(0.62f, 0.60f, 0.56f), 0.5f) * 0.82f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                int row = Mathf.FloorToInt(v * courses);
                float fy = v * courses - row;
                float dy = Mathf.Min(fy, 1f - fy) / courses;

                // blocos por fiada variam (2 a 4) e cada fiada desliza: nada alinha na vertical
                int n = 2 + Mathf.FloorToInt(ProcNoise.Hash(row, 0, seed) * 2.99f);
                float off = ProcNoise.Hash(row, 1, seed);
                float t = Frac(u - off) * n;
                int k = Mathf.FloorToInt(t);
                float local = t - k;
                float dx = Mathf.Min(local, 1f - local) / n;
                float edge = Mathf.Min(dx, dy);

                float blockTone = ProcNoise.Hash(row * 13 + k, 3, seed);
                float warm = ProcNoise.Hash(row * 13 + k, 4, seed) - 0.5f;
                float grain = ProcNoise.Fbm(u, v, 8, 5, seed + 5);
                float chip = ProcNoise.Fbm(u, v, 16, 3, seed + 9);
                float large = ProcNoise.Fbm(u, v, 2, 3, seed + 21);

                int i = y * s + x;
                if (edge < mortar)
                {
                    col[i] = mortarCol * (0.9f + 0.2f * grain);
                    h[i] = 0.05f * grain;
                    continue;
                }
                float bevel = Smooth01((edge - mortar) / 0.03f);
                float height = 0.55f + 0.35f * bevel + 0.22f * (grain - 0.5f);
                // lasca: a quina perde pedaço onde o ruído fino cai
                if (chip < 0.36f && edge < mortar + 0.045f) height -= 0.25f * (0.36f - chip) / 0.36f;
                h[i] = height;

                var c = b * (0.80f + 0.32f * blockTone) * (0.88f + 0.24f * grain) * (0.9f + 0.2f * large);
                c.r *= 1f + 0.08f * warm;
                c.b *= 1f - 0.08f * warm;
                // sombra de oclusão junto da argamassa: o bloco "sai" da parede
                c *= Mathf.Lerp(0.8f, 1f, bevel);
                if (ProcNoise.Hash(x, y, seed + 77) > 0.985f) c *= 0.7f; // pintas escuras do grão
                col[i] = c;
            }
            return 5f;
        }

        /// <summary>Tábuas (ou viga maciça): veio correndo ao longo do V.</summary>
        static float Planks(int s, Color b, int seed, Color[] col, float[] h, bool seams)
        {
            const int planks = 4;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                int p = Mathf.FloorToInt(u * planks);
                float local = u * planks - p;
                float tone = seams ? ProcNoise.Hash(p, 0, seed) : 0.5f;
                float warp = ProcNoise.FbmAniso(u, v, 4, 2, 4, seed + p);
                float ring = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (u * 22f + warp * 5f + tone * 7f));
                float fine = ProcNoise.FbmAniso(u, v, 64, 4, 3, seed + 3);
                float grain = 0.6f * ring + 0.4f * fine;

                var c = b * (0.78f + 0.3f * grain) * (0.88f + 0.24f * tone);
                float height = 0.5f + 0.2f * (grain - 0.5f);
                if (seams)
                {
                    float d = Mathf.Min(local, 1f - local) / planks;
                    if (d < 0.006f)
                    {
                        c *= 0.35f;
                        height = 0f;
                    }
                    else c *= Mathf.Lerp(0.75f, 1f, Smooth01((d - 0.006f) / 0.012f));
                    // pregos: dois por tábua, perto das pontas
                    float nv = Frac(v * 2f);
                    float ndx = (local - 0.5f) / planks, ndy = (nv - 0.08f) / 2f;
                    if (ndx * ndx + ndy * ndy < 0.00004f) c = new Color(0.2f, 0.2f, 0.21f);
                }
                int i = y * s + x;
                col[i] = c;
                h[i] = height;
            }
            return 2.5f;
        }

        /// <summary>Metal fundido: manchas, riscos finos e ferrugem/pátina nas cavidades.</summary>
        static float Metal(int s, Color b, int seed, Color[] col, float[] h, Color corrosion, float threshold)
        {
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                float mottle = ProcNoise.Fbm(u, v, 4, 5, seed);
                float scratch = ProcNoise.FbmAniso(u, v, 96, 6, 2, seed + 4);
                float rust = ProcNoise.Fbm(u, v, 3, 5, seed + 8);
                var c = b * (0.85f + 0.25f * mottle) * (0.95f + 0.1f * scratch);
                float r = Smooth01((rust - threshold) / 0.12f);
                c = Color.Lerp(c, corrosion * (0.8f + 0.4f * mottle), r * 0.75f);
                int i = y * s + x;
                col[i] = c;
                h[i] = 0.5f + 0.1f * scratch + 0.25f * r * mottle;
            }
            return 1.5f;
        }

        /// <summary>Telhas/ardósia sobrepostas: cada fiada cobre a de baixo.</summary>
        static float Shingles(int s, Color b, int seed, Color[] col, float[] h)
        {
            const int rows = 8, perRow = 6;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                int row = Mathf.FloorToInt(v * rows);
                float fv = v * rows - row;
                float shift = (row & 1) * 0.5f;
                float tu = Frac(u + shift / perRow) * perRow;
                int k = Mathf.FloorToInt(tu);
                float fu = tu - k;
                float tone = ProcNoise.Hash(row, k, seed);
                float grain = ProcNoise.Fbm(u, v, 16, 4, seed + 2);

                // borda de baixo arredondada; a telha engrossa para baixo (fv pequeno)
                float round = 0.18f * (1f - Mathf.Sin(fu * Mathf.PI));
                bool gap = fu < 0.04f || fu > 0.96f || fv < round * 0.6f;
                float height = gap ? 0.05f : 0.35f + 0.6f * (1f - fv);
                var c = b * (0.78f + 0.36f * tone) * (0.9f + 0.2f * grain);
                c *= gap ? 0.45f : Mathf.Lerp(0.7f, 1f, fv); // sombra da fiada de cima
                int i = y * s + x;
                col[i] = c;
                h[i] = height;
            }
            return 3.5f;
        }

        static float Weave(int s, Color b, int seed, Color[] col, float[] h)
        {
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                float wu = Mathf.Sin(2f * Mathf.PI * x / 4f), wv = Mathf.Sin(2f * Mathf.PI * y / 4f);
                float weave = 0.5f + 0.5f * wu * wv;
                float dirt = ProcNoise.Fbm(u, v, 4, 4, seed);
                var c = b * (0.86f + 0.14f * weave) * (0.88f + 0.16f * dirt);
                int i = y * s + x;
                col[i] = c;
                h[i] = 0.3f * weave;
            }
            return 1.2f;
        }

        static float Leather(int s, Color b, int seed, Color[] col, float[] h)
        {
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                ProcNoise.Cells(u, v, 24, seed, out float f1, out float f2, out _);
                float crease = Smooth01((f2 - f1) / 0.12f);
                float tone = ProcNoise.Fbm(u, v, 4, 4, seed + 3);
                var c = b * (0.75f + 0.3f * tone) * Mathf.Lerp(0.8f, 1f, crease);
                int i = y * s + x;
                col[i] = c;
                h[i] = 0.4f * crease + 0.2f * tone;
            }
            return 1.5f;
        }

        static float Soft(int s, Color b, int seed, Color[] col, float[] h, float amount)
        {
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                float n = ProcNoise.Fbm(u, v, 8, 4, seed);
                int i = y * s + x;
                col[i] = b * (1f - amount + 2f * amount * n);
                h[i] = 0.2f * n;
            }
            return 0.4f;
        }

        static float Coat(int s, Color b, int seed, Color[] col, float[] h)
        {
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                float hair = ProcNoise.FbmAniso(u, v, 64, 8, 3, seed);
                float tone = ProcNoise.Fbm(u, v, 3, 3, seed + 5);
                int i = y * s + x;
                col[i] = b * (0.85f + 0.2f * hair) * (0.9f + 0.2f * tone);
                h[i] = 0.3f * hair;
            }
            return 0.8f;
        }

        static float Ice(int s, Color b, int seed, Color[] col, float[] h)
        {
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                ProcNoise.Cells(u, v, 6, seed, out float f1, out float f2, out float id);
                float crack = 1f - Smooth01((f2 - f1) / 0.05f);
                float cloud = ProcNoise.Fbm(u, v, 4, 4, seed + 1);
                var c = b * (0.85f + 0.2f * cloud + 0.1f * id);
                c = Color.Lerp(c, Color.white, crack * 0.6f);
                int i = y * s + x;
                col[i] = c;
                h[i] = 0.5f - 0.4f * crack;
            }
            return 2f;
        }

        static float Foliage(int s, Color b, int seed, Color[] col, float[] h)
        {
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                ProcNoise.Cells(u, v, 30, seed, out float f1, out _, out float id);
                float clump = 1f - Smooth01(f1 / 0.75f);
                float fine = ProcNoise.Fbm(u, v, 32, 3, seed + 2);
                var c = b * (0.72f + 0.36f * clump) * (0.85f + 0.3f * fine);
                c.r *= 0.9f + 0.25f * id; // folhas mais amareladas aqui e ali
                int i = y * s + x;
                col[i] = c;
                h[i] = 0.7f * clump + 0.3f * fine;
            }
            return 3f;
        }

        static float Bark(int s, Color b, int seed, Color[] col, float[] h)
        {
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                float warp = ProcNoise.FbmAniso(u, v, 8, 2, 3, seed);
                float ridge = Mathf.Abs(Mathf.Sin(Mathf.PI * (u * 10f + warp * 3f)));
                float fine = ProcNoise.Fbm(u, v, 16, 3, seed + 4);
                var c = b * (0.6f + 0.5f * ridge) * (0.85f + 0.3f * fine);
                int i = y * s + x;
                col[i] = c;
                h[i] = ridge * 0.8f + 0.2f * fine;
            }
            return 4f;
        }

        /// <summary>
        /// Relva: manchas grandes de tom (a textura cobre várias células, sem padrão
        /// repetido visível), riscos finos de folha e falhas de terra.
        /// </summary>
        static float Grass(int s, Color b, int seed, Color[] col, float[] h)
        {
            var dry = Hex("#858447");
            var lush = Hex("#46652D");
            var dirt = Hex("#6B5A3E");
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                float patch = ProcNoise.Fbm(u, v, 3, 4, seed);
                float patch2 = ProcNoise.Fbm(u, v, 6, 3, seed + 40);
                float blades = ProcNoise.FbmAniso(u, v, 160, 48, 3, seed + 7);
                float speck = ProcNoise.Hash(x, y, seed + 9);

                var c = Color.Lerp(lush, b, Smooth01(patch * 1.6f - 0.3f));
                c = Color.Lerp(c, dry, Smooth01((patch2 - 0.55f) / 0.25f) * 0.55f);
                c *= 0.72f + 0.5f * blades;
                if (speck > 0.997f) c = Color.Lerp(c, dry * 1.15f, 0.4f);
                float bare = Smooth01((0.25f - patch) / 0.08f);
                c = Color.Lerp(c, dirt * (0.85f + 0.3f * blades), bare * 0.8f);

                int i = y * s + x;
                col[i] = c;
                h[i] = 0.6f * blades + 0.2f * patch2;
            }
            return 2.2f;
        }

        static float Dirt(int s, Color b, int seed, Color[] col, float[] h)
        {
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                ProcNoise.Cells(u, v, 20, seed, out float f1, out _, out float id);
                float pebble = id > 0.72f ? 1f - Smooth01(f1 / 0.4f) : 0f;
                float tone = ProcNoise.Fbm(u, v, 4, 5, seed + 1);
                var c = b * (0.75f + 0.4f * tone);
                c = Color.Lerp(c, Hex("#7E776A") * (0.8f + 0.3f * id), pebble * 0.55f);
                int i = y * s + x;
                col[i] = c;
                h[i] = 0.3f * tone + 0.6f * pebble;
            }
            return 3f;
        }

        static float Rock(int s, Color b, int seed, Color[] col, float[] h)
        {
            var lichen = Hex("#8A8F4A");
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                float n = ProcNoise.Fbm(u, v, 4, 6, seed);
                float strata = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (v * 9f + n * 2.5f));
                float l = ProcNoise.Fbm(u, v, 8, 3, seed + 3);
                var c = b * (0.62f + 0.4f * n) * (0.9f + 0.15f * strata);
                c = Color.Lerp(c, lichen, Smooth01((l - 0.62f) / 0.1f) * 0.6f);
                int i = y * s + x;
                col[i] = c;
                h[i] = 0.7f * n + 0.2f * strata;
            }
            return 4f;
        }

        /// <summary>Brasa: placas de carvão escuro separadas por rachaduras em brasa (claras no albedo).</summary>
        static float Ember(int s, Color b, int seed, Color[] col, float[] h)
        {
            var glow = Hex("#FF9A3C");
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                ProcNoise.Cells(u, v, 10, seed, out float f1, out float f2, out _);
                float crack = 1f - Smooth01((f2 - f1) / 0.09f);
                float n = ProcNoise.Fbm(u, v, 8, 4, seed + 3);
                int i = y * s + x;
                col[i] = Color.Lerp(b * (0.7f + 0.6f * n), glow, crack);
                h[i] = 0.6f - 0.5f * crack + 0.2f * n;
            }
            return 3f;
        }

        static float Water(int s, Color b, int seed, Color[] col, float[] h)
        {
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                float ripple = 0.5f + 0.25f * Mathf.Sin(2f * Mathf.PI * (u * 3f + v * 5f))
                                    + 0.25f * Mathf.Sin(2f * Mathf.PI * (u * 7f - v * 2f));
                float n = ProcNoise.Fbm(u, v, 8, 4, seed);
                int i = y * s + x;
                col[i] = b * (0.9f + 0.2f * n);
                h[i] = 0.5f * ripple + 0.5f * n;
            }
            return 1.2f;
        }

        // -------------------------------------------------------------- utilidades

        static float Frac(float x) => x - Mathf.Floor(x);

        static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// "#RRGGBB" em C# puro. Não usa ColorUtility de propósito: as texturas são
        /// geradas em várias threads no boot, e API do motor fora da thread principal
        /// é pedir para quebrar.
        /// </summary>
        public static Color Hex(string hex)
        {
            int v = System.Convert.ToInt32(hex.TrimStart('#'), 16);
            return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f);
        }

        static Color32[] ToColor32(Color[] c)
        {
            var o = new Color32[c.Length];
            for (int i = 0; i < c.Length; i++)
                o[i] = new Color32(To8(c[i].r), To8(c[i].g), To8(c[i].b), 255);
            return o;
        }

        static byte To8(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);

        /// <summary>
        /// Normal em espaço tangente a partir da altura (Sobel com repetição nas bordas).
        /// Codificada como (x, y, z, 1): serve tanto para quem lê RGB quanto para o
        /// caminho "AG" do Unity (que faz a *= r e lê .ag), sem precisar do importador.
        /// </summary>
        static Color32[] NormalFromHeight(float[] h, int s, float strength)
        {
            var o = new Color32[h.Length];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                int xl = (x + s - 1) % s, xr = (x + 1) % s, yd = (y + s - 1) % s, yu = (y + 1) % s;
                float dx = h[y * s + xr] - h[y * s + xl];
                float dy = h[yu * s + x] - h[yd * s + x];
                var n = new Vector3(-dx * strength, -dy * strength, 1f).normalized;
                o[y * s + x] = new Color32(To8(n.x * 0.5f + 0.5f), To8(n.y * 0.5f + 0.5f), To8(n.z * 0.5f + 0.5f), 255);
            }
            return o;
        }
    }
}
