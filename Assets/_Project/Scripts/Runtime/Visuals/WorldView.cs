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
            foreach (var p in layout.ScatterScenery())
            {
                GameObject placed = null;
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
            return root;
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
