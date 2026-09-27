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
            ArtFactory.Static("Cenario", layout.BuildScenery(), root, neutral);
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
