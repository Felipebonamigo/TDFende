using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TDFende.EditorTools
{
    /// <summary>
    /// Gera o executável de Windows (Builds/Windows/TDFende.exe) para testar sem abrir o Unity.
    /// Menu TDFende → Gerar executável, ou de fora do editor: Tools\GerarExecutavel.ps1
    /// (Unity -batchmode -executeMethod TDFende.EditorTools.BuildJogo.Windows).
    ///
    /// O jogo monta tudo em código, então o build precisa de duas coisas que o Play não precisa:
    ///   - uma cena (Assets/Scenes/Jogo.unity), só com a câmera: o GameBootstrap faz o resto;
    ///   - os shaders que o código acha por nome (Shader.Find) com as keywords que ele liga. Sem
    ///     nenhum material usando, o build descarta o shader ou a variante, e no executável sai
    ///     rosa ou sem normal map. Por isso <see cref="ShaderKeep"/> cria um material por
    ///     combinação em Resources/TDFende/ShaderKeep — o build leva tudo o que está em Resources.
    /// </summary>
    public static class BuildJogo
    {
        const string ScenePath = "Assets/Scenes/Jogo.unity";
        const string KeepDir = "Assets/Resources/TDFende/ShaderKeep";
        const string Output = "Builds/Windows/TDFende.exe";

        /// <summary>Shader e as combinações de keyword que o código usa nele.</summary>
        static readonly (string shader, string[][] combos)[] Shaders =
        {
            ("Universal Render Pipeline/Lit", new[]
            {
                new string[0],
                new[] { "_NORMALMAP" },
                new[] { "_EMISSION" },
                new[] { "_NORMALMAP", "_EMISSION" },
                new[] { "_ALPHATEST_ON" },
                new[] { "_ALPHATEST_ON", "_NORMALMAP" },
            }),
            ("Universal Render Pipeline/Terrain/Lit", new[]
            {
                new string[0],
                new[] { "_NORMALMAP" },
                new[] { "_NORMALMAP", "_TERRAIN_INSTANCED_PERPIXEL_NORMAL" },
            }),
            ("Skybox/Panoramic", new[] { new string[0] }),
            ("Skybox/Procedural", new[] { new string[0] }),
            ("TDFende/SoftParticle", new[] { new string[0] }),
            ("TDFende/TerritoryOverlay", new[] { new string[0] }),
        };

        [MenuItem("TDFende/Gerar executável")]
        public static void Windows()
        {
            EnsureScene();
            ShaderKeep();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log($"[TDFende] build {s.result}: {Path.GetFullPath(Output)} " +
                      $"({s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:0} s, {s.totalErrors} erro(s))");
            if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>Cena mínima: só a câmera principal (luz, céu e mundo vêm do código).</summary>
        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cam = new GameObject("Main Camera") { tag = "MainCamera" };
                cam.AddComponent<Camera>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ShaderKeep()
        {
            Directory.CreateDirectory(KeepDir);
            foreach (var (name, combos) in Shaders)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning($"[TDFende] build: shader {name} não encontrado");
                    continue;
                }
                foreach (var kws in combos)
                {
                    string file = $"{KeepDir}/{name.Replace('/', '_').Replace(' ', '_')}{(kws.Length > 0 ? "__" + string.Join("_", kws) : "")}.mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(file);
                    if (mat == null)
                    {
                        mat = new Material(shader);
                        AssetDatabase.CreateAsset(mat, file);
                    }
                    mat.shader = shader;
                    mat.shaderKeywords = kws;
                    if (System.Array.IndexOf(kws, "_ALPHATEST_ON") >= 0 && mat.HasProperty("_AlphaClip"))
                        mat.SetFloat("_AlphaClip", 1f);
                    EditorUtility.SetDirty(mat);
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
