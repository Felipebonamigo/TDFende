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
        const string DiagnosticOutput = "Builds/Diagnostico/TDFende.exe";

        /// <summary>
        /// Shader e as combinações de keyword que o código usa nele. A captura (-captura) reprova
        /// material criado em runtime com combinação que não esteja aqui, e diz qual acrescentar.
        /// </summary>
        static readonly (string shader, string[][] combos)[] Shaders =
        {
            (ShaderRefs.LitName, new[]
            {
                new string[0],
                new[] { "_NORMALMAP" },
                new[] { "_EMISSION" },
                new[] { "_NORMALMAP", "_EMISSION" },
                new[] { "_ALPHATEST_ON" },
                new[] { "_ALPHATEST_ON", "_NORMALMAP" },
                new[] { "_ALPHATEST_ON", "_EMISSION" },               // pelo do lobo e da águia
                new[] { "_ALPHATEST_ON", "_NORMALMAP", "_EMISSION" }, // pelo do rato
            }),
            (ShaderRefs.TerrainLitName, new[]
            {
                new string[0],
                new[] { "_TERRAIN_INSTANCED_PERPIXEL_NORMAL" }, // o que o GroundBuilder liga
                new[] { "_NORMALMAP" },
                new[] { "_NORMALMAP", "_TERRAIN_INSTANCED_PERPIXEL_NORMAL" },
            }),
            (ShaderRefs.SkyboxPanoramicName, new[] { new string[0] }),
            (ShaderRefs.SkyboxProceduralName, new[] { new string[0] }),
            (ShaderRefs.SoftParticleName, new[] { new string[0] }),
            (ShaderRefs.TerritoryOverlayName, new[] { new string[0] }),
        };

        [MenuItem("TDFende/Gerar executável")]
        public static void Windows()
        {
            bool ok = Build(Output);
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>
        /// Executável de diagnóstico (Builds/Diagnostico) com strictShaderVariantMatching: variante
        /// que falta sai como erro no Player.log e o objeto fica rosa, em vez de o Unity trocar em
        /// silêncio pela variante mais parecida. Confirma o que a captura acusa por censo (TEC-11).
        /// A opção é do ProjectSettings: liga só durante o build e volta ao que era.
        /// </summary>
        [MenuItem("TDFende/Gerar executável de diagnóstico (variantes estritas)")]
        public static void Diagnostico()
        {
            bool before = PlayerSettings.strictShaderVariantMatching;
            bool ok;
            PlayerSettings.strictShaderVariantMatching = true;
            try { ok = Build(DiagnosticOutput); }
            finally { PlayerSettings.strictShaderVariantMatching = before; }
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool Build(string output)
        {
            // selo (TEC-03): o commit vira a versão do executável (Application.version), que o
            // BuildStamp mostra no canto da tela e no Player.log. Só durante o build, para não
            // sujar o ProjectSettings a cada build
            string version = PlayerSettings.bundleVersion;
            string stamp = GitDescribe();
            PlayerSettings.bundleVersion = stamp;
            try
            {
                EnsureScene();
                ShaderKeep();
                var opts = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None,
                };
                var report = BuildPipeline.BuildPlayer(opts);
                var s = report.summary;
                Debug.Log($"[TDFende] build {s.result}: {Path.GetFullPath(output)} selo {stamp} " +
                          $"({s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:0} s, {s.totalErrors} erro(s))");
                return s.result == BuildResult.Succeeded;
            }
            finally { PlayerSettings.bundleVersion = version; }
        }

        /// <summary>`git describe --always --dirty` (hash curto, "-dirty" com mudança sem commit).</summary>
        static string GitDescribe()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("git", "describe --always --dirty")
                {
                    WorkingDirectory = Path.GetDirectoryName(Application.dataPath),
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    string text = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit();
                    if (p.ExitCode == 0 && text.Length > 0) return text;
                }
            }
            catch (System.Exception e) { Debug.LogWarning("[TDFende] build: git describe falhou: " + e.Message); }
            return "sem-git";
        }

        /// <summary>
        /// Cena mínima: só a câmera principal (luz, céu e mundo vêm do código). A névoa linear fica
        /// ligada na cena porque o corte automático de névoa (GraphicsSettings: Fog Modes =
        /// Automatic) só guarda as variantes FOG_LINEAR se alguma cena do build usar névoa linear;
        /// sem isso, a névoa que o SceneAmbience liga em runtime não existia no executável.
        /// </summary>
        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var created = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cam = new GameObject("Main Camera") { tag = "MainCamera" };
                cam.AddComponent<Camera>();
                EditorSceneManager.SaveScene(created, ScenePath);
            }
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!RenderSettings.fog || RenderSettings.fogMode != FogMode.Linear)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
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
                    // o projeto descarta variante de instancing que nenhum material usa
                    // (GraphicsSettings: Instancing Variants = Strip Unused), e a grama
                    // (RenderMeshInstanced) e o terreno (drawInstanced) só desenham com ela
                    mat.enableInstancing = true;
                    if (System.Array.IndexOf(kws, "_ALPHATEST_ON") >= 0 && mat.HasProperty("_AlphaClip"))
                        mat.SetFloat("_AlphaClip", 1f);
                    EditorUtility.SetDirty(mat);
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
