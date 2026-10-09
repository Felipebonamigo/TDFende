using UnityEditor;

namespace TDFende.EditorTools
{
    /// <summary>
    /// Importação das torres baixadas em Assets/Resources/TDFende/Torres/ (Tools/ConverterTorres).
    /// O FBX traz só a malha do corpo, com os nós Shaft e Top já em pé e em escala de jogo
    /// (1 = uma célula): sem material (o ArtFactory monta o dele com as texturas .bytes da
    /// pasta Textures), sem animação, sem câmera nem luz.
    /// </summary>
    class TowerImportRules : AssetPostprocessor
    {
        internal const string Root = "Assets/Resources/TDFende/Torres/";

        public override uint GetVersion() => 1;

        void OnPreprocessModel()
        {
            if (!ArtRoots.Under(assetPath, Root)) return;
            var mi = (ModelImporter)assetImporter;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importBlendShapes = false;
            mi.useFileScale = true;
            mi.globalScale = 1f;
            mi.bakeAxisConversion = true;
            mi.preserveHierarchy = true; // Shaft e Top continuam filhos, com o nome que a vista procura
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.CalculateMikk; // normal map
            mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.isReadable = false;
        }
    }
}
