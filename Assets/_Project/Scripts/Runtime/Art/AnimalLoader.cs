using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Bicho de verdade (modelo 3D baixado, com esqueleto e animação) no lugar do procedural.
    /// O procedural é feito de esferas e cilindros: dá silhueta, mas não parece um animal
    /// real. Com um arquivo em Assets/Resources/TDFende/Bichos/, o jogo usa o arquivo.
    ///
    /// Regras para o arquivo, pensadas para "baixou, arrastou, funcionou":
    ///   - o NOME só precisa conter o bicho, em português ou inglês: "elefante.fbx",
    ///     "African_Elephant_Walk.fbx", "tiger-animated.glb"... (ver <see cref="Keywords"/>);
    ///   - as animações podem vir DENTRO do arquivo (o normal de quem baixa) ou em arquivos
    ///     separados "Nome@Walk"; os clipes são achados pelo nome: walk/trot = andar,
    ///     run/gallop/sprint = correr, idle/stand = parado, death/die = morrer;
    ///   - tamanho e altura do chão são acertados sozinhos; se o bicho vier virado de
    ///     lado, ponha "_giro90", "_giro-90" ou "_giro180" no nome do arquivo.
    /// Sem arquivo, segue o procedural — nada quebra.
    /// </summary>
    public static class AnimalLoader
    {
        public const string Folder = "TDFende/Bichos";

        /// <summary>Palavras que identificam cada bicho no nome do arquivo (sem acento, minúsculas).</summary>
        /// <remarks>
        /// Do maior para o menor, e o rato por último: "rat" aparece dentro de outras palavras
        /// ("separate", "pirate"), então só vale se nenhum outro bicho bateu antes.
        /// </remarks>
        static readonly (string model, string[] words)[] Keywords =
        {
            ("Inimigo_Elefante", new[] { "elefante", "elephant", "mammoth" }),
            ("Inimigo_Rinoceronte", new[] { "rinoceronte", "rhino" }),
            ("Inimigo_Tigre", new[] { "tigre", "tiger" }),
            ("Inimigo_Urso", new[] { "urso", "bear", "grizzly" }),
            ("Inimigo_Aguia", new[] { "aguia", "falcao", "gaviao", "eagle", "hawk", "falcon" }),
            ("Inimigo_Javali", new[] { "javali", "porco", "boar", "warthog", "hog", "pig" }),
            ("Inimigo_Lobo", new[] { "lobo", "wolf" }),                    // antes do cachorro: "wolfdog" é lobo
            ("Inimigo_Cachorro", new[] { "cachorro", "cao", "dog", "hound", "husky", "shiba", "shepherd" }),
            ("Inimigo_Rato", new[] { "rato", "ratazana", "mouse", "rat" }),
        };

        static Dictionary<string, GameObject> _byModel;
        static AnimationClip[] _clips;

        static string Plain(string s)
        {
            s = s.ToLowerInvariant();
            var map = new Dictionary<char, char> { { 'á', 'a' }, { 'â', 'a' }, { 'ã', 'a' }, { 'à', 'a' }, { 'é', 'e' }, { 'ê', 'e' },
                { 'í', 'i' }, { 'ó', 'o' }, { 'ô', 'o' }, { 'õ', 'o' }, { 'ú', 'u' }, { 'ç', 'c' } };
            var chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (map.TryGetValue(chars[i], out var c)) chars[i] = c;
            return new string(chars);
        }

        /// <summary>Qual ModelDef este nome de arquivo representa (null = nenhum bicho reconhecido).</summary>
        public static string ModelFor(string fileName)
        {
            string n = Plain(fileName);
            foreach (var (model, words) in Keywords)
                foreach (var w in words)
                    if (n.Contains(w)) return model;
            return null;
        }

        static void Index()
        {
            if (_byModel != null) return;
            _byModel = new Dictionary<string, GameObject>();
            foreach (var go in Resources.LoadAll<GameObject>(Folder))
            {
                if (go == null || go.name.Contains("@")) continue; // "@Walk" é só animação
                string model = ModelFor(go.name);
                if (model != null && !_byModel.ContainsKey(model)) _byModel[model] = go;
            }
            _clips = Resources.LoadAll<AnimationClip>(Folder);
            if (_byModel.Count > 0)
                Debug.Log($"[TDFende] bichos de verdade: {string.Join(", ", _byModel.Keys)}");
        }

        public static bool Has(ModelDef def)
        {
            Index();
            return _byModel.ContainsKey(def.Name);
        }

        /// <summary>Monta o bicho de verdade sob <paramref name="root"/>. Null se não há arquivo.</summary>
        public static GameObject TrySpawn(ModelDef def, Transform root, Color team, out Animation anim)
        {
            anim = null;
            Index();
            if (!_byModel.TryGetValue(def.Name, out var prefab)) return null;

            // pivô intermediário: giro de correção e escala ficam nele, o root continua livre
            var holder = new GameObject("Bicho").transform;
            holder.SetParent(root, false);
            var inst = Object.Instantiate(prefab, holder, false);
            inst.name = prefab.name;

            string plain = Plain(prefab.name);
            float yaw = plain.Contains("_giro-90") ? -90f : plain.Contains("_giro90") ? 90f : plain.Contains("_giro180") ? 180f : 0f;
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // tamanho: altura do bicho no jogo = a do procedural (rato miúdo, elefante grande)
            var b = Bounds(inst);
            bool flies = def.Anim == AnimKind.Glider;
            float target = flies ? 0.45f : def.Height;
            if (b.size.y > 0.0001f)
            {
                float k = target / b.size.y;
                // asa aberta mede pouco na altura: para ave, mede pela envergadura
                if (flies) k = 0.9f / Mathf.Max(b.size.x, b.size.z, 0.0001f);
                holder.localScale = Vector3.one * k;
                b = Bounds(inst);
            }
            // pé no chão (ou no ar, para quem voa) e centro no pivô
            var local = root.InverseTransformPoint(b.center);
            float floor = root.InverseTransformPoint(b.min).y;
            float lift = flies ? 1.1f - (b.size.y * 0.5f) : 0f;
            holder.localPosition += new Vector3(-local.x, -floor + lift, -local.z);

            anim = inst.GetComponentInChildren<Animation>();
            if (anim == null)
            {
                // modelo com Animator (Mecanim): troca por Animation para tocar clipe pelo nome
                var animator = inst.GetComponentInChildren<Animator>();
                var host = animator != null ? animator.gameObject : inst;
                if (animator != null) Object.Destroy(animator);
                anim = host.AddComponent<Animation>();
            }
            AddClips(anim, prefab.name);
            anim.cullingType = AnimationCullingType.BasedOnRenderers;
            // animação que anda para a frente sozinha ("root motion") faria o bicho escorregar e
            // voltar: quem anda é a simulação, então o osso-raiz fica preso no lugar
            inst.AddComponent<RootLock>();
            if (anim.GetClip("Idle") != null) anim.Play("Idle");
            else if (anim.GetClip("Walk") != null) anim.Play("Walk");

            PrepareRenderers(inst);
            AddTeamRing(root, team, Mathf.Max(def.Height, 0.3f), flies);
            return inst;
        }

        /// <summary>
        /// Clipes embutidos no próprio arquivo (Resources.LoadAll com o caminho do arquivo
        /// devolve só o que está dentro dele) e os "Nome@Estado" de arquivos à parte do mesmo
        /// bicho. Cada clipe vai para o estado certo pelo nome; se houver dois de andar, fica o
        /// primeiro.
        /// </summary>
        static void AddClips(Animation anim, string fileName)
        {
            string model = ModelFor(fileName);
            var mine = new List<AnimationClip>(Resources.LoadAll<AnimationClip>(Folder + "/" + fileName));
            foreach (var clip in _clips)
            {
                if (clip == null || !clip.name.Contains("@")) continue;
                string owner = clip.name.Substring(0, clip.name.IndexOf('@'));
                if (ModelFor(owner) == model) mine.Add(clip);
            }
            foreach (var clip in mine)
            {
                if (clip == null || clip.name.StartsWith("__preview__")) continue;
                string state = StateFor(clip.name);
                if (state == null || anim.GetClip(state) != null) continue;
                clip.legacy = true;
                anim.AddClip(clip, state);
                anim[state].wrapMode = state == "Death" ? WrapMode.ClampForever : WrapMode.Loop;
            }
            // arquivo com um clipe só e nome sem pista ("Take 001", "Armature|mixamo"): é o andar
            if (anim.GetClipCount() == 0 && mine.Count > 0 && mine[0] != null)
            {
                mine[0].legacy = true;
                anim.AddClip(mine[0], "Walk");
                anim["Walk"].wrapMode = WrapMode.Loop;
            }
        }

        static string StateFor(string clipName)
        {
            string n = Plain(clipName);
            if (n.Contains("death") || n.Contains("die") || n.Contains("dead") || n.Contains("morre")) return "Death";
            if (n.Contains("run") || n.Contains("gallop") || n.Contains("sprint") || n.Contains("corre")) return "Run";
            if (n.Contains("walk") || n.Contains("trot") || n.Contains("anda")) return "Walk";
            if (n.Contains("idle") || n.Contains("stand") || n.Contains("breath") || n.Contains("parado")) return "Idle";
            if (n.Contains("fly") || n.Contains("flap") || n.Contains("voa")) return "Walk"; // ave: voar é o "andar"
            return null;
        }

        /// <summary>
        /// Sombra ligada e emissão liberada nos materiais (cópia por bicho): é por ela que
        /// passam o clarão do acerto, o brilho do fogo e o azul do gelo.
        /// </summary>
        static void PrepareRenderers(GameObject inst)
        {
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = false;
                foreach (var m in r.materials)
                {
                    if (m == null) continue;
                    m.EnableKeyword("_EMISSION");
                    if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
            }
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Anel no chão com a cor do time: o bicho baixado não tem coleira tingível.</summary>
        static void AddTeamRing(Transform root, Color team, float height, bool flies)
        {
            var ring = Overlays.MakeObject("AnelTime", Overlays.Ring(0.22f), root, new Color(team.r, team.g, team.b, 0.8f));
            float r = Mathf.Max(0.18f, height * 0.45f);
            ring.transform.localScale = new Vector3(r, 1f, r);
            ring.transform.localPosition = Vector3.up * 0.012f;
        }
    }

    /// <summary>Prende o osso-raiz no lugar (só no chão, X e Z): a animação não arrasta o bicho.</summary>
    public sealed class RootLock : MonoBehaviour
    {
        Transform _bone;
        Vector3 _rest;

        void Start()
        {
            var smr = GetComponentInChildren<SkinnedMeshRenderer>();
            _bone = smr != null && smr.rootBone != null ? smr.rootBone : null;
            if (_bone != null) _rest = _bone.localPosition;
        }

        void LateUpdate()
        {
            if (_bone == null) return;
            var p = _bone.localPosition;
            _bone.localPosition = new Vector3(_rest.x, p.y, _rest.z);
        }
    }
}
