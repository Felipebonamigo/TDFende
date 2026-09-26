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
        public static Transform Build(WorldLayout layout)
        {
            var root = new GameObject("Mundo").transform;
            var neutral = Color.white;

            var terrain = ArtFactory.Static("Terreno", layout.BuildTerrain(), root, neutral, castShadows: false);
            terrain.GetComponent<MeshRenderer>().receiveShadows = true;

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
