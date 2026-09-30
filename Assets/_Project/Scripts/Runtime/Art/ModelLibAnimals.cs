using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Os bichos que o atacante manda, do rato ao elefante. Mesma regra dos outros modelos:
    /// olham para +Z, pisam em Y = 0, e cada um tem SILHUETA própria no zoom do jogo —
    /// o tamanho já conta a história (rato miúdo, elefante do tamanho de uma torre).
    ///
    /// A cor do time vai no que um bicho de guerra usaria: coleira nos pequenos, manta
    /// no lombo dos grandes, e a torre de combate no elefante.
    /// </summary>
    public static partial class ModelLib
    {
        /// <summary>Proporções de um quadrúpede. Medidas em unidades de mundo.</summary>
        struct Quad
        {
            public float Hip;          // altura do topo da perna (ombro e anca)
            public float Len, W, H;    // meio comprimento, meia largura e meia altura do tronco
            public float LegR;         // raio da perna no alto
            public float HeadR;        // raio da cabeça
            public float Snout;        // comprimento do focinho (0 = sem focinho)
            public float NeckUp;       // quanto a cabeça fica acima do meio do tronco
            public ArtMat Coat, Paw, Nose;
            public float Stride;       // chão coberto por ciclo de galope
        }

        /// <summary>Onde ficaram as peças principais, para cada bicho pendurar o que é dele.</summary>
        struct QuadParts
        {
            public ModelDef Def;
            public MeshBuilder Body;
            public Vector3 Torso;      // centro do tronco
            public Vector3 Head;       // centro da cabeça
            public Vector3 SnoutTip;   // ponta do focinho
        }

        static QuadParts Quadruped(Quad q)
        {
            var d = new ModelDef { Anim = AnimKind.Horse, Stride = q.Stride };
            var b = d.Part(Body, Vector3.zero).Mesh;

            // tronco: peito, lombo e anca, para não virar um ovo
            var torso = new Vector3(0f, q.Hip + q.H * 0.55f, 0f);
            b.Sphere(q.Coat, torso, new Vector3(q.W, q.H, q.Len), 14, 10);
            b.Sphere(q.Coat, torso + new Vector3(0f, q.H * 0.08f, q.Len * 0.45f), new Vector3(q.W * 1.02f, q.H * 1.02f, q.Len * 0.55f), 12, 9);
            b.Sphere(q.Coat, torso + new Vector3(0f, q.H * 0.02f, -q.Len * 0.45f), new Vector3(q.W * 0.98f, q.H * 0.97f, q.Len * 0.55f), 12, 9);

            // pescoço e cabeça
            var head = torso + new Vector3(0f, q.NeckUp, q.Len * 0.95f + q.HeadR * 0.55f);
            var neckBase = torso + new Vector3(0f, q.H * 0.2f, q.Len * 0.6f);
            b.Rod(q.Coat, neckBase, head, Mathf.Min(q.W, q.H) * 0.7f, 10);
            b.Sphere(q.Coat, head, new Vector3(q.HeadR * 0.9f, q.HeadR * 0.88f, q.HeadR), 12, 9);
            var tip = head + new Vector3(0f, -q.HeadR * 0.2f, q.HeadR * 0.6f + q.Snout);
            if (q.Snout > 0f)
            {
                b.Rod(q.Coat, head + new Vector3(0f, -q.HeadR * 0.15f, q.HeadR * 0.4f), tip, q.HeadR * 0.5f, 10);
                b.Sphere(q.Coat, tip, Vector3.one * q.HeadR * 0.48f, 10, 7);
                b.Sphere(q.Nose, tip + new Vector3(0f, q.HeadR * 0.12f, q.HeadR * 0.38f), Vector3.one * q.HeadR * 0.2f, 8, 6);
            }
            foreach (float sx in new[] { -1f, 1f })
                b.Sphere(ArtMat.Hair, head + new Vector3(sx * q.HeadR * 0.55f, q.HeadR * 0.25f, q.HeadR * 0.62f),
                    Vector3.one * q.HeadR * 0.13f, 6, 4);

            // quatro patas, pivô no ombro/anca: a vista galopa girando em X
            var legs = new[] { ("LegFL", -1f, 1f), ("LegFR", 1f, 1f), ("LegBL", -1f, -1f), ("LegBR", 1f, -1f) };
            foreach (var (name, sx, sz) in legs)
            {
                var top = new Vector3(sx * q.W * 0.6f, q.Hip + q.H * 0.2f, sz * q.Len * 0.62f);
                var leg = d.Part(name, top, Body).Mesh;
                var foot = new Vector3(top.x, 0f, top.z);
                float h = top.y;
                leg.Lathe(q.Coat, foot, new[] { q.LegR * 0.62f, q.LegR * 0.6f, q.LegR * 0.8f, q.LegR },
                    new[] { 0f, h * 0.45f, h * 0.75f, h }, 9);
                leg.Sphere(q.Paw, foot + new Vector3(0f, q.LegR * 0.3f, q.LegR * 0.15f),
                    new Vector3(q.LegR * 0.72f, q.LegR * 0.38f, q.LegR * 0.85f), 8, 5);
            }

            d.Height = Mathf.Max(torso.y + q.H, head.y + q.HeadR);
            return new QuadParts { Def = d, Body = b, Torso = torso, Head = head, SnoutTip = tip };
        }

        /// <summary>Coleira na cor do time: é por ela que se sabe de quem é o bicho pequeno.</summary>
        static void Collar(QuadParts p, Quad q)
        {
            var at = Vector3.Lerp(p.Torso + new Vector3(0f, q.H * 0.2f, q.Len * 0.6f), p.Head, 0.45f);
            float r = Mathf.Min(q.W, q.H) * 0.78f;
            var dir = (p.Head - p.Torso).normalized;
            p.Body.Cylinder(ArtMat.ClothTeam, at - dir * r * 0.2f, r, r, r * 0.45f, 12, Along(dir));
        }

        /// <summary>Manta do time sobre o lombo (casca um pouco maior que o tronco).</summary>
        static void Blanket(QuadParts p, Quad q)
        {
            p.Body.Sphere(ArtMat.ClothTeam, p.Torso + new Vector3(0f, q.H * 0.12f, -q.Len * 0.05f),
                new Vector3(q.W * 1.06f, q.H * 0.95f, q.Len * 0.62f), 14, 9);
        }

        static void Ears(QuadParts p, Quad q, ArtMat m, float size, bool pointy, float spread = 0.55f, float droop = 0f)
        {
            foreach (float sx in new[] { -1f, 1f })
            {
                var at = p.Head + new Vector3(sx * q.HeadR * spread, q.HeadR * (0.75f - droop), -q.HeadR * 0.1f);
                if (pointy)
                    p.Body.Cylinder(m, at, size * 0.5f, 0f, size, 6, Quaternion.Euler(-12f, 0f, -sx * 14f));
                else
                    p.Body.Sphere(m, at + new Vector3(sx * size * 0.2f, -droop * q.HeadR, 0f),
                        new Vector3(size * 0.55f, size * (droop > 0f ? 1.1f : 0.8f), size * 0.2f), 8, 6);
            }
        }

        /// <summary>Rabo como uma curva de bastões: sai da anca e cai com <paramref name="bend"/>.</summary>
        static void Tail(QuadParts p, Quad q, ArtMat m, float length, float radius, float bend, int pieces = 4,
            ArtMat? tipMat = null)
        {
            var at = p.Torso + new Vector3(0f, q.H * 0.35f, -q.Len * 0.98f);
            var dir = new Vector3(0f, 0.35f, -1f).normalized;
            for (int i = 0; i < pieces; i++)
            {
                dir = (dir + new Vector3(0f, -bend, 0f)).normalized;
                var next = at + dir * (length / pieces);
                float r = radius * (1f - 0.5f * i / pieces);
                p.Body.Rod(i == pieces - 1 && tipMat.HasValue ? tipMat.Value : m, at, next, r, 6);
                at = next;
            }
        }

        // ----------------------------------------------------------------- os bichos

        static ModelDef Rat()
        {
            var q = new Quad
            {
                Hip = 0.05f, Len = 0.15f, W = 0.07f, H = 0.065f, LegR = 0.016f, HeadR = 0.05f, Snout = 0.04f,
                NeckUp = 0.0f, Coat = ArtMat.FurGray, Paw = ArtMat.Skin, Nose = ArtMat.Skin, Stride = 0.14f,
            };
            var p = Quadruped(q);
            Ears(p, q, ArtMat.Skin, 0.05f, false, 0.7f);
            Tail(p, q, ArtMat.Skin, 0.28f, 0.01f, 0.12f, 5);
            Collar(p, q);
            return p.Def;
        }

        static ModelDef Dog()
        {
            var q = new Quad
            {
                Hip = 0.2f, Len = 0.2f, W = 0.085f, H = 0.09f, LegR = 0.03f, HeadR = 0.075f, Snout = 0.07f,
                NeckUp = 0.11f, Coat = ArtMat.FurTan, Paw = ArtMat.FurTan, Nose = ArtMat.Hair, Stride = 0.3f,
            };
            var p = Quadruped(q);
            Ears(p, q, ArtMat.FurBrown, 0.07f, false, 0.72f, droop: 0.45f);   // orelha caída
            Tail(p, q, ArtMat.FurTan, 0.16f, 0.018f, -0.25f, 3);              // rabo para cima
            Collar(p, q);
            return p.Def;
        }

        static ModelDef Wolf()
        {
            var q = new Quad
            {
                Hip = 0.26f, Len = 0.26f, W = 0.095f, H = 0.105f, LegR = 0.032f, HeadR = 0.082f, Snout = 0.11f,
                NeckUp = 0.07f, Coat = ArtMat.FurGray, Paw = ArtMat.FurGray, Nose = ArtMat.Hair, Stride = 0.45f,
            };
            var p = Quadruped(q);
            Ears(p, q, ArtMat.FurGray, 0.08f, true);
            // juba de lobo no pescoço e rabo peludo caído
            p.Body.Sphere(ArtMat.FurGray, p.Torso + new Vector3(0f, q.H * 0.3f, q.Len * 0.75f),
                new Vector3(q.W * 1.15f, q.H * 1.15f, q.Len * 0.35f), 10, 8, 0.12f, 4);
            var tailRoot = p.Torso + new Vector3(0f, q.H * 0.2f, -q.Len * 1.15f);
            p.Body.Sphere(ArtMat.FurGray, tailRoot + new Vector3(0f, -0.06f, -0.08f), new Vector3(0.045f, 0.05f, 0.15f), 8, 6);
            Collar(p, q);
            return p.Def;
        }

        static ModelDef Boar()
        {
            var q = new Quad
            {
                Hip = 0.16f, Len = 0.27f, W = 0.14f, H = 0.15f, LegR = 0.035f, HeadR = 0.11f, Snout = 0.1f,
                NeckUp = -0.01f, Coat = ArtMat.FurBrown, Paw = ArtMat.Hair, Nose = ArtMat.Skin, Stride = 0.3f,
            };
            var p = Quadruped(q);
            Ears(p, q, ArtMat.FurBrown, 0.06f, true, 0.6f);
            // crista de cerdas no lombo e presas curvas para cima
            p.Body.Beam(ArtMat.Hair, p.Torso + new Vector3(0f, q.H * 0.98f, q.Len * 0.7f),
                p.Torso + new Vector3(0f, q.H * 0.9f, -q.Len * 0.5f), 0.04f);
            foreach (float sx in new[] { -1f, 1f })
                p.Body.Cylinder(ArtMat.Ivory, p.SnoutTip + new Vector3(sx * q.HeadR * 0.45f, -q.HeadR * 0.2f, -0.02f),
                    0.016f, 0f, 0.09f, 6, Quaternion.Euler(-25f, 0f, sx * -20f));
            Tail(p, q, ArtMat.FurBrown, 0.08f, 0.012f, 0.3f, 2);
            Blanket(p, q);
            return p.Def;
        }

        static ModelDef Bear()
        {
            var q = new Quad
            {
                Hip = 0.26f, Len = 0.33f, W = 0.2f, H = 0.2f, LegR = 0.07f, HeadR = 0.13f, Snout = 0.07f,
                NeckUp = 0.04f, Coat = ArtMat.FurBrown, Paw = ArtMat.Hair, Nose = ArtMat.Hair, Stride = 0.4f,
            };
            var p = Quadruped(q);
            Ears(p, q, ArtMat.FurBrown, 0.07f, false, 0.62f);
            // corcova de urso nos ombros
            p.Body.Sphere(ArtMat.FurBrown, p.Torso + new Vector3(0f, q.H * 0.55f, q.Len * 0.4f),
                new Vector3(q.W * 0.8f, q.H * 0.55f, q.Len * 0.4f), 10, 8, 0.1f, 7);
            Tail(p, q, ArtMat.FurBrown, 0.05f, 0.03f, 0.2f, 1);
            Blanket(p, q);
            return p.Def;
        }

        static ModelDef Tiger()
        {
            var q = new Quad
            {
                Hip = 0.3f, Len = 0.4f, W = 0.13f, H = 0.135f, LegR = 0.05f, HeadR = 0.12f, Snout = 0.05f,
                NeckUp = 0.05f, Coat = ArtMat.FurOrange, Paw = ArtMat.FurOrange, Nose = ArtMat.Skin, Stride = 0.6f,
            };
            var p = Quadruped(q);
            Ears(p, q, ArtMat.FurOrange, 0.05f, false, 0.62f);
            // barriga e focinho claros
            p.Body.Sphere(ArtMat.Cloth, p.Torso + new Vector3(0f, -q.H * 0.35f, 0f), new Vector3(q.W * 0.85f, q.H * 0.6f, q.Len * 0.85f), 12, 8);
            p.Body.Sphere(ArtMat.Cloth, p.SnoutTip + new Vector3(0f, -0.01f, 0f), Vector3.one * q.HeadR * 0.46f, 8, 6);
            // listras: manchas escuras finas coladas no pelo — no lombo e descendo pelos flancos
            for (int i = 0; i < 7; i++)
            {
                float z = -q.Len * 0.78f + i * q.Len * 0.26f;
                float shrink = 1f - Mathf.Abs(z) / (q.Len * 1.25f); // tronco afina nas pontas
                p.Body.Sphere(ArtMat.Hair, p.Torso + new Vector3(0f, q.H * 1.02f, z),
                    new Vector3(q.W * 0.55f, 0.012f, 0.02f), 8, 4);
                foreach (float sx in new[] { -1f, 1f })
                    p.Body.Sphere(ArtMat.Hair, p.Torso + new Vector3(sx * q.W * 1.0f, q.H * 0.25f, z + sx * 0.012f),
                        new Vector3(0.016f, q.H * 0.62f, 0.024f), 6, 6);
            }
            p.Body.Sphere(ArtMat.Hair, p.Head + new Vector3(0f, q.HeadR * 0.86f, 0.02f), new Vector3(0.05f, 0.01f, 0.012f), 6, 4);
            Tail(p, q, ArtMat.FurOrange, 0.4f, 0.022f, 0.14f, 5, ArtMat.Hair);
            Collar(p, q);
            return p.Def;
        }

        static ModelDef Rhino()
        {
            var q = new Quad
            {
                Hip = 0.26f, Len = 0.45f, W = 0.24f, H = 0.23f, LegR = 0.085f, HeadR = 0.15f, Snout = 0.14f,
                NeckUp = -0.03f, Coat = ArtMat.Hide, Paw = ArtMat.Hide, Nose = ArtMat.Hide, Stride = 0.45f,
            };
            var p = Quadruped(q);
            Ears(p, q, ArtMat.Hide, 0.08f, true, 0.55f);
            // dois chifres no focinho: o grande na ponta, o pequeno atrás
            p.Body.Cylinder(ArtMat.Ivory, p.SnoutTip + new Vector3(0f, q.HeadR * 0.35f, 0.02f), 0.05f, 0f, 0.2f, 8,
                Quaternion.Euler(-18f, 0f, 0f));
            p.Body.Cylinder(ArtMat.Ivory, p.SnoutTip + new Vector3(0f, q.HeadR * 0.4f, -0.11f), 0.035f, 0f, 0.09f, 8);
            // dobras de couro (placas) nos ombros e na anca
            p.Body.Sphere(ArtMat.Hide, p.Torso + new Vector3(0f, q.H * 0.2f, q.Len * 0.55f), new Vector3(q.W * 1.08f, q.H * 1.05f, q.Len * 0.18f), 12, 8);
            p.Body.Sphere(ArtMat.Hide, p.Torso + new Vector3(0f, q.H * 0.15f, -q.Len * 0.5f), new Vector3(q.W * 1.06f, q.H * 1.02f, q.Len * 0.2f), 12, 8);
            Tail(p, q, ArtMat.Hide, 0.12f, 0.018f, 0.35f, 2, ArtMat.Hair);
            Blanket(p, q);
            return p.Def;
        }

        static ModelDef Elephant()
        {
            var q = new Quad
            {
                Hip = 0.55f, Len = 0.5f, W = 0.32f, H = 0.34f, LegR = 0.13f, HeadR = 0.26f, Snout = 0f,
                NeckUp = 0.14f, Coat = ArtMat.Hide, Paw = ArtMat.Ivory, Nose = ArtMat.Hide, Stride = 0.7f,
            };
            var p = Quadruped(q);
            var b = p.Body;
            // orelhas enormes, abertas para os lados
            foreach (float sx in new[] { -1f, 1f })
                b.Sphere(ArtMat.Hide, p.Head + new Vector3(sx * q.HeadR * 0.95f, 0.02f, -q.HeadR * 0.25f),
                    new Vector3(0.04f, q.HeadR * 0.95f, q.HeadR * 0.75f), 10, 8);
            // tromba: desce da testa e enrola na ponta
            var at = p.Head + new Vector3(0f, -q.HeadR * 0.2f, q.HeadR * 0.8f);
            var dir = new Vector3(0f, -0.35f, 1f).normalized;
            float r = 0.075f;
            for (int i = 0; i < 6; i++)
            {
                dir = (dir + new Vector3(0f, -0.28f, i >= 4 ? 0.25f : 0f)).normalized;
                var next = at + dir * 0.12f;
                b.Rod(ArtMat.Hide, at, next, r, 8);
                at = next;
                r *= 0.86f;
            }
            // presas de marfim
            foreach (float sx in new[] { -1f, 1f })
                b.Cylinder(ArtMat.Ivory, p.Head + new Vector3(sx * q.HeadR * 0.4f, -q.HeadR * 0.45f, q.HeadR * 0.7f),
                    0.035f, 0.008f, 0.32f, 8, Quaternion.Euler(70f, sx * 8f, 0f));
            Tail(p, q, ArtMat.Hide, 0.3f, 0.02f, 0.4f, 3, ArtMat.Hair);

            // elefante de guerra: manta do time e torre de combate (howdah) no lombo
            Blanket(p, q);
            var seat = p.Torso + new Vector3(0f, q.H * 0.95f, -q.Len * 0.1f);
            b.Box(ArtMat.WoodDark, seat + new Vector3(0f, 0.04f, 0f), new Vector3(q.W * 1.2f, 0.06f, q.Len * 0.8f));
            foreach (float sx in new[] { -1f, 1f })
            {
                b.Box(ArtMat.Wood, seat + new Vector3(sx * q.W * 0.58f, 0.16f, 0f), new Vector3(0.03f, 0.2f, q.Len * 0.8f));
                foreach (float sz in new[] { -1f, 1f })
                    b.Rod(ArtMat.WoodDark, seat + new Vector3(sx * q.W * 0.58f, 0.04f, sz * q.Len * 0.38f),
                        seat + new Vector3(sx * q.W * 0.58f, 0.46f, sz * q.Len * 0.38f), 0.018f, 5);
            }
            b.Box(ArtMat.Wood, seat + new Vector3(0f, 0.16f, q.Len * 0.39f), new Vector3(q.W * 1.16f, 0.2f, 0.03f));
            b.Box(ArtMat.Wood, seat + new Vector3(0f, 0.16f, -q.Len * 0.39f), new Vector3(q.W * 1.16f, 0.2f, 0.03f));
            b.Cylinder(ArtMat.ClothTeam, seat + new Vector3(0f, 0.46f, 0f), q.W * 0.85f, 0f, 0.2f, 4, null, false, true, true, 45f);
            var mast = seat + new Vector3(0f, 0.66f, 0f);
            b.Rod(ArtMat.WoodDark, mast, mast + Up * 0.3f, 0.012f, 5);
            b.DoubleTri(ArtMat.ClothTeam, mast + Up * 0.28f, mast + Up * 0.16f, mast + new Vector3(0f, 0.22f, -0.22f));

            p.Def.Height = seat.y + 0.9f;
            return p.Def;
        }

        /// <summary>Águia: voa por cima da fronteira. Asas separadas (WingL/WingR) batem devagar.</summary>
        static ModelDef Eagle()
        {
            var d = new ModelDef { Anim = AnimKind.Glider };
            const float alt = 1.1f;
            var b = d.Part(Body, Vector3.zero).Mesh;
            var c = new Vector3(0f, alt, 0f);

            b.Sphere(ArtMat.Feather, c, new Vector3(0.075f, 0.07f, 0.16f), 12, 9);
            // cabeça branca, bico amarelo curvo
            var head = c + new Vector3(0f, 0.045f, 0.17f);
            b.Sphere(ArtMat.Ivory, head, new Vector3(0.05f, 0.05f, 0.058f), 10, 8);
            b.Cylinder(ArtMat.Beak, head + new Vector3(0f, -0.005f, 0.045f), 0.022f, 0f, 0.055f, 6, Quaternion.Euler(100f, 0f, 0f));
            foreach (float sx in new[] { -1f, 1f })
                b.Sphere(ArtMat.Hair, head + new Vector3(sx * 0.03f, 0.015f, 0.03f), Vector3.one * 0.008f, 5, 4);
            // leque da cauda e garras encolhidas com fita do time
            b.DoubleTri(ArtMat.Feather, c + new Vector3(0f, 0.005f, -0.13f), c + new Vector3(-0.09f, 0f, -0.3f), c + new Vector3(0.09f, 0f, -0.3f));
            b.DoubleTri(ArtMat.Ivory, c + new Vector3(0f, 0.004f, -0.24f), c + new Vector3(-0.08f, 0f, -0.3f), c + new Vector3(0.08f, 0f, -0.3f));
            foreach (float sx in new[] { -1f, 1f })
            {
                b.Sphere(ArtMat.Beak, c + new Vector3(sx * 0.03f, -0.07f, -0.02f), new Vector3(0.015f, 0.015f, 0.03f), 6, 4);
                b.Beam(ArtMat.ClothTeam, c + new Vector3(sx * 0.03f, -0.055f, -0.02f), c + new Vector3(sx * 0.03f, -0.07f, -0.14f), 0.02f);
            }

            // asas: pivô no ombro; penas da ponta separadas como dedos
            foreach (float sx in new[] { -1f, 1f })
            {
                var shoulder = c + new Vector3(sx * 0.06f, 0.02f, 0.03f);
                var w = d.Part(sx < 0f ? "WingL" : "WingR", shoulder, Body).Mesh;
                var elbow = shoulder + new Vector3(sx * 0.22f, 0.03f, 0.02f);
                var tip = shoulder + new Vector3(sx * 0.46f, 0.05f, -0.04f);
                w.DoubleQuad(ArtMat.Feather, shoulder + new Vector3(0f, 0f, 0.05f), elbow + new Vector3(0f, 0f, 0.04f),
                    elbow + new Vector3(0f, 0f, -0.14f), shoulder + new Vector3(0f, 0f, -0.1f));
                for (int f = 0; f < 5; f++)
                {
                    float t = f / 4f;
                    var root = Vector3.Lerp(elbow + new Vector3(0f, 0f, 0.04f), elbow + new Vector3(0f, 0f, -0.12f), t);
                    var end = tip + new Vector3(sx * -0.04f * t, 0f, -0.02f - 0.1f * t);
                    w.DoubleTri(f == 0 ? ArtMat.ClothTeam : ArtMat.Feather, root, end, root + new Vector3(0f, 0f, -0.04f));
                }
            }

            d.Height = alt + 0.12f;
            return d;
        }
    }
}
