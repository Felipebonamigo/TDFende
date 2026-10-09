using System.IO;
using UnityEditor;
using UnityEngine;

namespace TDFende.EditorTools
{
    /// <summary>
    /// Importação dos personagens baixados (Mixamo) em Assets/Resources/TDFende/Personagens/.
    /// Arrastar os FBX para a pasta basta — nada de configurar no Inspector:
    ///
    ///   Nome.fbx        modelo com pele: Legacy, materiais do próprio FBX, sem animação
    ///   Nome@Walk.fbx   só animação: Legacy, clipe chamado "Nome@Walk", sem material
    ///
    /// Legacy e não Humanoid/Mecanim de propósito: o CharacterLoader toca o clipe pelo
    /// nome, em runtime, sem precisar de AnimatorController montado no editor.
    /// </summary>
    class CharacterImportRules : AssetPostprocessor
    {
        internal const string Root = "Assets/Resources/TDFende/Personagens/";

        public override uint GetVersion() => 1;

        static bool IsClipFile(string path) => Path.GetFileNameWithoutExtension(path).Contains("@");

        void OnPreprocessModel()
        {
            if (!ArtRoots.Under(assetPath, Root)) return;
            var mi = (ModelImporter)assetImporter;
            mi.animationType = ModelImporterAnimationType.Legacy;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importAnimation = IsClipFile(assetPath);
            mi.materialImportMode = IsClipFile(assetPath)
                ? ModelImporterMaterialImportMode.None
                : ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        void OnPreprocessAnimation()
        {
            if (!ArtRoots.Under(assetPath, Root) || !IsClipFile(assetPath)) return;
            var mi = (ModelImporter)assetImporter;
            var clips = mi.defaultClipAnimations;
            if (clips.Length == 0) return;

            string file = Path.GetFileNameWithoutExtension(assetPath);
            string state = file.Substring(file.IndexOf('@') + 1);
            bool loop = state == "Walk" || state == "Run" || state == "Idle";
            // o Mixamo chama todo clipe de "mixamo.com"; renomear pelo arquivo é o que
            // permite achar "o Walk do Recruta" em runtime
            clips[0].name = file;
            clips[0].loopTime = loop;
            clips[0].wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;
            mi.clipAnimations = new[] { clips[0] };
        }
    }
}
