using System.IO;
using UnityEditor;
using UnityEngine;

namespace TDFende.EditorTools
{
    /// <summary>
    /// Importação dos bichos baixados em Assets/Resources/TDFende/Bichos/. Arrastar o FBX
    /// basta: Legacy (o AnimalLoader toca o clipe pelo nome, sem AnimatorController),
    /// materiais do próprio arquivo, e TODOS os clipes embutidos mantidos — os de andar,
    /// correr, ficar parado e voar em loop; o de morrer para no último quadro.
    /// Texturas dos bichos que vêm com o projeto (pasta Textures, ver Tools/ConverterBichos):
    /// "_normal" vira mapa de normal, "_alfa" tem recorte de pelo/pena no canal alfa.
    /// </summary>
    class AnimalImportRules : AssetPostprocessor
    {
        internal const string Root = "Assets/Resources/TDFende/Bichos/";

        public override uint GetVersion() => 2;

        void OnPreprocessModel()
        {
            if (!ArtRoots.Under(assetPath, Root)) return;
            var mi = (ModelImporter)assetImporter;
            mi.animationType = ModelImporterAnimationType.Legacy;
            mi.importAnimation = true;
            mi.importCameras = false;
            mi.importLights = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        void OnPreprocessTexture()
        {
            if (!ArtRoots.Under(assetPath, Root)) return;
            var ti = (TextureImporter)assetImporter;
            string file = Path.GetFileNameWithoutExtension(assetPath);
            if (file.EndsWith("_normal")) ti.textureType = TextureImporterType.NormalMap;
            if (file.EndsWith("_alfa"))
            {
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.alphaIsTransparency = true;
            }
            ti.maxTextureSize = Mathf.Min(ti.maxTextureSize, 1024);
        }

        void OnPreprocessAnimation()
        {
            if (!ArtRoots.Under(assetPath, Root)) return;
            var mi = (ModelImporter)assetImporter;
            var clips = mi.defaultClipAnimations;
            if (clips.Length == 0) return;
            string file = Path.GetFileNameWithoutExtension(assetPath);
            bool separate = file.Contains("@");
            for (int i = 0; i < clips.Length; i++)
            {
                // arquivo só de animação ("Elefante@Walk"): o clipe leva o nome do arquivo
                if (separate && clips.Length == 1) clips[i].name = file;
                string n = clips[i].name.ToLowerInvariant();
                bool death = n.Contains("death") || n.Contains("die") || n.Contains("dead");
                clips[i].loopTime = !death;
                clips[i].wrapMode = death ? WrapMode.ClampForever : WrapMode.Loop;
            }
            mi.clipAnimations = clips;
        }
    }
}
