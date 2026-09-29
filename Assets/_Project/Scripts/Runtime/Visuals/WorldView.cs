using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Monta o mundo em volta do tabuleiro a partir do WorldLayout: relevo, rio,
    /// mureta das lanes e mata. Tudo estático e combinado em poucas malhas — o
    /// cenário inteiro custa um punhado de draw calls, não um por árvore.
    /// </summary>
    public static class WorldView
    {
        /// <param name="terrain">
        /// Terreno de verdade do GroundBuilder (grama fotográfica + tufos 3D). Se existir, ele
        /// é o chão: aqui entram só mureta e mata, assentadas na altura dele, e o rio sai
        /// (o terreno é plano entre as lanes). Nulo = relevo, rio e chão procedurais.
        /// </param>
        public static Transform Build(WorldLayout layout, Terrain terrain = null)
        {
            var root = new GameObject("Mundo").transform;
            var neutral = Color.white;

            if (terrain != null)
            {
                layout.River = false;
                float baseY = terrain.transform.position.y;
                layout.GroundHeight = (x, z) => terrain.SampleHeight(new Vector3(x, 0f, z)) + baseY;
            }
            else
            {
                var ground = ArtFactory.Static("Terreno", layout.BuildTerrain(), root, neutral, castShadows: false);
                ground.GetComponent<MeshRenderer>().receiveShadows = true;
            }

            if (layout.River)
            {
                var water = ArtFactory.Static("Rio", layout.BuildWater(), root, neutral, castShadows: false);
                water.AddComponent<WaterFlow>();
            }

            ArtFactory.Static("Mureta", layout.BuildCurbs(), root, neutral);
            // cenário: modelo baixado (Poly Haven) onde existe, procedural no resto
            var models = new GameObject("CenarioModelos").transform;
            models.SetParent(root, false);
            var procedural = new System.Collections.Generic.List<WorldLayout.Prop>();
            // árvore de verdade pesa (dezenas de milhares de triângulos cada): só as mais
            // perto do tabuleiro; as do fundo, que a névoa já apaga, seguem procedurais
            const int MaxModelTrees = 60;
            const float ModelTreeReach = 22f;
            int modelTrees = 0;
            var wind = models.gameObject.AddComponent<TreeWind>();
            foreach (var p in layout.ScatterScenery())
            {
                if (InBackdrop(layout, p.Pos)) continue;
                GameObject placed = null;
                bool tree = p.Kind == WorldLayout.PropKind.Pine || p.Kind == WorldLayout.PropKind.Oak;
                if (tree && modelTrees < MaxModelTrees && layout.DistanceToPlay(p.Pos.x, p.Pos.z) < ModelTreeReach)
                {
                    // altura da árvore procedural equivalente: ~2x o "tamanho" do pinheiro
                    placed = SceneryModels.Place("Arvores", models, p.Pos, p.Size * 1.9f, p.Seed, byHeight: true);
                    if (placed != null)
                    {
                        modelTrees++;
                        wind.Add(placed.transform);
                    }
                }
                if (p.Kind == WorldLayout.PropKind.Boulder)
                    placed = SceneryModels.Place("Pedras", models, p.Pos, p.Size * 2.2f, p.Seed);
                else if (p.Kind == WorldLayout.PropKind.Stump)
                    placed = (p.Seed & 1) == 0
                        ? SceneryModels.Place("Troncos", models, p.Pos, p.Size * 3.2f, p.Seed)
                        : SceneryModels.Place("Tocos", models, p.Pos, p.Size * 0.9f, p.Seed, byHeight: true);
                if (placed == null) procedural.Add(p);
            }
            ArtFactory.Static("Cenario", WorldLayout.BuildScenery(procedural), root, neutral);

            // depósito de suprimentos junto de cada fortaleza, do lado de fora da mureta:
            // barril e caixa de verdade dão escala humana à cena
            foreach (var area in layout.PlayAreas)
            {
                float x = area.Max.x + 0.75f, z = (area.Min.y + area.Max.y) * 0.5f;
                float Y(float px, float pz) => layout.GroundHeight != null ? layout.GroundHeight(px, pz) : 0f;
                var spots = new[]
                {
                    ("Barris", new Vector3(x, 0f, z - 1.1f), 0.28f, true),
                    ("Barris", new Vector3(x + 0.35f, 0f, z - 0.8f), 0.26f, true),
                    ("Caixas", new Vector3(x + 0.1f, 0f, z + 1.0f), 0.34f, false),
                    ("Caixas", new Vector3(x + 0.5f, 0f, z + 1.35f), 0.3f, false),
                    ("Troncos", new Vector3(x + 0.6f, 0f, z + 0.1f), 0.9f, false),
                };
                int n = 0;
                foreach (var (cat, spot, size, byH) in spots)
                {
                    var at = new Vector3(spot.x, Y(spot.x, spot.z), spot.z);
                    SceneryModels.Place(cat, models, at, size, 900 + n++ * 13, byH);
                }
            }

            BuildBackdrop(layout, root);
            return root;
        }

        // muralha atrás da fortaleza (Max.x) e acampamento de onde o inimigo sai (Min.x)
        const float WallGap = 2.1f, CampGap = 1.2f;

        static bool InBackdrop(WorldLayout layout, Vector3 p)
        {
            foreach (var a in layout.PlayAreas)
            {
                float zc = (a.Min.y + a.Max.y) * 0.5f;
                if (p.x > a.Max.x + WallGap - 1.0f && p.x < a.Max.x + WallGap + 1.1f
                    && p.z > a.Min.y - 1.0f && p.z < a.Max.y + 1.0f) return true;
                if (p.x > a.Min.x - CampGap - 3.4f && p.x < a.Min.x - CampGap + 0.8f
                    && Mathf.Abs(p.z - zc) < 4.4f) return true;
            }
            return false;
        }

        /// <summary>
        /// Pano de fundo que conta a história da partida: a muralha do castelo de quem defende,
        /// com o estandarte dele, e o acampamento do exército que ataca, nas cores do atacante.
        /// </summary>
        static void BuildBackdrop(WorldLayout layout, Transform root)
        {
            var fires = new System.Collections.Generic.List<Vector3>();
            int i = 0;
            foreach (var a in layout.PlayAreas)
            {
                Color owner = a.Owner.a > 0f ? a.Owner : Palette.TeamPlayer;
                Color attacker = a.Attacker.a > 0f ? a.Attacker : Palette.TeamFoe;
                float zc = (a.Min.y + a.Max.y) * 0.5f;
                float Y(float px, float pz) => layout.GroundHeight != null ? layout.GroundHeight(px, pz) : layout.Height(px, pz);

                float wx = a.Max.x + WallGap;
                var wall = new MeshBuilder();
                ModelLib.CastleWall(wall, 0f, a.Min.y - zc, a.Max.y - zc);
                var wgo = ArtFactory.Static("Muralha", wall, root, owner);
                wgo.transform.position = new Vector3(wx, Y(wx, zc) - 0.05f, zc);

                float cx = a.Min.x - CampGap;
                var camp = new MeshBuilder();
                var local = ModelLib.ArmyCamp(camp, 0f, 0f, 31 + i * 17);
                var cgo = ArtFactory.Static("Acampamento", camp, root, attacker);
                var basePos = new Vector3(cx, Y(cx, zc) - 0.02f, zc);
                cgo.transform.position = basePos;
                foreach (var f in local) fires.Add(basePos + f);
                i++;
            }
            if (fires.Count > 0) root.gameObject.AddComponent<CampFires>().Points = fires.ToArray();
        }
    }

    /// <summary>
    /// Vento nas árvores de verdade: balanço lento a partir da base, cada uma com fase e
    /// ritmo próprios, e rajadas que atravessam o campo. Um componente para todas (não
    /// um por árvore) — são dezenas de transforms, custo desprezível.
    /// </summary>
    public class TreeWind : MonoBehaviour
    {
        readonly System.Collections.Generic.List<Transform> _trees = new System.Collections.Generic.List<Transform>();
        readonly System.Collections.Generic.List<Quaternion> _rest = new System.Collections.Generic.List<Quaternion>();

        public void Add(Transform tree)
        {
            _trees.Add(tree);
            _rest.Add(tree.localRotation);
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < _trees.Count; i++)
            {
                var tr = _trees[i];
                if (tr == null) continue;
                var p = tr.position;
                // rajada: onda que anda pelo mapa, então árvores vizinhas se curvam juntas
                float gust = 0.6f + 0.4f * Mathf.Sin(t * 0.35f - p.x * 0.08f - p.z * 0.05f);
                float sway = Mathf.Sin(t * (0.9f + (i % 5) * 0.07f) + i * 1.7f) * 1.4f * gust;
                float side = Mathf.Sin(t * 0.7f + i * 2.3f) * 0.6f * gust;
                tr.localRotation = _rest[i] * Quaternion.Euler(sway, 0f, side);
            }
        }
    }

    /// <summary>Fogueiras do acampamento: chama e fagulha contínuas, cada uma no seu ritmo.</summary>
    public class CampFires : MonoBehaviour
    {
        public Vector3[] Points;
        float _t;

        void Update()
        {
            if (Points == null || Vfx.Instance == null) return;
            _t += Time.deltaTime;
            if (_t < 0.09f) return;
            _t = 0f;
            foreach (var p in Points) Vfx.Instance.Brazier(p);
        }
    }

    /// <summary>Correnteza: arrasta a textura da água devagar, rio abaixo.</summary>
    public class WaterFlow : MonoBehaviour
    {
        Material _mat;

        void Start() => _mat = GetComponent<MeshRenderer>().sharedMaterial;

        void Update()
        {
            if (_mat == null) return;
            _mat.mainTextureOffset = new Vector2(Time.time * 0.035f, Mathf.Sin(Time.time * 0.3f) * 0.02f);
        }
    }
}
