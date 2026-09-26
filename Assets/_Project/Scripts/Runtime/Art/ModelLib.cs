using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>Como a vista anima um modelo. Os dados ficam aqui; o movimento, no EnemyRig.</summary>
    public enum AnimKind { None, Walker, Horse, Wheels, Glider }

    /// <summary>
    /// Uma peça do modelo. A malha está em coordenadas do MODELO; o pivô diz em torno
    /// de onde a peça gira (quadril da perna, munhão do canhão, eixo da roda).
    /// </summary>
    public sealed class ModelPart
    {
        public string Name;
        public string Parent;
        public Vector3 Pivot;
        public bool StartHidden;
        public readonly MeshBuilder Mesh = new MeshBuilder();
    }

    /// <summary>Modelo procedural: peças + os metadados que a vista precisa para animar.</summary>
    public sealed class ModelDef
    {
        public string Name;
        public readonly List<ModelPart> Parts = new List<ModelPart>();
        public AnimKind Anim;
        /// <summary>Altura do topo (barra de vida fica logo acima).</summary>
        public float Height;
        /// <summary>Torres: altura do fuste, que estica com o nível.</summary>
        public float ShaftHeight;
        /// <summary>Torres: boca do cano, em coordenadas do modelo.</summary>
        public Vector3 Muzzle;
        /// <summary>Andantes: comprimento do passo (mundo). Rodas: raio.</summary>
        public float Stride = 0.3f;

        public ModelPart Part(string name, Vector3 pivot, string parent = null)
        {
            var p = new ModelPart { Name = name, Pivot = pivot, Parent = parent };
            Parts.Add(p);
            return p;
        }

        public ModelPart Find(string name)
        {
            for (int i = 0; i < Parts.Count; i++)
                if (Parts[i].Name == name) return Parts[i];
            return null;
        }
    }

    /// <summary>
    /// Todos os modelos do jogo, em código. Direção de arte: realista, fim da Idade
    /// Média / início da pólvora (a era em que Rise of Nations começa a ficar bom):
    /// cantaria, madeira, bronze fundido, cota de malha.
    ///
    /// Escala: 1 unidade = 1 célula do grid ≈ 3 m. Um soldado tem ~0,6 de altura.
    /// Todo modelo olha para +Z e pisa em Y = 0.
    ///
    /// Regra que vem do SendCatalog e continua valendo: cada tipo precisa de SILHUETA
    /// própria no zoom do jogo — lanceiro, escaramuçador, cavaleiro, cavaleiro de
    /// armadura, planador, torre de cerco. Detalhe é bônus; silhueta é leitura.
    /// </summary>
    public static class ModelLib
    {
        // nomes de peça que a vista procura
        public const string Shaft = "Shaft", Top = "Top", Turret = "Turret", Barrel = "Barrel", Flag = "Flag";
        public const string Body = "Body", LegL = "LegL", LegR = "LegR", ArmL = "ArmL", ArmR = "ArmR", Wing = "Wing";

        static readonly Dictionary<string, ModelDef> Cache = new Dictionary<string, ModelDef>();

        static ModelDef Cached(string key, System.Func<ModelDef> build)
        {
            if (!Cache.TryGetValue(key, out var d))
            {
                d = build();
                d.Name = key;
                Cache[key] = d;
            }
            return d;
        }

        public static ModelDef Tower(int typeId) => typeId switch
        {
            1 => Cached("Torre_Morteiro", MortarTower),
            2 => Cached("Torre_Gelo", FrostTower),
            3 => Cached("Torre_Sentinela", WatchTower),
            _ => Cached("Torre_Canhao", CannonTower),
        };

        public static ModelDef Enemy(int typeId) => typeId switch
        {
            1 => Cached("Inimigo_Enxame", Skirmisher),
            2 => Cached("Inimigo_Corredor", Horseman),
            3 => Cached("Inimigo_Couracado", Knight),
            4 => Cached("Inimigo_Planador", Glider),
            5 => Cached("Inimigo_Colosso", SiegeTower),
            _ => Cached("Inimigo_Recruta", Spearman),
        };

        public static ModelDef Projectile(int towerTypeId) => towerTypeId switch
        {
            1 => Cached("Tiro_Morteiro", () => Ball(0.085f)),
            2 => Cached("Tiro_Gelo", IceShard),
            3 => Cached("Tiro_Sentinela", Bolt),
            _ => Cached("Tiro_Canhao", () => Ball(0.06f)),
        };

        public static ModelDef Keep() => Cached("Fortaleza", BuildKeep);
        public static ModelDef Camp() => Cached("Acampamento", BuildCamp);

        // ================================================================ torres

        static readonly Vector3 Up = Vector3.up;

        static Quaternion Along(Vector3 dir) => Quaternion.FromToRotation(Vector3.up, dir.normalized);

        /// <summary>Anel de caixas tangentes a um círculo (parapeito, ameias).</summary>
        static void Ring(MeshBuilder mb, ArtMat m, Vector3 center, float radius, int count, Vector3 size,
            float phase = 0f, int every = 1)
        {
            for (int i = 0; i < count; i++)
            {
                if (i % every != 0) continue;
                float a = phase + i * Mathf.PI * 2f / count;
                var pos = center + new Vector3(Mathf.Cos(a) * radius, size.y * 0.5f, Mathf.Sin(a) * radius);
                mb.Box(m, pos, size, Quaternion.Euler(0f, -(a * Mathf.Rad2Deg + 90f), 0f));
            }
        }

        /// <summary>Parapeito ameado: mureta contínua + merlões alternados.</summary>
        static void Battlement(MeshBuilder mb, ArtMat m, Vector3 floor, float radius, int merlons, float wallH,
            float merlonH, float thick)
        {
            int segs = merlons * 2;
            float seg = 2f * Mathf.PI * radius / segs * 1.08f;
            Ring(mb, m, floor, radius, segs, new Vector3(seg, wallH, thick));
            Ring(mb, m, floor + Up * wallH, radius, segs, new Vector3(seg, merlonH, thick), 0f, 2);
        }

        static void Plinth(ModelDef d, float size, float h = 0.08f)
        {
            var b = d.Part("Base", Vector3.zero).Mesh;
            b.Box(ArtMat.StoneDark, new Vector3(0f, h * 0.5f, 0f), new Vector3(size, h, size));
            b.Box(ArtMat.StoneDark, new Vector3(0f, h + 0.012f, 0f), new Vector3(size * 0.92f, 0.024f, size * 0.92f));
        }

        static void TeamFlag(ModelDef d, string parent, Vector3 foot, float poleH, float w, float h)
        {
            var f = d.Part(Flag, foot, parent);
            f.StartHidden = true;
            f.Mesh.Rod(ArtMat.WoodDark, foot, foot + Up * poleH, 0.012f);
            f.Mesh.Sphere(ArtMat.Bronze, foot + Up * (poleH + 0.012f), Vector3.one * 0.018f, 8, 6);
            var top = foot + Up * (poleH - 0.01f);
            f.Mesh.DoubleQuad(ArtMat.ClothTeam, top + new Vector3(0.012f, -h, 0f), top + new Vector3(0.012f, 0f, 0f),
                top + new Vector3(w, -0.015f, 0f), top + new Vector3(w * 0.96f, -h + 0.01f, 0f));
        }

        /// <summary>Canhão: torre redonda de cantaria com canhão de bronze no topo. A régua.</summary>
        static ModelDef CannonTower()
        {
            var d = new ModelDef();
            Plinth(d, 0.9f);

            const float y0 = 0.104f, shaftH = 0.52f;
            d.ShaftHeight = shaftH;
            var s = d.Part(Shaft, new Vector3(0f, y0, 0f)).Mesh;
            // talude: a base alarga, como muralha de verdade
            s.Lathe(ArtMat.Stone, new Vector3(0f, y0, 0f), new[] { 0.40f, 0.36f, 0.34f, 0.34f },
                new[] { 0f, 0.12f, 0.2f, shaftH }, 22);
            s.Cylinder(ArtMat.StoneDark, new Vector3(0f, y0 + 0.12f, 0f), 0.365f, 0.365f, 0.025f, 22);
            // seteiras
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var p = new Vector3(Mathf.Cos(a) * 0.335f, y0 + 0.34f, Mathf.Sin(a) * 0.335f);
                s.Box(ArtMat.WoodDark, p, new Vector3(0.025f, 0.09f, 0.02f), Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f));
            }

            float ty = y0 + shaftH;
            var t = d.Part(Top, new Vector3(0f, ty, 0f)).Mesh;
            // mata-cães: o topo avança sobre o fuste
            t.Cylinder(ArtMat.StoneDark, new Vector3(0f, ty, 0f), 0.34f, 0.39f, 0.05f, 22);
            t.Cylinder(ArtMat.Wood, new Vector3(0f, ty + 0.05f, 0f), 0.37f, 0.37f, 0.012f, 22);
            Battlement(t, ArtMat.Stone, new Vector3(0f, ty + 0.05f, 0f), 0.36f, 8, 0.07f, 0.07f, 0.06f);

            float cy = ty + 0.062f;
            var tu = d.Part(Turret, new Vector3(0f, cy, 0f), Top).Mesh;
            // reparo de madeira com duas rodas
            tu.Box(ArtMat.WoodDark, new Vector3(0f, cy + 0.05f, -0.02f), new Vector3(0.16f, 0.07f, 0.3f));
            tu.Box(ArtMat.WoodDark, new Vector3(0f, cy + 0.1f, 0f), new Vector3(0.18f, 0.04f, 0.1f));
            foreach (float sx in new[] { -1f, 1f })
            {
                var hub = new Vector3(sx * 0.1f, cy + 0.06f, 0.04f);
                tu.Cylinder(ArtMat.Wood, hub - Vector3.right * 0.012f * sx, 0.06f, 0.06f, 0.024f, 12,
                    Along(Vector3.right * sx));
                tu.Cylinder(ArtMat.Iron, hub + Vector3.right * 0.012f * sx, 0.018f, 0.018f, 0.012f, 8,
                    Along(Vector3.right * sx));
            }

            var trunnion = new Vector3(0f, cy + 0.13f, 0.0f);
            var b = d.Part(Barrel, trunnion, Turret).Mesh;
            var breech = trunnion + new Vector3(0f, 0f, -0.13f);
            b.Lathe(ArtMat.Bronze, breech,
                new[] { 0.0f, 0.055f, 0.062f, 0.058f, 0.05f, 0.046f, 0.052f, 0.052f, 0.03f },
                new[] { 0.0f, 0.004f, 0.04f, 0.09f, 0.18f, 0.29f, 0.30f, 0.34f, 0.34f }, 14, Along(Vector3.forward));
            b.Sphere(ArtMat.Bronze, breech + new Vector3(0f, 0f, -0.018f), Vector3.one * 0.022f, 8, 6);
            b.Cylinder(ArtMat.Iron, breech + new Vector3(0f, 0f, 0.338f), 0.03f, 0.03f, 0.004f, 10, Along(Vector3.forward));
            b.Rod(ArtMat.Bronze, trunnion + Vector3.left * 0.075f, trunnion + Vector3.right * 0.075f, 0.014f, 8);
            d.Muzzle = breech + new Vector3(0f, 0f, 0.36f);

            TeamFlag(d, Top, new Vector3(-0.25f, ty + 0.05f, -0.25f), 0.42f, 0.2f, 0.13f);
            d.Height = ty + 0.3f;
            return d;
        }

        /// <summary>Morteiro: bastião octogonal baixo e largo, morteiro de bronze apontado ao céu.</summary>
        static ModelDef MortarTower()
        {
            var d = new ModelDef();
            Plinth(d, 0.96f);

            const float y0 = 0.104f, shaftH = 0.26f;
            d.ShaftHeight = shaftH;
            float oct = Mathf.PI / 8f;
            var s = d.Part(Shaft, new Vector3(0f, y0, 0f)).Mesh;
            s.Cylinder(ArtMat.Stone, new Vector3(0f, y0, 0f), 0.47f, 0.43f, shaftH, 8, flat: true, angleOffset: oct);
            s.Cylinder(ArtMat.StoneDark, new Vector3(0f, y0 + shaftH - 0.03f, 0f), 0.445f, 0.445f, 0.03f, 8,
                flat: true, angleOffset: oct);

            float ty = y0 + shaftH;
            var t = d.Part(Top, new Vector3(0f, ty, 0f)).Mesh;
            t.Cylinder(ArtMat.Wood, new Vector3(0f, ty, 0f), 0.42f, 0.42f, 0.012f, 8, flat: true, angleOffset: oct);
            // parapeito octogonal, uma mureta por face
            float apothem = 0.42f * Mathf.Cos(oct);
            float side = 2f * 0.42f * Mathf.Sin(oct);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var p = new Vector3(Mathf.Cos(a) * apothem, ty + 0.05f, Mathf.Sin(a) * apothem);
                t.Box(ArtMat.Stone, p, new Vector3(side * 1.02f, 0.1f, 0.06f),
                    Quaternion.Euler(0f, -(a * Mathf.Rad2Deg + 90f), 0f));
            }
            // barris de pólvora e pilha de bombas nos cantos
            foreach (var c in new[] { new Vector3(0.26f, 0f, 0.2f), new Vector3(-0.25f, 0f, 0.22f) })
            {
                var bp = new Vector3(c.x, ty + 0.012f, c.z);
                t.Lathe(ArtMat.Wood, bp, new[] { 0.045f, 0.055f, 0.045f }, new[] { 0f, 0.055f, 0.11f }, 10);
                t.Cylinder(ArtMat.Iron, bp + Up * 0.02f, 0.051f, 0.051f, 0.008f, 10);
                t.Cylinder(ArtMat.Iron, bp + Up * 0.085f, 0.051f, 0.051f, 0.008f, 10);
            }
            var pile = new Vector3(-0.22f, ty + 0.05f, -0.22f);
            t.Sphere(ArtMat.Iron, pile + new Vector3(-0.04f, 0f, 0f), Vector3.one * 0.038f, 8, 6);
            t.Sphere(ArtMat.Iron, pile + new Vector3(0.04f, 0f, 0f), Vector3.one * 0.038f, 8, 6);
            t.Sphere(ArtMat.Iron, pile + new Vector3(0f, 0f, 0.06f), Vector3.one * 0.038f, 8, 6);
            t.Sphere(ArtMat.Iron, pile + new Vector3(0f, 0.055f, 0.02f), Vector3.one * 0.038f, 8, 6);

            float cy = ty + 0.012f;
            var tu = d.Part(Turret, new Vector3(0f, cy, 0f), Top).Mesh;
            tu.Box(ArtMat.WoodDark, new Vector3(0f, cy + 0.04f, 0f), new Vector3(0.28f, 0.08f, 0.3f));
            tu.Box(ArtMat.WoodDark, new Vector3(0.1f, cy + 0.1f, 0f), new Vector3(0.04f, 0.08f, 0.16f));
            tu.Box(ArtMat.WoodDark, new Vector3(-0.1f, cy + 0.1f, 0f), new Vector3(0.04f, 0.08f, 0.16f));

            var trunnion = new Vector3(0f, cy + 0.13f, 0f);
            var b = d.Part(Barrel, trunnion, Turret).Mesh;
            var tilt = Quaternion.Euler(40f, 0f, 0f);
            var foot = trunnion + tilt * new Vector3(0f, -0.07f, 0f);
            b.Lathe(ArtMat.Bronze, foot,
                new[] { 0.0f, 0.085f, 0.1f, 0.095f, 0.085f, 0.1f, 0.1f, 0.06f },
                new[] { 0.0f, 0.005f, 0.05f, 0.1f, 0.2f, 0.21f, 0.25f, 0.25f }, 16, tilt);
            b.Cylinder(ArtMat.Iron, foot + tilt * new Vector3(0f, 0.248f, 0f), 0.06f, 0.06f, 0.004f, 12, tilt);
            b.Rod(ArtMat.Bronze, trunnion + Vector3.left * 0.12f, trunnion + Vector3.right * 0.12f, 0.022f, 8);
            d.Muzzle = foot + tilt * new Vector3(0f, 0.27f, 0f);

            TeamFlag(d, Top, new Vector3(0.3f, ty + 0.012f, -0.28f), 0.4f, 0.2f, 0.13f);
            d.Height = ty + 0.35f;
            return d;
        }

        /// <summary>Gelo: torre esguia de pedra clara, coroada por um aglomerado de cristal sob telhado de ardósia.</summary>
        static ModelDef FrostTower()
        {
            var d = new ModelDef();
            Plinth(d, 0.8f);

            const float y0 = 0.104f, shaftH = 0.58f;
            d.ShaftHeight = shaftH;
            var s = d.Part(Shaft, new Vector3(0f, y0, 0f)).Mesh;
            s.Lathe(ArtMat.StoneFrost, new Vector3(0f, y0, 0f), new[] { 0.32f, 0.28f, 0.27f },
                new[] { 0f, 0.1f, shaftH }, 20);
            s.Cylinder(ArtMat.StoneDark, new Vector3(0f, y0 + 0.3f, 0f), 0.285f, 0.285f, 0.02f, 20);
            s.Box(ArtMat.WoodDark, new Vector3(0f, y0 + 0.09f, -0.305f), new Vector3(0.11f, 0.17f, 0.03f)); // porta

            float ty = y0 + shaftH;
            var t = d.Part(Top, new Vector3(0f, ty, 0f)).Mesh;
            t.Cylinder(ArtMat.StoneDark, new Vector3(0f, ty, 0f), 0.28f, 0.32f, 0.045f, 20);
            // quatro colunas sustentam o telhado; o cristal fica à vista entre elas
            const float postH = 0.4f;
            for (int i = 0; i < 4; i++)
            {
                float a = Mathf.PI * 0.25f + i * Mathf.PI * 0.5f;
                var p = new Vector3(Mathf.Cos(a) * 0.25f, ty + 0.045f, Mathf.Sin(a) * 0.25f);
                t.Box(ArtMat.StoneFrost, p + Up * postH * 0.5f, new Vector3(0.06f, postH, 0.06f),
                    Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f));
            }
            float roofY = ty + 0.045f + postH;
            t.Cylinder(ArtMat.StoneDark, new Vector3(0f, roofY, 0f), 0.31f, 0.31f, 0.03f, 16);
            t.Cylinder(ArtMat.Slate, new Vector3(0f, roofY + 0.03f, 0f), 0.36f, 0.0f, 0.34f, 16, capTop: false);
            t.Sphere(ArtMat.Bronze, new Vector3(0f, roofY + 0.38f, 0f), Vector3.one * 0.025f, 8, 6);
            // braseiro de bronze onde o cristal assenta
            t.Lathe(ArtMat.Bronze, new Vector3(0f, ty + 0.045f, 0f), new[] { 0.03f, 0.03f, 0.11f, 0.12f },
                new[] { 0f, 0.06f, 0.1f, 0.12f }, 14);

            float cy = ty + 0.16f;
            var tu = d.Part(Turret, new Vector3(0f, cy, 0f), Top).Mesh;
            Crystal(tu, new Vector3(0f, cy - 0.01f, 0f), Quaternion.identity, 0.055f, 0.26f);
            Crystal(tu, new Vector3(0.05f, cy - 0.01f, 0.02f), Quaternion.Euler(0f, 0f, -24f), 0.035f, 0.16f);
            Crystal(tu, new Vector3(-0.045f, cy - 0.01f, -0.02f), Quaternion.Euler(18f, 0f, 22f), 0.035f, 0.15f);
            Crystal(tu, new Vector3(0f, cy - 0.01f, 0.05f), Quaternion.Euler(28f, 0f, 0f), 0.03f, 0.13f);
            d.Muzzle = new Vector3(0f, cy + 0.2f, 0.04f);

            TeamFlag(d, Top, new Vector3(0f, roofY + 0.36f, 0f), 0.22f, 0.16f, 0.1f);
            d.Height = roofY + 0.45f;
            return d;
        }

        static void Crystal(MeshBuilder mb, Vector3 foot, Quaternion rot, float r, float h)
        {
            mb.Lathe(ArtMat.Ice, foot, new[] { 0.0f, r, r * 0.9f, 0.0f }, new[] { 0f, h * 0.18f, h * 0.72f, h },
                6, rot, flat: true);
        }

        /// <summary>Sentinela: base de pedra, torre de vigia de madeira alta com balista no alto.</summary>
        static ModelDef WatchTower()
        {
            var d = new ModelDef();
            Plinth(d, 0.8f);

            const float y0 = 0.104f, shaftH = 0.86f;
            d.ShaftHeight = shaftH;
            var s = d.Part(Shaft, new Vector3(0f, y0, 0f)).Mesh;
            float q = Mathf.PI * 0.25f;
            s.Cylinder(ArtMat.StoneDark, new Vector3(0f, y0, 0f), 0.36f, 0.33f, 0.26f, 4, flat: true, angleOffset: q);
            float legY = y0 + 0.26f, topY = y0 + shaftH;
            var corners = new[] { new Vector3(1, 0, 1), new Vector3(-1, 0, 1), new Vector3(-1, 0, -1), new Vector3(1, 0, -1) };
            for (int i = 0; i < 4; i++)
            {
                var c = corners[i];
                var bottom = new Vector3(c.x * 0.2f, legY, c.z * 0.2f);
                var top = new Vector3(c.x * 0.17f, topY, c.z * 0.17f);
                s.Beam(ArtMat.WoodDark, bottom, top, 0.05f);

                // contraventamento em X em cada face
                var n = corners[(i + 1) % 4];
                var nb = new Vector3(n.x * 0.2f, legY, n.z * 0.2f);
                var nt = new Vector3(n.x * 0.17f, topY, n.z * 0.17f);
                var mid = Vector3.Lerp(bottom, top, 0.5f);
                var nmid = Vector3.Lerp(nb, nt, 0.5f);
                s.Beam(ArtMat.Wood, bottom, nmid, 0.028f);
                s.Beam(ArtMat.Wood, nb, mid, 0.028f);
                s.Beam(ArtMat.Wood, mid, nmid, 0.03f);
                s.Beam(ArtMat.Wood, top, nt, 0.03f);
            }
            // escada encostada
            s.Beam(ArtMat.Wood, new Vector3(-0.06f, legY, -0.3f), new Vector3(-0.06f, topY, -0.2f), 0.018f);
            s.Beam(ArtMat.Wood, new Vector3(0.06f, legY, -0.3f), new Vector3(0.06f, topY, -0.2f), 0.018f);
            for (int k = 1; k < 8; k++)
            {
                float f = k / 8f;
                var p = Vector3.Lerp(new Vector3(0f, legY, -0.3f), new Vector3(0f, topY, -0.2f), f);
                s.Box(ArtMat.Wood, p, new Vector3(0.12f, 0.012f, 0.014f));
            }

            float ty = topY;
            var t = d.Part(Top, new Vector3(0f, ty, 0f)).Mesh;
            t.Box(ArtMat.Wood, new Vector3(0f, ty + 0.02f, 0f), new Vector3(0.56f, 0.04f, 0.56f));
            const float railH = 0.1f, roofPost = 0.3f;
            for (int i = 0; i < 4; i++)
            {
                var c = corners[i];
                var p = new Vector3(c.x * 0.26f, ty + 0.04f, c.z * 0.26f);
                t.Beam(ArtMat.WoodDark, p, p + Up * roofPost, 0.035f);
                var n = corners[(i + 1) % 4];
                var pn = new Vector3(n.x * 0.26f, ty + 0.04f, n.z * 0.26f);
                t.Beam(ArtMat.Wood, p + Up * railH, pn + Up * railH, 0.022f);
            }
            float roofY = ty + 0.04f + roofPost;
            t.Cylinder(ArtMat.RoofTile, new Vector3(0f, roofY, 0f), 0.46f, 0.0f, 0.24f, 4, flat: true, angleOffset: q);

            float cy = ty + 0.04f;
            var tu = d.Part(Turret, new Vector3(0f, cy, 0f), Top).Mesh;
            tu.Rod(ArtMat.WoodDark, new Vector3(0f, cy, 0f), new Vector3(0f, cy + 0.1f, 0f), 0.02f, 6);
            var stockC = new Vector3(0f, cy + 0.11f, 0.02f);
            tu.Box(ArtMat.WoodDark, stockC, new Vector3(0.05f, 0.035f, 0.32f));
            var bowC = stockC + new Vector3(0f, 0.01f, 0.1f);
            var tipL = bowC + new Vector3(-0.17f, 0f, -0.05f);
            var tipR = bowC + new Vector3(0.17f, 0f, -0.05f);
            tu.Beam(ArtMat.Wood, bowC, tipL, 0.022f);
            tu.Beam(ArtMat.Wood, bowC, tipR, 0.022f);
            var nock = stockC + new Vector3(0f, 0.02f, -0.1f);
            tu.Rod(ArtMat.Leather, tipL, nock, 0.004f, 4);
            tu.Rod(ArtMat.Leather, tipR, nock, 0.004f, 4);
            tu.Rod(ArtMat.Wood, nock, nock + new Vector3(0f, 0f, 0.3f), 0.007f, 5);
            tu.Cylinder(ArtMat.Iron, nock + new Vector3(0f, 0f, 0.3f), 0.013f, 0f, 0.04f, 6, Along(Vector3.forward));
            d.Muzzle = nock + new Vector3(0f, 0f, 0.34f);

            TeamFlag(d, Top, new Vector3(0f, roofY + 0.22f, 0f), 0.22f, 0.16f, 0.1f);
            d.Height = roofY + 0.3f;
            return d;
        }

        // ============================================================= inimigos

        /// <summary>Opções do boneco genérico — cada tipo é o mesmo esqueleto com outra roupa.</summary>
        struct Kit
        {
            public float Height;
            public ArtMat Torso, Legs, Arms;
            public bool Helmet, GreatHelm, Hood;
            public bool Spear, Club, Sword, RoundShield, KiteShield;
            public float Bulk;
        }

        /// <summary>
        /// Boneco de pé. Medidas em "metros de gente" (1,8 m) multiplicadas por k no fim:
        /// é mais fácil acertar proporção pensando em corpo humano do que em fração de célula.
        /// </summary>
        static ModelDef Walker(Kit kit)
        {
            var d = new ModelDef { Anim = AnimKind.Walker };
            float k = kit.Height / 1.8f;
            float bulk = kit.Bulk <= 0f ? 1f : kit.Bulk;
            Vector3 M(float x, float y, float z) => new Vector3(x, y, z) * k;

            var body = d.Part(Body, Vector3.zero).Mesh;
            // tronco e saia da túnica
            body.Box(kit.Torso, M(0f, 1.18f, 0f), M(0.38f * bulk, 0.56f, 0.24f * bulk));
            body.Lathe(kit.Torso, M(0f, 0.74f, 0f), new[] { 0.23f * k * bulk, 0.2f * k * bulk }, new[] { 0f, 0.3f * k }, 8);
            body.Box(ArtMat.Leather, M(0f, 0.95f, 0f), M(0.4f * bulk, 0.06f, 0.26f * bulk));
            body.Box(ArtMat.Bronze, M(0f, 0.95f, 0.13f * bulk), M(0.06f, 0.05f, 0.02f));
            // pescoço e cabeça
            body.Cylinder(ArtMat.Skin, M(0f, 1.44f, 0f), 0.055f * k, 0.05f * k, 0.08f * k, 8);
            body.Sphere(ArtMat.Skin, M(0f, 1.6f, 0.01f), M(0.1f, 0.12f, 0.11f), 10, 8);
            body.Box(ArtMat.Skin, M(0f, 1.59f, 0.11f), M(0.03f, 0.05f, 0.04f)); // nariz: dá direção ao rosto
            if (kit.Helmet)
            {
                // chapéu de ferro (kettle hat): copa + aba larga
                body.Lathe(ArtMat.Iron, M(0f, 1.62f, 0f), new[] { 0.19f * k, 0.12f * k, 0.12f * k, 0.09f * k, 0f },
                    new[] { 0f, 0.01f * k, 0.04f * k, 0.12f * k, 0.16f * k }, 12);
            }
            if (kit.GreatHelm)
            {
                body.Cylinder(ArtMat.Iron, M(0f, 1.47f, 0f), 0.125f * k, 0.12f * k, 0.26f * k, 12);
                body.Box(ArtMat.Hair, M(0f, 1.62f, 0.118f), M(0.16f, 0.018f, 0.01f)); // viseira
                body.Cylinder(ArtMat.Bronze, M(0f, 1.6f, 0f), 0.127f * k, 0.127f * k, 0.01f * k, 12);
            }
            if (kit.Hood)
            {
                body.Sphere(kit.Torso == ArtMat.Leather ? ArtMat.ClothTeam : ArtMat.Cloth, M(0f, 1.63f, -0.02f),
                    M(0.125f, 0.135f, 0.13f), 10, 8);
                body.Lathe(ArtMat.ClothTeam, M(0f, 1.36f, 0f), new[] { 0.2f * k, 0.12f * k }, new[] { 0f, 0.12f * k }, 8);
            }
            if (kit.GreatHelm || kit.Bulk > 1.05f)
            {
                // ombreiras
                body.Sphere(ArtMat.Iron, M(-0.22f * bulk, 1.4f, 0f), M(0.1f, 0.08f, 0.11f), 8, 6);
                body.Sphere(ArtMat.Iron, M(0.22f * bulk, 1.4f, 0f), M(0.1f, 0.08f, 0.11f), 8, 6);
            }

            // pernas: pivô no quadril, balançam em X
            foreach (var (name, sx) in new[] { (LegL, -1f), (LegR, 1f) })
            {
                var hip = M(sx * 0.1f * bulk, 0.9f, 0f);
                var leg = d.Part(name, hip, Body).Mesh;
                leg.Box(kit.Legs, M(sx * 0.1f * bulk, 0.55f, 0f), M(0.14f, 0.7f, 0.15f));
                leg.Box(ArtMat.Leather, M(sx * 0.1f * bulk, 0.1f, 0.03f), M(0.15f, 0.2f, 0.24f));
            }

            // braços: pivô no ombro
            foreach (var (name, sx) in new[] { (ArmL, -1f), (ArmR, 1f) })
            {
                var shoulder = M(sx * 0.25f * bulk, 1.4f, 0f);
                var arm = d.Part(name, shoulder, Body).Mesh;
                arm.Box(kit.Arms, M(sx * 0.25f * bulk, 1.13f, 0f), M(0.11f, 0.56f, 0.12f));
                arm.Sphere(ArtMat.Skin, M(sx * 0.25f * bulk, 0.82f, 0.02f), M(0.055f, 0.06f, 0.055f), 8, 6);

                var hand = M(sx * 0.25f * bulk, 0.82f, 0.02f);
                if (sx > 0f && kit.Spear)
                {
                    arm.Rod(ArtMat.Wood, hand + M(0f, -0.7f, -0.2f), hand + M(0f, 1.3f, 0.35f), 0.018f * k, 6);
                    var tip = hand + M(0f, 1.3f, 0.35f);
                    arm.Cylinder(ArtMat.Iron, tip, 0.03f * k, 0f, 0.2f * k, 6,
                        Along(M(0f, 2f, 0.55f)));
                }
                if (sx > 0f && kit.Club)
                {
                    arm.Rod(ArtMat.WoodDark, hand + M(0f, -0.1f, 0f), hand + M(0f, 0.2f, 0.5f), 0.02f * k, 6);
                    arm.Box(ArtMat.Iron, hand + M(0f, 0.22f, 0.5f), M(0.03f, 0.14f, 0.12f),
                        Quaternion.Euler(-30f, 0f, 0f));
                }
                if (sx > 0f && kit.Sword)
                {
                    arm.Box(ArtMat.Leather, hand, M(0.035f, 0.035f, 0.14f));
                    arm.Box(ArtMat.Bronze, hand + M(0f, 0f, 0.08f), M(0.2f, 0.03f, 0.03f));
                    arm.Box(ArtMat.Iron, hand + M(0f, 0f, 0.5f), M(0.05f, 0.012f, 0.8f));
                }
                if (sx < 0f && kit.RoundShield)
                {
                    var c = hand + M(-0.08f, 0.2f, 0.06f);
                    arm.Cylinder(ArtMat.WoodDark, c, 0.24f * k, 0.24f * k, 0.03f * k, 16, Along(Vector3.left));
                    arm.Cylinder(ArtMat.ClothTeam, c + M(-0.031f, 0f, 0f), 0.2f * k, 0.2f * k, 0.004f * k, 16,
                        Along(Vector3.left));
                    arm.Sphere(ArtMat.Iron, c + M(-0.04f, 0f, 0f), M(0.05f, 0.05f, 0.05f), 8, 6);
                }
                if (sx < 0f && kit.KiteShield)
                {
                    // escudo de pipa: tábua pintada nas cores do time, borda de ferro
                    var c = hand + M(-0.1f, 0.25f, 0.1f);
                    var rot = Quaternion.Euler(0f, 0f, 0f);
                    arm.Box(ArtMat.ClothTeam, c, M(0.04f, 0.62f, 0.4f), rot);
                    arm.Box(ArtMat.Iron, c + M(0f, 0.31f, 0f), M(0.05f, 0.03f, 0.42f), rot);
                    // ponta de baixo da pipa: triângulo nas duas faces da tábua
                    foreach (float fx in new[] { -0.021f, 0.021f })
                        arm.DoubleTri(ArtMat.ClothTeam, c + M(fx, -0.31f, -0.2f), c + M(fx, -0.31f, 0.2f),
                            c + M(fx, -0.62f, 0f));
                    arm.Box(ArtMat.Bronze, c + M(-0.025f, 0.05f, 0f), M(0.01f, 0.36f, 0.05f));
                    arm.Box(ArtMat.Bronze, c + M(-0.025f, 0.12f, 0f), M(0.01f, 0.05f, 0.28f));
                }
            }

            d.Height = kit.Height * 1.02f;
            d.Stride = kit.Height * 0.55f;
            return d;
        }

        /// <summary>Recruta: lanceiro de gibão, chapéu de ferro e broquel. O inimigo-régua.</summary>
        static ModelDef Spearman() => Walker(new Kit
        {
            Height = 0.62f, Torso = ArtMat.ClothTeam, Legs = ArtMat.Cloth, Arms = ArtMat.ClothTeam,
            Helmet = true, Spear = true, RoundShield = true,
        });

        /// <summary>Enxame: escaramuçador leve de couro e capuz, com maça — pequeno e em bando.</summary>
        static ModelDef Skirmisher() => Walker(new Kit
        {
            Height = 0.5f, Torso = ArtMat.Leather, Legs = ArtMat.Hair, Arms = ArtMat.Leather,
            Hood = true, Club = true, Bulk = 0.9f,
        });

        /// <summary>Couraçado: cavaleiro a pé de armadura, elmo fechado e escudo de pipa.</summary>
        static ModelDef Knight() => Walker(new Kit
        {
            Height = 0.72f, Torso = ArtMat.Iron, Legs = ArtMat.Iron, Arms = ArtMat.Iron,
            GreatHelm = true, Sword = true, KiteShield = true, Bulk = 1.18f,
        });

        /// <summary>Corredor: cavaleiro montado de lança. Rápido, e a silhueta comprida grita "rápido".</summary>
        static ModelDef Horseman()
        {
            var d = new ModelDef { Anim = AnimKind.Horse };
            const float k = 0.34f;
            Vector3 M(float x, float y, float z) => new Vector3(x, y, z) * k;

            var body = d.Part(Body, Vector3.zero).Mesh;
            // cavalo: tronco, pescoço, cabeça
            body.Sphere(ArtMat.HorseCoat, M(0f, 1.3f, 0f), M(0.34f, 0.36f, 0.78f), 14, 10);
            body.Lathe(ArtMat.HorseCoat, M(0f, 1.42f, 0.6f), new[] { 0.24f * k, 0.2f * k, 0.15f * k },
                new[] { 0f, 0.35f * k, 0.6f * k }, 10, Along(new Vector3(0f, 1f, 0.75f)));
            body.Sphere(ArtMat.HorseCoat, M(0f, 1.86f, 1.12f), M(0.13f, 0.15f, 0.3f), 10, 8);
            body.Sphere(ArtMat.HorseCoat, M(0f, 1.78f, 1.36f), M(0.1f, 0.11f, 0.12f), 8, 6);
            foreach (float sx in new[] { -1f, 1f })
            {
                body.Cylinder(ArtMat.HorseCoat, M(sx * 0.07f, 2.0f, 1.0f), 0.04f * k, 0f, 0.14f * k, 5); // orelhas
                body.Sphere(ArtMat.Hair, M(sx * 0.12f, 1.9f, 1.22f), M(0.025f, 0.025f, 0.025f), 6, 4); // olhos
            }
            // crina e cauda
            body.Beam(ArtMat.Hair, M(0f, 1.7f, 0.55f), M(0f, 2.05f, 0.95f), 0.08f * k);
            body.Beam(ArtMat.Hair, M(0f, 1.45f, -0.72f), M(0f, 0.85f, -0.98f), 0.1f * k);
            // xairel nas cores do time + sela
            body.Box(ArtMat.ClothTeam, M(0f, 1.5f, -0.05f), M(0.72f, 0.3f, 0.7f));
            body.Box(ArtMat.Leather, M(0f, 1.66f, -0.05f), M(0.4f, 0.1f, 0.5f));

            // cavaleiro sentado (parte do corpo: não anda, cavalga)
            body.Box(ArtMat.ClothTeam, M(0f, 2.05f, -0.05f), M(0.36f, 0.56f, 0.24f));
            body.Box(ArtMat.Iron, M(0f, 2.07f, 0.06f), M(0.3f, 0.4f, 0.04f));
            body.Sphere(ArtMat.Skin, M(0f, 2.46f, -0.03f), M(0.1f, 0.12f, 0.11f), 10, 8);
            body.Lathe(ArtMat.Iron, M(0f, 2.46f, -0.03f), new[] { 0.12f * k, 0.12f * k, 0.08f * k, 0f },
                new[] { 0f, 0.08f * k, 0.15f * k, 0.2f * k }, 10);
            foreach (float sx in new[] { -1f, 1f })
            {
                body.Beam(ArtMat.Cloth, M(sx * 0.2f, 1.78f, 0.0f), M(sx * 0.3f, 1.35f, 0.18f), 0.13f * k);
                body.Box(ArtMat.Leather, M(sx * 0.31f, 1.25f, 0.2f), M(0.12f, 0.16f, 0.2f));
                body.Beam(ArtMat.ClothTeam, M(sx * 0.24f, 2.28f, -0.05f), M(sx * 0.27f, 1.95f, 0.2f), 0.1f * k);
            }
            // lança em riste
            body.Rod(ArtMat.Wood, M(0.26f, 1.9f, -0.7f), M(0.24f, 2.3f, 1.9f), 0.025f * k, 6);
            body.Cylinder(ArtMat.Iron, M(0.24f, 2.3f, 1.9f), 0.04f * k, 0f, 0.25f * k, 6, Along(new Vector3(0f, 0.15f, 1f)));
            body.DoubleTri(ArtMat.ClothTeam, M(0.24f, 2.28f, 1.6f), M(0.24f, 2.2f, 1.62f), M(0.24f, 2.22f, 1.25f));

            // quatro pernas: pivô no ombro/anca
            var legs = new[] { ("LegFL", -1f, 0.55f), ("LegFR", 1f, 0.55f), ("LegBL", -1f, -0.5f), ("LegBR", 1f, -0.5f) };
            foreach (var (name, sx, z) in legs)
            {
                var pivot = M(sx * 0.19f, 1.15f, z);
                var leg = d.Part(name, pivot, Body).Mesh;
                leg.Lathe(ArtMat.HorseCoat, M(sx * 0.19f, 0.12f, z), new[] { 0.06f * k, 0.065f * k, 0.1f * k, 0.13f * k },
                    new[] { 0f, 0.5f * k, 0.75f * k, 1.05f * k }, 8);
                leg.Cylinder(ArtMat.Hair, M(sx * 0.19f, 0f, z + 0.02f), 0.08f * k, 0.07f * k, 0.13f * k, 8);
            }

            d.Height = 2.75f * k;
            d.Stride = 0.55f;
            return d;
        }

        /// <summary>Planador: asa de morcego à Da Vinci, piloto deitado embaixo. Voa sobre a fronteira.</summary>
        static ModelDef Glider()
        {
            var d = new ModelDef { Anim = AnimKind.Glider };
            const float alt = 1.15f;
            var body = d.Part(Body, Vector3.zero).Mesh;

            // piloto deitado, pendurado na armação
            var pc = new Vector3(0f, alt - 0.1f, -0.02f);
            body.Box(ArtMat.ClothTeam, pc, new Vector3(0.1f, 0.07f, 0.2f));
            body.Sphere(ArtMat.Skin, pc + new Vector3(0f, 0.01f, 0.14f), new Vector3(0.04f, 0.04f, 0.045f), 8, 6);
            body.Lathe(ArtMat.Leather, pc + new Vector3(0f, 0.02f, 0.14f), new[] { 0.045f, 0.03f, 0f },
                new[] { 0f, 0.03f, 0.045f }, 8);
            body.Box(ArtMat.Cloth, pc + new Vector3(-0.03f, -0.005f, -0.16f), new Vector3(0.035f, 0.035f, 0.16f));
            body.Box(ArtMat.Cloth, pc + new Vector3(0.03f, -0.005f, -0.16f), new Vector3(0.035f, 0.035f, 0.16f));
            // cordas até a asa
            var hang = new Vector3(0f, alt + 0.04f, 0f);
            body.Rod(ArtMat.Leather, pc + new Vector3(-0.04f, 0.03f, 0.05f), hang + new Vector3(-0.08f, 0f, 0.05f), 0.004f, 4);
            body.Rod(ArtMat.Leather, pc + new Vector3(0.04f, 0.03f, 0.05f), hang + new Vector3(0.08f, 0f, 0.05f), 0.004f, 4);
            body.Rod(ArtMat.Leather, pc + new Vector3(0f, 0.03f, -0.08f), hang + new Vector3(0f, 0f, -0.12f), 0.004f, 4);

            var w = d.Part(Wing, hang, Body).Mesh;
            // longarina central e bordo de ataque em arco
            w.Rod(ArtMat.WoodDark, hang + new Vector3(0f, 0f, 0.28f), hang + new Vector3(0f, 0f, -0.36f), 0.012f, 6);
            const float span = 0.55f;
            int ribs = 5;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 prevLead = hang + new Vector3(0f, 0f, 0.26f);
                Vector3 prevTrail = hang + new Vector3(0f, 0f, -0.34f);
                for (int r = 1; r <= ribs; r++)
                {
                    float f = r / (float)ribs;
                    float x = side * span * f;
                    float lift = 0.1f * f * f; // diedro: a ponta sobe
                    var lead = hang + new Vector3(x, lift, 0.26f - 0.12f * f * f);
                    // borda de fuga recortada, como asa de morcego
                    float scallop = (r % 2 == 0) ? 0.06f : 0f;
                    var trail = hang + new Vector3(x, lift, -0.34f + 0.3f * f + scallop);
                    w.Rod(ArtMat.WoodDark, prevLead, lead, 0.008f, 5);
                    w.Rod(ArtMat.Wood, lead, trail, 0.005f, 4);
                    // lona crua; só a ponta leva a cor do time — de longe lê como asa, não como toldo
                    w.DoubleQuad(r == ribs ? ArtMat.ClothTeam : ArtMat.Cloth, prevLead, lead, trail, prevTrail);
                    prevLead = lead;
                    prevTrail = trail;
                }
            }
            // leme de cauda
            w.DoubleTri(ArtMat.ClothTeam, hang + new Vector3(0f, 0f, -0.3f), hang + new Vector3(0f, 0.14f, -0.42f),
                hang + new Vector3(0f, 0f, -0.46f));

            d.Height = alt + 0.25f;
            return d;
        }

        /// <summary>Colosso: torre de cerco sobre rodas, frente coberta de couro, ponte no alto.</summary>
        static ModelDef SiegeTower()
        {
            var d = new ModelDef { Anim = AnimKind.Wheels };
            var body = d.Part(Body, Vector3.zero).Mesh;
            const float bottom = 0.16f, h = 1.12f, hw = 0.33f;
            float top = bottom + h;

            body.Box(ArtMat.Wood, new Vector3(0f, bottom + h * 0.5f, 0f), new Vector3(hw * 2f, h, hw * 2f));
            // couro cru pregado na frente e nos lados contra flecha incendiária
            body.Box(ArtMat.Leather, new Vector3(0f, bottom + h * 0.5f, hw + 0.012f), new Vector3(hw * 2.02f, h * 0.98f, 0.02f));
            foreach (float sx in new[] { -1f, 1f })
                body.Box(ArtMat.Leather, new Vector3(sx * (hw + 0.012f), bottom + h * 0.6f, 0.1f),
                    new Vector3(0.02f, h * 0.7f, hw * 1.3f));
            // quinas e cintas de viga
            foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                body.Box(ArtMat.WoodDark, new Vector3(sx * hw, bottom + h * 0.5f + 0.02f, sz * hw), new Vector3(0.07f, h + 0.08f, 0.07f));
            for (int i = 0; i <= 3; i++)
            {
                float y = bottom + 0.02f + i * (h - 0.04f) / 3f;
                body.Box(ArtMat.WoodDark, new Vector3(0f, y, hw + 0.03f), new Vector3(hw * 2.1f, 0.05f, 0.03f));
                body.Box(ArtMat.WoodDark, new Vector3(0f, y, -hw - 0.02f), new Vector3(hw * 2.1f, 0.05f, 0.03f));
                body.Box(ArtMat.WoodDark, new Vector3(hw + 0.02f, y, 0f), new Vector3(0.03f, 0.05f, hw * 2.1f));
                body.Box(ArtMat.WoodDark, new Vector3(-hw - 0.02f, y, 0f), new Vector3(0.03f, 0.05f, hw * 2.1f));
            }
            // plataforma com parapeito de tábuas
            body.Box(ArtMat.WoodDark, new Vector3(0f, top + 0.02f, 0f), new Vector3(hw * 2.25f, 0.04f, hw * 2.25f));
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                for (int m = -2; m <= 2; m++)
                {
                    var tangent = new Vector3(-dir.z, 0f, dir.x);
                    var p = dir * (hw * 1.08f) + tangent * (m * 0.14f) + new Vector3(0f, top + 0.1f, 0f);
                    body.Box(ArtMat.Wood, p, new Vector3(0.1f, 0.13f, 0.1f));
                }
            }
            // ponte levadiça erguida na frente, com correntes
            body.Box(ArtMat.WoodDark, new Vector3(0f, top - 0.2f, hw + 0.06f), new Vector3(hw * 1.4f, 0.44f, 0.035f));
            body.Rod(ArtMat.Iron, new Vector3(-0.18f, top - 0.0f, hw + 0.08f), new Vector3(-0.2f, top + 0.2f, hw), 0.006f, 4);
            body.Rod(ArtMat.Iron, new Vector3(0.18f, top - 0.0f, hw + 0.08f), new Vector3(0.2f, top + 0.2f, hw), 0.006f, 4);
            // estandarte
            body.Rod(ArtMat.WoodDark, new Vector3(-0.25f, top, -0.25f), new Vector3(-0.25f, top + 0.45f, -0.25f), 0.012f, 6);
            body.DoubleQuad(ArtMat.ClothTeam, new Vector3(-0.24f, top + 0.26f, -0.25f), new Vector3(-0.24f, top + 0.44f, -0.25f),
                new Vector3(0.02f, top + 0.42f, -0.25f), new Vector3(0.0f, top + 0.27f, -0.25f));
            // eixos
            body.Rod(ArtMat.WoodDark, new Vector3(-hw - 0.06f, 0.14f, 0.24f), new Vector3(hw + 0.06f, 0.14f, 0.24f), 0.02f, 6);
            body.Rod(ArtMat.WoodDark, new Vector3(-hw - 0.06f, 0.14f, -0.24f), new Vector3(hw + 0.06f, 0.14f, -0.24f), 0.02f, 6);

            int wi = 0;
            foreach (float sz in new[] { 0.24f, -0.24f })
            foreach (float sx in new[] { -1f, 1f })
            {
                var axle = new Vector3(sx * (hw + 0.07f), 0.14f, sz);
                var wheel = d.Part("Wheel" + wi++, axle, Body).Mesh;
                var r = Along(Vector3.right);
                wheel.Cylinder(ArtMat.Wood, axle - Vector3.right * 0.03f, 0.14f, 0.14f, 0.06f, 16, r);
                wheel.Cylinder(ArtMat.Iron, axle - Vector3.right * 0.032f, 0.145f, 0.145f, 0.064f, 16, r, false, false);
                wheel.Cylinder(ArtMat.Iron, axle + Vector3.right * 0.03f * sx, 0.035f, 0.035f, 0.02f, 8, Along(Vector3.right * sx));
                // raios aparentes: ajudam a VER a roda girar
                for (int s = 0; s < 3; s++)
                {
                    var rot = Quaternion.AngleAxis(s * 60f, Vector3.right);
                    var a = rot * new Vector3(0f, 0.12f, 0f);
                    wheel.Beam(ArtMat.WoodDark, axle + a + Vector3.right * 0.034f * sx,
                        axle - a + Vector3.right * 0.034f * sx, 0.02f);
                }
            }

            d.Height = top + 0.5f;
            d.Stride = 0.14f; // raio da roda
            return d;
        }

        // ============================================================ projéteis

        static ModelDef Ball(float r)
        {
            var d = new ModelDef();
            d.Part(Body, Vector3.zero).Mesh.Sphere(ArtMat.Iron, Vector3.zero, Vector3.one * r, 10, 7);
            return d;
        }

        static ModelDef IceShard()
        {
            var d = new ModelDef();
            d.Part(Body, Vector3.zero).Mesh.Lathe(ArtMat.Ice, new Vector3(0f, 0f, -0.12f),
                new[] { 0f, 0.035f, 0.03f, 0f }, new[] { 0f, 0.05f, 0.16f, 0.24f }, 6, Along(Vector3.forward), flat: true);
            return d;
        }

        static ModelDef Bolt()
        {
            var d = new ModelDef();
            var b = d.Part(Body, Vector3.zero).Mesh;
            b.Rod(ArtMat.Wood, new Vector3(0f, 0f, -0.16f), new Vector3(0f, 0f, 0.13f), 0.009f, 5);
            b.Cylinder(ArtMat.Iron, new Vector3(0f, 0f, 0.13f), 0.02f, 0f, 0.06f, 6, Along(Vector3.forward));
            b.DoubleTri(ArtMat.Cloth, new Vector3(0f, 0f, -0.1f), new Vector3(0f, 0.04f, -0.17f), new Vector3(0f, 0f, -0.16f));
            b.DoubleTri(ArtMat.Cloth, new Vector3(0f, 0f, -0.1f), new Vector3(0.04f, 0f, -0.17f), new Vector3(0f, 0f, -0.16f));
            return d;
        }

        // ======================================================== base e origem

        /// <summary>Fortaleza: torre de menagem com torreões, porta virada para o inimigo (+Z).</summary>
        static ModelDef BuildKeep()
        {
            var d = new ModelDef();
            var b = d.Part(Body, Vector3.zero).Mesh;
            b.Box(ArtMat.StoneDark, new Vector3(0f, 0.05f, 0f), new Vector3(1.05f, 0.1f, 1.05f));
            const float hw = 0.36f, h = 1.0f, y0 = 0.1f;
            b.Box(ArtMat.Stone, new Vector3(0f, y0 + h * 0.5f, 0f), new Vector3(hw * 2f, h, hw * 2f));
            b.Box(ArtMat.StoneDark, new Vector3(0f, y0 + h - 0.02f, 0f), new Vector3(hw * 2.12f, 0.06f, hw * 2.12f));
            // ameias nos quatro lados
            float ty = y0 + h + 0.01f;
            for (int side = 0; side < 4; side++)
            {
                var rot = Quaternion.Euler(0f, side * 90f, 0f);
                b.Box(ArtMat.Stone, rot * new Vector3(0f, ty + 0.04f, hw * 1.02f), rot * new Vector3(hw * 2.05f, 0.08f, 0.06f));
                for (int m = -2; m <= 2; m++)
                    b.Box(ArtMat.Stone, rot * new Vector3(m * 0.14f, ty + 0.12f, hw * 1.02f), rot * new Vector3(0.08f, 0.08f, 0.06f));
            }
            b.Box(ArtMat.Slate, new Vector3(0f, ty + 0.01f, 0f), new Vector3(hw * 1.9f, 0.02f, hw * 1.9f));
            // torreões redondos nas quinas, com telhado cônico
            foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {
                var c = new Vector3(sx * hw, y0, sz * hw);
                b.Cylinder(ArtMat.Stone, c, 0.14f, 0.13f, h + 0.18f, 14);
                b.Cylinder(ArtMat.StoneDark, c + Up * (h + 0.18f), 0.13f, 0.155f, 0.04f, 14);
                b.Cylinder(ArtMat.Slate, c + Up * (h + 0.22f), 0.165f, 0f, 0.3f, 14, capTop: false);
                b.Sphere(ArtMat.Bronze, c + Up * (h + 0.53f), Vector3.one * 0.018f, 6, 4);
            }
            // portão de madeira com ferragens, e janelas
            b.Box(ArtMat.WoodDark, new Vector3(0f, y0 + 0.16f, hw + 0.005f), new Vector3(0.24f, 0.32f, 0.02f));
            b.Cylinder(ArtMat.WoodDark, new Vector3(0f, y0 + 0.32f, hw - 0.005f), 0.12f, 0.12f, 0.02f, 12,
                Along(Vector3.forward));
            b.Box(ArtMat.Iron, new Vector3(0f, y0 + 0.12f, hw + 0.017f), new Vector3(0.25f, 0.02f, 0.006f));
            b.Box(ArtMat.Iron, new Vector3(0f, y0 + 0.26f, hw + 0.017f), new Vector3(0.25f, 0.02f, 0.006f));
            for (int side = 0; side < 4; side++)
            {
                var rot = Quaternion.Euler(0f, side * 90f, 0f);
                b.Box(ArtMat.Hair, rot * new Vector3(0.12f, y0 + 0.66f, hw + 0.004f), rot * new Vector3(0.05f, 0.11f, 0.01f));
                b.Box(ArtMat.Hair, rot * new Vector3(-0.12f, y0 + 0.66f, hw + 0.004f), rot * new Vector3(0.05f, 0.11f, 0.01f));
            }
            // estandarte grande: a peça "Flag" balança ao vento
            var foot = new Vector3(0f, ty, 0f);
            var f = d.Part(Flag, foot, Body).Mesh;
            f.Rod(ArtMat.WoodDark, foot, foot + Up * 0.62f, 0.016f, 6);
            f.Sphere(ArtMat.Bronze, foot + Up * 0.63f, Vector3.one * 0.022f, 8, 6);
            f.DoubleQuad(ArtMat.ClothTeam, foot + new Vector3(0.015f, 0.36f, 0f), foot + new Vector3(0.015f, 0.6f, 0f),
                foot + new Vector3(0.42f, 0.58f, 0f), foot + new Vector3(0.4f, 0.38f, 0f));

            d.Height = ty + 0.65f;
            return d;
        }

        /// <summary>Acampamento de onde o inimigo sai: chão batido, portal de toras e duas tochas.</summary>
        static ModelDef BuildCamp()
        {
            var d = new ModelDef();
            var b = d.Part(Body, Vector3.zero).Mesh;
            b.GroundQuad(ArtMat.Dirt, new Vector3(0f, 0.006f, 0f), 0.48f, 0.48f);
            foreach (float sx in new[] { -1f, 1f })
            {
                b.Cylinder(ArtMat.Bark, new Vector3(sx * 0.36f, 0f, 0f), 0.05f, 0.045f, 0.62f, 8);
                b.Cylinder(ArtMat.Bark, new Vector3(sx * 0.36f, 0.62f, 0f), 0.045f, 0f, 0.07f, 8);
                // tocha: haste + cabeça de piche
                var tp = new Vector3(sx * 0.46f, 0f, 0.22f);
                b.Rod(ArtMat.WoodDark, tp, tp + Up * 0.34f, 0.012f, 5);
                b.Cylinder(ArtMat.Hair, tp + Up * 0.32f, 0.022f, 0.028f, 0.05f, 6);
            }
            b.Rod(ArtMat.Bark, new Vector3(-0.44f, 0.55f, 0f), new Vector3(0.44f, 0.55f, 0f), 0.045f, 8);
            b.Rod(ArtMat.Bark, new Vector3(-0.36f, 0.3f, 0.02f), new Vector3(0.36f, 0.5f, 0.02f), 0.025f, 6);
            // estacas afiadas atrás
            for (int i = -3; i <= 3; i++)
            {
                var p = new Vector3(i * 0.13f, 0f, -0.4f);
                float hgt = 0.22f + 0.05f * ((i * 7 + 3) % 3);
                b.Cylinder(ArtMat.Bark, p, 0.035f, 0.035f, hgt, 6);
                b.Cylinder(ArtMat.Wood, p + Up * hgt, 0.035f, 0f, 0.07f, 6);
            }
            d.Height = 0.8f;
            return d;
        }

        // ============================================================== cenário

        /// <summary>Pinheiro: tronco + saias de galhos empilhadas, com variação por semente.</summary>
        public static void Pine(MeshBuilder mb, Vector3 at, float size, int seed)
        {
            float lean = (ProcNoise.Hash(seed, 1, 5) - 0.5f) * 8f;
            mb.SetTransform(at, Quaternion.Euler(lean, ProcNoise.Hash(seed, 2, 5) * 360f, 0f));
            mb.Cylinder(ArtMat.Bark, Vector3.zero, 0.07f * size, 0.04f * size, 0.5f * size, 7, capBottom: false);
            int tiers = 4;
            for (int t = 0; t < tiers; t++)
            {
                float f = t / (float)tiers;
                float y = (0.3f + f * 1.05f) * size;
                float r = (0.5f - 0.36f * f) * size * (0.9f + 0.2f * ProcNoise.Hash(seed, t, 9));
                mb.Lathe(ArtMat.Foliage, new Vector3(0f, y, 0f), new[] { r * 0.2f, r, r * 0.85f, 0f },
                    new[] { 0f, 0.05f * size, 0.12f * size, 0.62f * size }, 8, null, false, t * 0.7f);
            }
            mb.ResetTransform();
        }

        /// <summary>Árvore de copa larga (carvalho): tronco e três ou quatro massas de folhagem.</summary>
        public static void Oak(MeshBuilder mb, Vector3 at, float size, int seed)
        {
            mb.SetTransform(at, Quaternion.Euler(0f, ProcNoise.Hash(seed, 2, 7) * 360f, 0f));
            mb.Cylinder(ArtMat.Bark, Vector3.zero, 0.09f * size, 0.06f * size, 0.75f * size, 8, capBottom: false);
            mb.Rod(ArtMat.Bark, new Vector3(0f, 0.55f * size, 0f), new Vector3(0.22f * size, 0.9f * size, 0.05f * size), 0.035f * size);
            mb.Rod(ArtMat.Bark, new Vector3(0f, 0.6f * size, 0f), new Vector3(-0.18f * size, 0.95f * size, -0.1f * size), 0.035f * size);
            int blobs = 3 + (seed & 1);
            for (int i = 0; i < blobs; i++)
            {
                float a = i * Mathf.PI * 2f / blobs + ProcNoise.Hash(seed, i, 3);
                var c = new Vector3(Mathf.Cos(a) * 0.2f, 1.0f + 0.15f * ProcNoise.Hash(seed, i, 4), Mathf.Sin(a) * 0.2f) * size;
                float r = (0.36f + 0.12f * ProcNoise.Hash(seed, i, 6)) * size;
                mb.Sphere(ArtMat.Foliage, c, new Vector3(r, r * 0.8f, r), 8, 6, 0.14f, seed * 11 + i);
            }
            mb.Sphere(ArtMat.Foliage, new Vector3(0f, 1.25f * size, 0f), Vector3.one * 0.34f * size, 8, 6, 0.14f, seed * 13);
            mb.ResetTransform();
        }

        public static void Boulder(MeshBuilder mb, Vector3 at, float size, int seed)
        {
            mb.SetTransform(at, Quaternion.Euler(0f, ProcNoise.Hash(seed, 1, 2) * 360f, 0f));
            var r = new Vector3(1f + 0.4f * ProcNoise.Hash(seed, 3, 2), 0.6f, 0.9f) * size;
            mb.Sphere(ArtMat.Rock, new Vector3(0f, r.y * 0.35f, 0f), r, 9, 6, 0.22f, seed);
            mb.ResetTransform();
        }

        public static void Bush(MeshBuilder mb, Vector3 at, float size, int seed)
        {
            mb.SetTransform(at, Quaternion.identity);
            mb.Sphere(ArtMat.Foliage, new Vector3(0f, 0.12f * size, 0f), new Vector3(0.3f, 0.2f, 0.26f) * size, 8, 5, 0.2f, seed);
            mb.Sphere(ArtMat.Foliage, new Vector3(0.15f * size, 0.1f * size, 0.08f * size), new Vector3(0.2f, 0.15f, 0.2f) * size,
                7, 5, 0.2f, seed + 3);
            mb.ResetTransform();
        }
    }
}
