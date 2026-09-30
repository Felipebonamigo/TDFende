using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// A torre MUDA a cada nível — não só cresce. Cada nível de 2 a 6 acende um conjunto de
    /// peças (escondidas até lá): primeiro reforço de bronze, depois contrafortes, coroa de
    /// pontas, runas acesas e, no máximo, o remate com aura no chão. Por cima disso, cada
    /// tipo ganha o que é dele: cristais e pingentes no Gelo, braseiros e lava no Fogo,
    /// canhão duplo no Canhão, pás extras no Ar.
    ///
    /// As peças se chamam "Lv{n}_{pai}": o ModelRig mostra as de nível ≤ o atual.
    /// </summary>
    public static partial class ModelLib
    {
        /// <summary>Medidas de cada torre que os enfeites precisam (batem com os construtores).</summary>
        struct TierGeom
        {
            public float BaseSize;   // lado do plinto
            public float ShaftR;     // raio do fuste (onde vão as runas)
            public float ShaftMidY;  // altura das runas no fuste
            public float RimR;       // raio da borda do topo
            public float RimY;       // altura da borda do topo (coroa de pontas)
            public float ApexY;      // ponto mais alto (remate)
            public ArtMat RuneMat;   // do que é feita a runa acesa
            public bool Square;      // Sentinela: plataforma quadrada
        }

        static TierGeom GeomFor(int type) => type switch
        {
            1 => new TierGeom { BaseSize = 0.96f, ShaftR = 0.45f, ShaftMidY = 0.23f, RimR = 0.42f, RimY = 0.464f, ApexY = 0.62f, RuneMat = ArtMat.Rune },
            2 => new TierGeom { BaseSize = 0.8f, ShaftR = 0.28f, ShaftMidY = 0.45f, RimR = 0.33f, RimY = 1.13f, ApexY = 1.47f, RuneMat = ArtMat.Ice },
            3 => new TierGeom { BaseSize = 0.8f, ShaftR = 0.34f, ShaftMidY = 0.24f, RimR = 0.28f, RimY = 1.1f, ApexY = 1.54f, RuneMat = ArtMat.Rune, Square = true },
            4 => new TierGeom { BaseSize = 0.9f, ShaftR = 0.375f, ShaftMidY = 0.35f, RimR = 0.37f, RimY = 0.674f, ApexY = 0.8f, RuneMat = ArtMat.Ember },
            5 => new TierGeom { BaseSize = 0.82f, ShaftR = 0.265f, ShaftMidY = 0.45f, RimR = 0.28f, RimY = 0.764f, ApexY = 1.12f, RuneMat = ArtMat.Rune },
            _ => new TierGeom { BaseSize = 0.9f, ShaftR = 0.34f, ShaftMidY = 0.45f, RimR = 0.36f, RimY = 0.814f, ApexY = 0.95f, RuneMat = ArtMat.Rune },
        };

        /// <summary>Malha de um enfeite do nível <paramref name="level"/>, pendurada em <paramref name="parent"/>.</summary>
        static MeshBuilder Tier(ModelDef d, int level, string parent)
        {
            string name = $"Lv{level}_{parent}";
            var existing = d.Find(name);
            if (existing != null) return existing.Mesh;
            var pivot = d.Find(parent)?.Pivot ?? Vector3.zero;
            var part = d.Part(name, pivot, parent);
            part.StartHidden = true;
            return part.Mesh;
        }

        static ModelDef WithTiers(ModelDef d, int type)
        {
            var g = GeomFor(type);
            float topY = d.Find(Top)?.Pivot.y ?? 0.5f;

            // ---- nível 2: cinta de bronze na borda do topo ----
            var l2 = Tier(d, 2, Top);
            if (g.Square)
            {
                foreach (var (x, z) in new[] { (1f, 1f), (-1f, 1f), (-1f, -1f), (1f, -1f) })
                    l2.Box(ArtMat.Bronze, new Vector3(x * 0.27f, topY + 0.02f, z * 0.27f), new Vector3(0.07f, 0.05f, 0.07f));
            }
            else
                l2.Cylinder(ArtMat.Bronze, new Vector3(0f, topY - 0.012f, 0f), g.RimR + 0.018f, g.RimR + 0.018f, 0.024f, 24);

            // ---- nível 3: contrafortes de pedra nos quatro cantos da base ----
            var l3b = Tier(d, 3, "Base");
            for (int i = 0; i < 4; i++)
            {
                float a = Mathf.PI * 0.25f + i * Mathf.PI * 0.5f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var at = dir * (g.BaseSize * 0.48f) + Up * 0.2f;
                var rot = Quaternion.LookRotation(-dir) * Quaternion.Euler(-14f, 0f, 0f);
                l3b.Box(ArtMat.StoneDark, at, new Vector3(0.1f, 0.26f, 0.12f), rot);
                l3b.Box(ArtMat.Stone, at + Up * 0.14f - dir * 0.02f, new Vector3(0.12f, 0.03f, 0.1f), rot);
            }

            // ---- nível 4: coroa de pontas de bronze ----
            var l4 = Tier(d, 4, Top);
            int spikes = g.Square ? 4 : 10;
            for (int i = 0; i < spikes; i++)
            {
                float a = (g.Square ? Mathf.PI * 0.25f : 0f) + i * Mathf.PI * 2f / spikes;
                float r = g.Square ? 0.38f : g.RimR + 0.01f;
                var p = new Vector3(Mathf.Cos(a) * r, g.RimY, Mathf.Sin(a) * r);
                l4.Cylinder(ArtMat.Bronze, p, 0.018f, 0f, 0.09f, 6);
            }

            // ---- nível 5: runas acesas no fuste ----
            var l5 = Tier(d, 5, Shaft);
            float shaftBase = d.Find(Shaft)?.Pivot.y ?? 0.1f;
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                l5.Box(g.RuneMat, dir * (g.ShaftR + 0.006f) + Up * (shaftBase + g.ShaftMidY),
                    new Vector3(0.06f, 0.1f, 0.012f), Quaternion.LookRotation(dir));
            }

            // ---- nível 6: remate no alto e aura acesa no chão ----
            var l6 = Tier(d, 6, Top);
            var apex = new Vector3(0f, g.ApexY, 0f);
            l6.Rod(ArtMat.Bronze, apex, apex + Up * 0.16f, 0.012f, 6);
            l6.Sphere(g.RuneMat, apex + Up * 0.19f, Vector3.one * 0.035f, 10, 7);
            var l6b = Tier(d, 6, "Base");
            // anel fino (não um disco): 36 lascas em volta do plinto
            float auraR = g.BaseSize * 0.58f;
            Ring(l6b, g.RuneMat, new Vector3(0f, 0.094f, 0f), auraR, 36,
                new Vector3(2f * Mathf.PI * auraR / 36f * 1.05f, 0.014f, 0.03f));

            // ---- o que é de cada tipo ----
            switch (type)
            {
                case 0: CannonTiers(d, topY); break;
                case 1: MortarTiers(d, topY); break;
                case 2: FrostTiers(d, g); break;
                case 3: WatchTiers(d, topY); break;
                case 4: FireTiers(d, g, topY); break;
                case 5: WindTiers(d); break;
            }
            return d;
        }

        static void CannonTiers(ModelDef d, float ty)
        {
            // nv 3: pirâmide de balas e barril de pólvora no adarve
            var l3 = Tier(d, 3, Top);
            var pile = new Vector3(0.2f, ty + 0.1f, -0.18f);
            foreach (var o in new[] { new Vector3(-0.035f, 0f, 0f), new Vector3(0.035f, 0f, 0f), new Vector3(0f, 0f, 0.05f), new Vector3(0f, 0.05f, 0.017f) })
                l3.Sphere(ArtMat.Iron, pile + o, Vector3.one * 0.03f, 8, 6);
            var keg = new Vector3(-0.22f, ty + 0.06f, 0.16f);
            l3.Lathe(ArtMat.Wood, keg, new[] { 0.04f, 0.05f, 0.04f }, new[] { 0f, 0.05f, 0.1f }, 10);

            // nv 4: canhão gêmeo ao lado do principal (gira junto com a torreta)
            var tu = d.Find(Turret);
            if (tu != null)
            {
                var l4 = Tier(d, 4, Turret);
                var trunnion = tu.Pivot + new Vector3(0.12f, 0.11f, 0f);
                var breech = trunnion + new Vector3(0f, 0f, -0.1f);
                l4.Lathe(ArtMat.Bronze, breech, new[] { 0f, 0.04f, 0.045f, 0.036f, 0.034f, 0.04f, 0.022f },
                    new[] { 0f, 0.004f, 0.03f, 0.12f, 0.24f, 0.27f, 0.27f }, 12, Along(Vector3.forward));
                l4.Box(ArtMat.WoodDark, trunnion + new Vector3(0f, -0.05f, 0.02f), new Vector3(0.06f, 0.05f, 0.22f));
            }
            // nv 5: escudos do time pendurados nas ameias
            var l5 = Tier(d, 5, Top);
            for (int i = 0; i < 4; i++)
            {
                float a = Mathf.PI * 0.25f + i * Mathf.PI * 0.5f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                l5.Cylinder(ArtMat.ClothTeam, dir * 0.4f + Up * (ty + 0.1f), 0.055f, 0.055f, 0.012f, 12, Along(dir));
                l5.Cylinder(ArtMat.Bronze, dir * 0.412f + Up * (ty + 0.1f), 0.015f, 0.015f, 0.008f, 8, Along(dir));
            }
        }

        static void MortarTiers(ModelDef d, float ty)
        {
            // nv 3: mais bombas e barris
            var l3 = Tier(d, 3, Top);
            var pile = new Vector3(0.24f, ty + 0.05f, -0.2f);
            foreach (var o in new[] { new Vector3(-0.04f, 0f, 0f), new Vector3(0.04f, 0f, 0f), new Vector3(0f, 0f, 0.06f), new Vector3(0f, 0.055f, 0.02f) })
                l3.Sphere(ArtMat.Iron, pile + o, Vector3.one * 0.038f, 8, 6);
            // nv 4: cintas de ferro no bastião
            var l4 = Tier(d, 4, Shaft);
            float oct = Mathf.PI / 8f;
            l4.Cylinder(ArtMat.Iron, new Vector3(0f, 0.16f, 0f), 0.475f, 0.465f, 0.03f, 8, flat: true, angleOffset: oct);
            // nv 6: segundo morteiro no canto, apontado ao céu
            var l6 = Tier(d, 6, Top);
            var foot = new Vector3(-0.22f, ty + 0.02f, -0.2f);
            var tilt = Quaternion.Euler(30f, 40f, 0f);
            l6.Box(ArtMat.WoodDark, foot + Up * 0.03f, new Vector3(0.14f, 0.06f, 0.16f));
            l6.Lathe(ArtMat.Bronze, foot + Up * 0.05f, new[] { 0f, 0.06f, 0.07f, 0.06f, 0.07f, 0.045f },
                new[] { 0f, 0.004f, 0.04f, 0.14f, 0.17f, 0.17f }, 12, tilt);
        }

        static void FrostTiers(ModelDef d, TierGeom g)
        {
            // nv 3: cristais brotando nos cantos da base
            var l3 = Tier(d, 3, "Base");
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var at = dir * (g.BaseSize * 0.42f) + Up * 0.1f;
                Crystal(l3, at, Quaternion.FromToRotation(Up, (Up * 2f + dir).normalized), 0.04f, 0.2f);
                Crystal(l3, at + new Vector3(dir.z, 0f, -dir.x) * 0.06f, Quaternion.FromToRotation(Up, (Up * 3f + dir).normalized), 0.028f, 0.12f);
            }
            // nv 4: pingentes de gelo pendurados no beiral do telhado
            var l4 = Tier(d, 4, Top);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                var p = new Vector3(Mathf.Cos(a) * 0.33f, g.RimY + 0.02f, Mathf.Sin(a) * 0.33f);
                float len = 0.07f + 0.05f * ((i * 7) % 3) / 2f;
                l4.Cylinder(ArtMat.Ice, p, 0.016f, 0f, len, 6, Quaternion.Euler(180f, 0f, 0f));
            }
            // nv 5: crosta de gelo subindo pelo fuste
            var l5 = Tier(d, 5, Shaft);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f + 0.3f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Crystal(l5, dir * 0.27f + Up * (0.14f + 0.05f * (i % 3)), Quaternion.FromToRotation(Up, (Up * 2f + dir).normalized),
                    0.03f, 0.14f);
            }
            // nv 6: cristal gigante coroando o telhado
            var l6 = Tier(d, 6, Top);
            Crystal(l6, new Vector3(0f, g.ApexY - 0.04f, 0f), Quaternion.identity, 0.06f, 0.34f);
            Crystal(l6, new Vector3(0.04f, g.ApexY - 0.06f, 0f), Quaternion.Euler(0f, 0f, -25f), 0.035f, 0.18f);
            Crystal(l6, new Vector3(-0.04f, g.ApexY - 0.06f, 0.01f), Quaternion.Euler(10f, 0f, 25f), 0.035f, 0.17f);
        }

        static void WatchTiers(ModelDef d, float ty)
        {
            // nv 3: escudos do time pendurados no parapeito
            var l3 = Tier(d, 3, Top);
            foreach (var (x, z) in new[] { (0f, 1f), (1f, 0f), (0f, -1f), (-1f, 0f) })
            {
                var dir = new Vector3(x, 0f, z);
                l3.Cylinder(ArtMat.ClothTeam, dir * 0.29f + Up * (ty + 0.12f), 0.06f, 0.06f, 0.012f, 12, Along(dir));
                l3.Cylinder(ArtMat.Iron, dir * 0.302f + Up * (ty + 0.12f), 0.016f, 0.016f, 0.008f, 8, Along(dir));
            }
            // nv 5: lanternas acesas nos cantos do telhado
            var l5 = Tier(d, 5, Top);
            foreach (var (x, z) in new[] { (1f, 1f), (-1f, 1f), (-1f, -1f), (1f, -1f) })
            {
                var p = new Vector3(x * 0.3f, ty + 0.28f, z * 0.3f);
                l5.Rod(ArtMat.Iron, p + Up * 0.05f, p + Up * 0.09f, 0.004f, 4);
                l5.Box(ArtMat.Rune, p, new Vector3(0.04f, 0.06f, 0.04f));
            }
        }

        static void FireTiers(ModelDef d, TierGeom g, float ty)
        {
            // nv 3: dois braseiros a mais no parapeito, acesos
            var l3 = Tier(d, 3, Top);
            foreach (var p in new[] { new Vector3(-0.2f, ty + 0.13f, 0.16f), new Vector3(0.18f, ty + 0.13f, -0.2f) })
            {
                l3.Lathe(ArtMat.Iron, p, new[] { 0.02f, 0.06f, 0.07f }, new[] { 0f, 0.03f, 0.05f }, 10);
                l3.Sphere(ArtMat.Ember, p + Up * 0.045f, new Vector3(0.055f, 0.02f, 0.055f), 8, 4, 0.25f, 5);
                d.FirePoints.Add((p + Up * 0.07f, 3, Top));
            }
            // nv 4: rachaduras de lava no fuste
            var l4 = Tier(d, 4, Shaft);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.9f + 0.4f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float y = 0.16f + 0.045f * (i % 5);
                l4.Box(ArtMat.Ember, dir * 0.377f + Up * y, new Vector3(0.012f, 0.09f, 0.006f),
                    Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 25f : -30f)));
            }
            // nv 5: braseiros nos cantos da base
            var l5 = Tier(d, 5, "Base");
            for (int i = 0; i < 4; i++)
            {
                float a = Mathf.PI * 0.25f + i * Mathf.PI * 0.5f;
                var p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (g.BaseSize * 0.58f) + Up * 0.1f;
                l5.Rod(ArtMat.Iron, p, p + Up * 0.12f, 0.012f, 5);
                l5.Lathe(ArtMat.Iron, p + Up * 0.12f, new[] { 0.015f, 0.045f, 0.05f }, new[] { 0f, 0.025f, 0.04f }, 10);
                l5.Sphere(ArtMat.Ember, p + Up * 0.155f, new Vector3(0.04f, 0.015f, 0.04f), 8, 4, 0.25f, 6);
                d.FirePoints.Add((p + Up * 0.18f, 5, "Base"));
            }
            // nv 6: coroa de brasa na borda do topo, chama em volta toda
            var l6 = Tier(d, 6, Top);
            Ring(l6, ArtMat.Ember, new Vector3(0f, g.RimY - 0.005f, 0f), g.RimR + 0.02f, 28,
                new Vector3(2f * Mathf.PI * (g.RimR + 0.02f) / 28f * 1.05f, 0.02f, 0.035f));
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                d.FirePoints.Add((new Vector3(Mathf.Cos(a) * g.RimR, g.RimY + 0.05f, Mathf.Sin(a) * g.RimR), 6, Top));
            }
        }

        static void WindTiers(ModelDef d)
        {
            // nv 3: cata-vento em cima da cabeça do moinho
            var tu = d.Find(Turret);
            if (tu != null)
            {
                var l3 = Tier(d, 3, Turret);
                var p = tu.Pivot + new Vector3(0f, 0.32f, -0.12f);
                l3.Rod(ArtMat.Iron, p, p + Up * 0.14f, 0.006f, 5);
                l3.DoubleTri(ArtMat.Bronze, p + Up * 0.12f, p + new Vector3(0f, 0.12f, -0.12f), p + new Vector3(0f, 0.07f, -0.1f));
                l3.Cylinder(ArtMat.Bronze, p + new Vector3(0f, 0.12f, 0f), 0.012f, 0f, 0.05f, 6, Along(Vector3.forward));
            }
            // nv 4: mais quatro pás entre as originais (giram junto com o rotor)
            var rotor = d.Find(Rotor);
            if (rotor != null)
            {
                var l4 = Tier(d, 4, Rotor);
                var rc = rotor.Pivot;
                for (int i = 0; i < 4; i++)
                {
                    var rot = Quaternion.AngleAxis(i * 90f + 65f, Vector3.forward);
                    var tip = rc + rot * new Vector3(0f, 0.36f, 0f) + Vector3.forward * 0.015f;
                    l4.Beam(ArtMat.WoodDark, rc, tip, 0.014f);
                    var side = rot * new Vector3(0.07f, 0f, 0f);
                    l4.DoubleQuad(ArtMat.ClothTeam, rc + rot * new Vector3(0f, 0.1f, 0f) + Vector3.forward * 0.015f, tip,
                        tip + side, rc + rot * new Vector3(0f, 0.1f, 0f) + side * 0.8f + Vector3.forward * 0.015f);
                }
                // nv 6: cubo de bronze no eixo
                var l6 = Tier(d, 6, Rotor);
                l6.Sphere(ArtMat.Rune, rc + Vector3.forward * 0.02f, Vector3.one * 0.05f, 10, 7);
            }
        }

        /// <summary>
        /// Casca de gelo que cobre o bicho congelado: cristais em volta das patas e sobre o
        /// lombo, para um bicho de altura 1. A vista escala pela altura do bicho.
        /// </summary>
        public static ModelDef IceCrust() => Cached("Gelo_Casca", () =>
        {
            var d = new ModelDef();
            var b = d.Part(Body, Vector3.zero).Mesh;
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f + 0.2f;
                var dir = new Vector3(Mathf.Cos(a) * 0.55f, 0f, Mathf.Sin(a) * 0.9f);
                float h = 0.35f + 0.25f * ((i * 5) % 3) / 2f;
                Crystal(b, dir, Quaternion.FromToRotation(Up, (Up * 2.2f + dir.normalized).normalized), 0.1f, h);
            }
            for (int i = 0; i < 4; i++)
            {
                var at = new Vector3((i % 2 == 0 ? -0.12f : 0.14f), 0.72f, -0.35f + i * 0.24f);
                Crystal(b, at, Quaternion.Euler(i * 11f - 15f, i * 40f, (i % 2 == 0 ? 20f : -20f)), 0.08f, 0.32f);
            }
            d.Height = 1f;
            return d;
        });
    }
}
