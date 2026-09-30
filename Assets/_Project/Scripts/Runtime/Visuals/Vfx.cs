using UnityEngine;
using UnityEngine.Rendering;

namespace TDFende
{
    /// <summary>
    /// Efeitos de partícula construídos em código. Cada efeito do jogo é uma receita
    /// de camadas (clarão + fumaça + faísca...), e cada camada é UM ParticleSystem em
    /// espaço de mundo: um sistema atende infinitas explosões simultâneas — basta
    /// mover e chamar Emit.
    ///
    /// Realista: pólvora solta clarão curto e fumaça cinza que sobe e se abre; impacto
    /// levanta poeira; morte vira nuvem de poeira e lascas. A morte por atrito continua
    /// com cor própria (geada azulada) — dá pra VER qual mecânica está matando.
    /// </summary>
    public class Vfx
    {
        public static Vfx Instance { get; private set; }

        readonly ParticleSystem _flash, _bigFlash, _smoke, _darkSmoke, _dust, _debris, _sparks, _frost, _mist;
        readonly ParticleSystem _flame, _ember, _wind;

        struct Recipe
        {
            public Color Color;
            public float Life, Speed, Size, Grow, Gravity, Radius;
            public bool Additive;
            public float Softness;
        }

        public Vfx()
        {
            Instance = this;
            var root = new GameObject("VFX").transform;
            _flash = Create(root, "Clarao", new Recipe
            { Color = Palette.MuzzleFlash, Life = 0.1f, Speed = 0.4f, Size = 0.32f, Grow = 1.3f, Radius = 0.03f, Additive = true, Softness = 2.2f });
            _bigFlash = Create(root, "ClaraoGrande", new Recipe
            { Color = Palette.LeakFlash, Life = 0.25f, Speed = 0.6f, Size = 0.9f, Grow = 1.6f, Radius = 0.1f, Additive = true, Softness = 2f });
            _smoke = Create(root, "Fumaca", new Recipe
            { Color = Palette.Smoke, Life = 1.5f, Speed = 0.45f, Size = 0.26f, Grow = 3f, Gravity = -0.06f, Radius = 0.06f, Softness = 1.4f });
            _darkSmoke = Create(root, "FumacaEscura", new Recipe
            { Color = Palette.DarkSmoke, Life = 1.8f, Speed = 0.7f, Size = 0.4f, Grow = 3f, Gravity = -0.08f, Radius = 0.15f, Softness = 1.4f });
            _dust = Create(root, "Poeira", new Recipe
            { Color = Palette.Dust, Life = 0.9f, Speed = 1.3f, Size = 0.2f, Grow = 2.4f, Gravity = 0.15f, Radius = 0.12f, Softness = 1.5f });
            _debris = Create(root, "Lascas", new Recipe
            { Color = Palette.Debris, Life = 0.75f, Speed = 2.6f, Size = 0.06f, Grow = 1f, Gravity = 1.4f, Radius = 0.1f, Softness = 0.6f });
            _sparks = Create(root, "Faiscas", new Recipe
            { Color = Palette.Spark, Life = 0.28f, Speed = 3f, Size = 0.045f, Grow = 0.6f, Gravity = 0.8f, Radius = 0.04f, Additive = true, Softness = 1.2f });
            _frost = Create(root, "Geada", new Recipe
            { Color = Palette.Frost, Life = 0.7f, Speed = 1.6f, Size = 0.09f, Grow = 0.7f, Gravity = 0.3f, Radius = 0.12f, Additive = true, Softness = 1.3f });
            _flame = Create(root, "Chama", new Recipe
            { Color = Palette.Flame, Life = 0.55f, Speed = 0.5f, Size = 0.2f, Grow = 0.4f, Gravity = -0.5f, Radius = 0.06f, Additive = true, Softness = 1.8f });
            _ember = Create(root, "Fagulha", new Recipe
            { Color = Palette.Spark, Life = 0.9f, Speed = 0.8f, Size = 0.035f, Grow = 0.5f, Gravity = -0.25f, Radius = 0.08f, Additive = true, Softness = 1f });
            _wind = Create(root, "Vento", new Recipe
            { Color = Palette.Wind, Life = 0.6f, Speed = 2.2f, Size = 0.3f, Grow = 2.5f, Gravity = 0f, Radius = 0.2f, Softness = 2f });
            _mist = Create(root, "Nevoa", new Recipe
            { Color = Palette.FrostMist, Life = 1.1f, Speed = 0.5f, Size = 0.3f, Grow = 2.2f, Gravity = -0.03f, Radius = 0.12f, Softness = 1.6f });
        }

        public void KillBurst(Vector3 pos, bool byAttrition)
        {
            if (byAttrition)
            {
                Emit(_frost, pos, 16);
                Emit(_mist, pos, 6);
                return;
            }
            Emit(_dust, pos, 10);
            Emit(_debris, pos, 9);
        }

        public void Impact(Vector3 pos)
        {
            Emit(_dust, pos, 4);
            Emit(_sparks, pos, 4);
        }

        public void Muzzle(Vector3 pos)
        {
            Emit(_flash, pos, 2);
            Emit(_smoke, pos, 3);
            Emit(_sparks, pos, 2);
        }

        /// <summary>Tiro de uma torre específica: cada tipo tem clarão próprio.</summary>
        public void Muzzle(Vector3 pos, int towerType)
        {
            switch (towerType)
            {
                case 1: // morteiro: tranco grande, muita fumaça
                    Emit(_bigFlash, pos, 2);
                    Emit(_smoke, pos, 8);
                    Emit(_sparks, pos, 5);
                    break;
                case 2: // gelo: sopro de névoa
                    Emit(_mist, pos, 3);
                    Emit(_frost, pos, 5);
                    break;
                case 3: // balista: estalo seco de madeira, sem pólvora
                    Emit(_dust, pos, 2);
                    break;
                case 4: // fogo grego: jato de chama
                    Emit(_flame, pos, 10);
                    Emit(_ember, pos, 6);
                    Emit(_darkSmoke, pos, 2);
                    break;
                case 5: // ar: lufada
                    Emit(_wind, pos, 6);
                    break;
                default:
                    Emit(_flash, pos, 3);
                    Emit(_smoke, pos, 5);
                    Emit(_sparks, pos, 4);
                    break;
            }
        }

        /// <summary>Chegada do tiro: onde o jogador VÊ que acertou.</summary>
        public void Impact(Vector3 pos, int towerType)
        {
            switch (towerType)
            {
                case 1: // bomba de morteiro: explosão
                    Emit(_bigFlash, pos, 3);
                    Emit(_darkSmoke, pos, 8);
                    Emit(_debris, pos, 14);
                    Emit(_dust, pos, 10);
                    break;
                case 2:
                    Emit(_frost, pos, 10);
                    Emit(_mist, pos, 3);
                    break;
                case 4:
                    Emit(_flame, pos, 14);
                    Emit(_ember, pos, 8);
                    break;
                case 5:
                    Emit(_wind, pos, 8);
                    Emit(_dust, pos, 6);
                    break;
                default:
                    Impact(pos);
                    break;
            }
        }

        /// <summary>Inimigo em chamas: chamar poucas vezes por segundo por inimigo.</summary>
        public void Burn(Vector3 pos)
        {
            Emit(_flame, pos, 2);
            Emit(_ember, pos, 1);
        }

        /// <summary>Poeira de casco e de roda: chão de terra batida sob peso.</summary>
        public void Footstep(Vector3 pos) => Emit(_dust, pos, 1);

        /// <summary>Gelado (lento): cristalzinho de geada soltando do corpo.</summary>
        public void Chill(Vector3 pos) => Emit(_frost, pos, 1);

        /// <summary>Congelou: estalo de geada e névoa fria em volta.</summary>
        public void Freeze(Vector3 pos)
        {
            Emit(_frost, pos, 18);
            Emit(_mist, pos, 5);
        }

        /// <summary>Descongelou: a casca estoura em lascas de gelo.</summary>
        public void Thaw(Vector3 pos)
        {
            Emit(_frost, pos, 12);
            Emit(_mist, pos, 2);
        }

        /// <summary>Chama viva do braseiro da torre de Fogo.</summary>
        public void Brazier(Vector3 pos) => Emit(_flame, pos, 1);

        /// <summary>
        /// Rastro do tiro no ar. É o que faz o tiro APARECER de longe: a bala em si tem
        /// poucos pixels no zoom do jogo, o rastro tem dezenas.
        /// </summary>
        public static TrailRenderer AddTrail(GameObject go, int towerType)
        {
            Color c;
            float width, time;
            bool additive;
            switch (towerType)
            {
                case 1: c = Palette.Smoke; width = 0.13f; time = 0.5f; additive = false; break;
                case 2: c = Palette.Frost; width = 0.07f; time = 0.25f; additive = true; break;
                case 3: c = Color.white; width = 0.03f; time = 0.16f; additive = true; break;
                case 4: c = Palette.Flame; width = 0.16f; time = 0.3f; additive = true; break;
                case 5: c = Palette.Wind; width = 0.3f; time = 0.35f; additive = false; break;
                default: c = Palette.Smoke; width = 0.08f; time = 0.3f; additive = false; break;
            }
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = time;
            tr.minVertexDistance = 0.04f;
            tr.widthCurve = AnimationCurve.Linear(0f, width, 1f, width * 0.2f);
            var g = new Gradient();
            var c0 = c;
            var c1 = c;
            c1.a = 0f;
            g.SetKeys(new[] { new GradientColorKey(c0, 0f), new GradientColorKey(c1, 1f) },
                new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = g;
            tr.numCapVertices = 2;
            tr.shadowCastingMode = ShadowCastingMode.Off;
            tr.receiveShadows = false;
            var m = new Material(Mat(additive));
            m.SetFloat("_Softness", 0.5f);
            tr.sharedMaterial = m;
            return tr;
        }

        public void Build(Vector3 pos)
        {
            Emit(_dust, pos + Vector3.up * 0.05f, 22);
            Emit(_debris, pos + Vector3.up * 0.1f, 6);
        }

        public void Leak(Vector3 pos)
        {
            Emit(_bigFlash, pos + Vector3.up * 0.4f, 3);
            Emit(_darkSmoke, pos + Vector3.up * 0.3f, 12);
            Emit(_debris, pos + Vector3.up * 0.3f, 10);
        }

        static void Emit(ParticleSystem ps, Vector3 pos, int count)
        {
            if (ps == null) return; // sobrevive a uma troca de cena sem estourar
            ps.transform.position = pos;
            ps.Emit(count);
        }

        static Material _alpha, _additive;

        static Material Mat(bool additive)
        {
            ref var slot = ref additive ? ref _additive : ref _alpha;
            if (slot != null) return slot;
            var shader = Shader.Find("TDFende/SoftParticle") ?? Shader.Find("TDFende/TerritoryOverlay");
            slot = shader != null ? new Material(shader) : MaterialFactory.Get(Color.white);
            slot.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            slot.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            return slot;
        }

        static ParticleSystem Create(Transform parent, string name, Recipe r)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();

            var main = ps.main;
            main.duration = 1f;
            // loop = true de propósito, mesmo sem emissão automática: um sistema NÃO-cíclico
            // atinge o estado Stopped ao fim da duração (sem partículas, isso é 1s depois do
            // boot), e Emit() num sistema parado não simula nada. Como o primeiro efeito do
            // jogo só acontece muitos segundos depois, praticamente TODAS as partículas
            // caíam num sistema morto. Cíclico + emission desligada = fica vivo e ocioso.
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(r.Life * 0.6f, r.Life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(r.Speed * 0.4f, r.Speed);
            main.startSize = new ParticleSystem.MinMaxCurve(r.Size * 0.6f, r.Size);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(r.Color * 0.85f, r.Color);
            main.gravityModifier = r.Gravity;
            main.maxParticles = 800;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = false; // só emissão manual via Emit()

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = r.Radius;

            // some suave em vez de piscar fora
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            // fumaça e poeira se abrem; faísca e lasca encolhem
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            float g = Mathf.Max(0.05f, r.Grow);
            sol.size = g >= 1f
                ? new ParticleSystem.MinMaxCurve(g, AnimationCurve.EaseInOut(0f, 1f / g, 1f, 1f))
                : new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, g));

            // fumaça desacelera no ar em vez de voar reto para sempre
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.08f;
            limit.limit = r.Speed;

            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            pr.shadowCastingMode = ShadowCastingMode.Off;
            pr.receiveShadows = false;
            // o shader usa a cor de vértice crua; em espaço Linear quem converte a cor da
            // partícula é o renderer, e só com isto ligado (não há garantia do padrão em
            // renderer criado por código)
            pr.applyActiveColorSpace = true;
            pr.sharedMaterial = new Material(Mat(r.Additive));
            pr.sharedMaterial.SetFloat("_Softness", r.Softness);
            pr.sortingFudge = r.Additive ? -1f : 0f;

            ps.Play(); // fica ligado e ocioso, esperando Emit
            return ps;
        }
    }
}
