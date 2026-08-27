using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Efeitos de partícula construídos em código, um ParticleSystem por tipo.
    /// Como os sistemas simulam em espaço de mundo, um único sistema atende
    /// infinitas explosões simultâneas — basta mover e chamar Emit.
    /// </summary>
    public class Vfx
    {
        public static Vfx Instance { get; private set; }

        readonly ParticleSystem _killBurst;      // morte por tiro
        readonly ParticleSystem _attritionBurst; // morte por atrito (cor diferente de propósito:
                                                 // dá pra VER qual mecânica está matando)
        readonly ParticleSystem _impact;
        readonly ParticleSystem _muzzle;
        readonly ParticleSystem _build;
        readonly ParticleSystem _leak;

        public Vfx()
        {
            Instance = this;
            var root = new GameObject("VFX").transform;
            _killBurst = Create(root, "KillBurst", Palette.EnemyFull, 0.55f, 4.5f, 0.20f);
            _attritionBurst = Create(root, "AttritionBurst", Palette.TerritoryEdge, 0.70f, 3.0f, 0.16f);
            _impact = Create(root, "Impact", Palette.Projectile, 0.25f, 5.0f, 0.11f);
            _muzzle = Create(root, "Muzzle", Palette.Projectile, 0.12f, 2.5f, 0.14f);
            _build = Create(root, "Build", Palette.TowerHead, 0.60f, 2.2f, 0.18f);
            _leak = Create(root, "Leak", Palette.TextDanger, 0.80f, 6.0f, 0.26f);
        }

        public void KillBurst(Vector3 pos, bool byAttrition) =>
            Emit(byAttrition ? _attritionBurst : _killBurst, pos, byAttrition ? 14 : 18);

        public void Impact(Vector3 pos) => Emit(_impact, pos, 6);
        public void Muzzle(Vector3 pos) => Emit(_muzzle, pos, 3);
        public void Build(Vector3 pos) => Emit(_build, pos, 20);
        public void Leak(Vector3 pos) => Emit(_leak, pos, 28);

        static void Emit(ParticleSystem ps, Vector3 pos, int count)
        {
            if (ps == null) return; // sobrevive a uma troca de cena sem estourar
            ps.transform.position = pos;
            ps.Emit(count);
        }

        static ParticleSystem Create(Transform parent, string name, Color color,
            float lifetime, float speed, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();

            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = color;
            main.gravityModifier = 0.35f;
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = false; // só emissão manual via Emit()

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.18f;

            // some suave em vez de piscar fora
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.45f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.25f));

            // mesmo shader do overlay de fronteira: unlit + cor por vértice,
            // funciona em URP e Built-in sem variante nenhuma
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pr.receiveShadows = false;
            var shader = Shader.Find("TDFende/TerritoryOverlay");
            pr.sharedMaterial = shader != null
                ? new Material(shader)
                : MaterialFactory.Get(color);

            ps.Play(); // fica ligado e ocioso, esperando Emit
            return ps;
        }
    }
}
