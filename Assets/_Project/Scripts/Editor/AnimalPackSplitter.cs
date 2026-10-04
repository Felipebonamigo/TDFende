using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TDFende.EditorTools
{
    /// <summary>
    /// Pacote com MUITOS bichos num FBX só (ex.: "Ultimate Animal Pack", 100 animais): o
    /// jogo precisa de um modelo por bicho. Quando um arquivo desses entra em
    /// Assets/Resources/TDFende/Bichos/, isto separa sozinho os nove bichos do jogo em
    /// prefabs (Bichos/Gerados/Inimigo_Elefante.prefab...), cada um com:
    ///   - só a malha e o esqueleto daquele bicho (o resto do pacote sai);
    ///   - um componente Animation com os clipes daquele bicho (o nome do clipe tem o bicho:
    ///     "Wolf|Wolf_Walk").
    /// E escreve Gerados/conteudo-do-pacote.txt com o que achou e o que faltou — é o arquivo
    /// para me mandar se algum bicho não for reconhecido.
    /// Também dá para rodar à mão: menu TDFende → Separar pacote de bichos.
    /// </summary>
    class AnimalPackSplitter : AssetPostprocessor
    {
        const string Root = "Assets/Resources/TDFende/Bichos/";
        const string OutDir = Root + "Gerados/";

        static readonly string[] Models =
        {
            "Inimigo_Rato", "Inimigo_Cachorro", "Inimigo_Lobo", "Inimigo_Javali", "Inimigo_Aguia",
            "Inimigo_Urso", "Inimigo_Tigre", "Inimigo_Rinoceronte", "Inimigo_Elefante",
        };

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var packs = imported.Where(p => p.StartsWith(Root) && !p.StartsWith(OutDir) && IsModel(p)).ToList();
            if (packs.Count == 0) return;
            // depois que a importação terminar: criar prefab dentro do postprocess é frágil
            EditorApplication.delayCall += () =>
            {
                foreach (var p in packs) SplitIfPack(p, false);
            };
        }

        [MenuItem("TDFende/Separar pacote de bichos")]
        static void SplitAll()
        {
            var guids = AssetDatabase.FindAssets("t:Model", new[] { Root.TrimEnd('/') });
            int done = 0;
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (!path.StartsWith(OutDir) && SplitIfPack(path, true)) done++;
            }
            if (done == 0)
                Debug.Log("[TDFende] nenhum pacote com vários bichos em " + Root);
        }

        static bool IsModel(string p)
        {
            string e = Path.GetExtension(p).ToLowerInvariant();
            return e == ".fbx" || e == ".glb" || e == ".gltf" || e == ".obj" || e == ".blend";
        }

        /// <summary>Nó do bicho: o mais raso cujo nome é o bicho e que tem malha embaixo.</summary>
        static List<Transform> FindAnimalNodes(Transform root, string model)
        {
            var found = new List<Transform>();
            var queue = new Queue<Transform>();
            foreach (Transform c in root) queue.Enqueue(c);
            while (queue.Count > 0)
            {
                var t = queue.Dequeue();
                if (AnimalLoader.ModelFor(t.name) == model)
                {
                    found.Add(t); // achou: não desce mais (o subnó é parte deste bicho)
                    continue;
                }
                foreach (Transform c in t) queue.Enqueue(c);
            }
            // só os que têm malha OU são o esqueleto de uma malha achada
            return found;
        }

        static bool SplitIfPack(string path, bool verbose)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) return false;

            // quantos bichos do jogo este arquivo tem? um só = arquivo comum, o loader resolve
            var present = Models.Where(m => FindAnimalNodes(asset.transform, m).Count > 0).ToList();
            if (present.Count < 2)
            {
                if (verbose) Debug.Log($"[TDFende] {Path.GetFileName(path)}: não é pacote ({present.Count} bicho reconhecido)");
                return false;
            }

            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToList();
            Directory.CreateDirectory(OutDir);

            var report = new StringBuilder();
            report.AppendLine($"Pacote: {Path.GetFileName(path)}");
            report.AppendLine($"Bichos reconhecidos: {present.Count} de 9");
            report.AppendLine();

            foreach (var model in Models)
            {
                var nodes = FindAnimalNodes(asset.transform, model);
                if (nodes.Count == 0)
                {
                    report.AppendLine($"[FALTOU] {model}: nenhum nó com o nome deste bicho");
                    continue;
                }

                var inst = (GameObject)Object.Instantiate(asset);
                inst.name = model;
                var keep = KeepSet(inst.transform, asset.transform, nodes);
                Prune(inst.transform, keep);

                foreach (var a in inst.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(a);
                foreach (var a in inst.GetComponentsInChildren<Animation>(true)) Object.DestroyImmediate(a);
                var anim = inst.AddComponent<Animation>();
                var mine = clips.Where(c => AnimalLoader.ModelFor(c.name) == model).ToList();
                foreach (var c in mine)
                {
                    c.legacy = true;
                    anim.AddClip(c, c.name);
                }
                if (mine.Count > 0) anim.clip = mine[0];

                string outPath = OutDir + model + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(inst, outPath);
                Object.DestroyImmediate(inst);
                report.AppendLine($"[OK] {model}: nó \"{string.Join("\", \"", nodes.Select(n => n.name))}\", " +
                                  $"{mine.Count} clipe(s): {string.Join(", ", mine.Select(c => c.name).Take(12))}");
            }

            report.AppendLine();
            report.AppendLine("Tudo o que tem no pacote (para eu ajustar se faltou algum bicho):");
            foreach (Transform c in asset.transform) report.AppendLine("  nó: " + c.name);
            foreach (var c in clips.Take(400)) report.AppendLine("  clipe: " + c.name);
            File.WriteAllText(OutDir + "conteudo-do-pacote.txt", report.ToString(), new UTF8Encoding(true));
            AssetDatabase.Refresh();
            Debug.Log($"[TDFende] pacote {Path.GetFileName(path)} separado: {present.Count} bicho(s) em {OutDir}");
            return true;
        }

        /// <summary>
        /// O que fica no prefab do bicho: os nós dele (com tudo embaixo) e os ossos que as
        /// malhas dele usam — às vezes o esqueleto é irmão da malha, não filho.
        /// </summary>
        static HashSet<Transform> KeepSet(Transform instRoot, Transform assetRoot, List<Transform> assetNodes)
        {
            var keep = new HashSet<Transform>();
            foreach (var n in assetNodes)
            {
                var t = instRoot.Find(RelPath(assetRoot, n));
                if (t == null) continue;
                foreach (var x in t.GetComponentsInChildren<Transform>(true)) keep.Add(x);
                foreach (var smr in t.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.rootBone != null)
                        foreach (var x in smr.rootBone.GetComponentsInChildren<Transform>(true)) keep.Add(x);
                    foreach (var b in smr.bones)
                        if (b != null) keep.Add(b);
                }
            }
            return keep;
        }

        static string RelPath(Transform root, Transform t)
        {
            var parts = new List<string>();
            for (var x = t; x != null && x != root; x = x.parent) parts.Insert(0, x.name);
            return string.Join("/", parts);
        }

        /// <summary>Apaga tudo que não é do bicho nem caminho até ele.</summary>
        static void Prune(Transform node, HashSet<Transform> keep)
        {
            for (int i = node.childCount - 1; i >= 0; i--)
            {
                var c = node.GetChild(i);
                if (keep.Contains(c)) continue; // do bicho: fica inteiro
                bool leadsToKept = c.GetComponentsInChildren<Transform>(true).Any(keep.Contains);
                if (leadsToKept) Prune(c, keep);
                else Object.DestroyImmediate(c.gameObject);
            }
        }
    }
}
