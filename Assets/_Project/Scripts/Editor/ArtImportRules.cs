using System.IO;
using UnityEditor;
using UnityEngine;

namespace TDFende.EditorTools
{
    /// <summary>
    /// Configuração de importação da arte em Assets/Resources/Art/, por convenção de nome
    /// (a da Poly Haven): *_nor_gl* é mapa normal, *_diffalpha* é cor com recorte, Sky/ é
    /// HDRI panorâmico, modelos não trazem material (o código cria o dele).
    ///
    /// Errar isto não quebra nada visivelmente — só deixa feio de um jeito difícil de
    /// diagnosticar: normal map importado como cor vira relevo invertido e manchado; folha
    /// com mipmap comum some de longe porque o alfa médio cai abaixo do corte.
    /// </summary>
    class ArtImportRules : AssetPostprocessor
    {
        internal const string Root = "Assets/Resources/Art/";

        // mudar este número reimporta tudo que passou por aqui
        public override uint GetVersion() => 1;

        void OnPreprocessTexture()
        {
            if (!ArtRoots.Under(assetPath, Root)) return;
            Apply((TextureImporter)assetImporter, assetPath);
        }

        void OnPreprocessModel()
        {
            if (!ArtRoots.Under(assetPath, Root)) return;
            var mi = (ModelImporter)assetImporter;
            mi.materialImportMode = ModelImporterMaterialImportMode.None; // material vem do código
            mi.importAnimation = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.bakeAxisConversion = true; // Blender é Z-up; assado na malha, o nó fica limpo
        }

        /// <summary>Regras de textura. Devolve true se mudou alguma coisa.</summary>
        internal static bool Apply(TextureImporter ti, string path)
        {
            string file = Path.GetFileNameWithoutExtension(path);
            bool changed = false;

            void Set<T>(T current, T wanted, System.Action<T> set)
            {
                if (Equals(current, wanted)) return;
                set(wanted);
                changed = true;
            }

            if (ArtRoots.Under(path, Root + "Sky/"))
            {
                Set(ti.textureShape, TextureImporterShape.Texture2D, v => ti.textureShape = v);
                // Mip LIGADO: o céu nunca aparece na tela (câmera a 55° para baixo), ele só
                // alimenta luz ambiente e reflexo, capturados em baixa resolução. Sem mip,
                // o sol do HDRI (~3 pixels) entra ou não na captura conforme o alinhamento.
                Set(ti.mipmapEnabled, true, v => ti.mipmapEnabled = v);
                Set(ti.wrapModeU, TextureWrapMode.Repeat, v => ti.wrapModeU = v);
                Set(ti.wrapModeV, TextureWrapMode.Clamp, v => ti.wrapModeV = v);
                Set(ti.maxTextureSize, 2048, v => ti.maxTextureSize = v);
                return changed;
            }

            Set(ti.maxTextureSize, 2048, v => ti.maxTextureSize = v);
            Set(ti.anisoLevel, 8, v => ti.anisoLevel = v); // chão visto inclinado: sem isto borra

            if (file.Contains("_nor_gl"))
            {
                Set(ti.textureType, TextureImporterType.NormalMap, v => ti.textureType = v);
            }
            else if (file.Contains("_diffalpha"))
            {
                Set(ti.alphaIsTransparency, true, v => ti.alphaIsTransparency = v);
                // mantém a cobertura do recorte nos mipmaps: sem isto a grama some ao longe
                Set(ti.mipMapsPreserveCoverage, true, v => ti.mipMapsPreserveCoverage = v);
                Set(ti.alphaTestReferenceValue, 0.5f, v => ti.alphaTestReferenceValue = v);
            }
            return changed;
        }
    }

    /// <summary>
    /// Rede de segurança da ordem de importação: se a arte entrou no projeto ANTES deste
    /// script compilar, ela foi importada sem as regras. Confere tudo ao carregar o editor
    /// e reimporta só o que divergir.
    /// </summary>
    [InitializeOnLoad]
    static class ArtImportConvergence
    {
        static ArtImportConvergence() => EditorApplication.delayCall += Check;

        static void Check()
        {
            if (!AssetDatabase.IsValidFolder(ArtImportRules.Root.TrimEnd('/'))) return;
            int fixedCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtImportRules.Root.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter ti && ArtImportRules.Apply(ti, path))
                {
                    ti.SaveAndReimport();
                    fixedCount++;
                }
            }
            if (fixedCount > 0)
                Debug.Log($"[TDFende] {fixedCount} textura(s) de arte reimportada(s) com as regras certas.");
        }
    }
}
