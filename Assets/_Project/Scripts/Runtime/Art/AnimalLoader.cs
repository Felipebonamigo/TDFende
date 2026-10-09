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
        /// <summary>Pasta de Resources de onde cada bicho veio: a pública ou a privada (TEC-23).</summary>
        static Dictionary<string, string> _folderOf;
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
        /// <summary>
        /// Nomes que CONTÊM a palavra de um bicho mas são outro bicho (ou coisa nenhuma):
        /// "prairie dog" não é cachorro, "hedgehog" não é javali, "pirate" não é rato.
        /// Pacote grande (100 bichos num arquivo) tem muito disso.
        /// </summary>
        static readonly string[] NotThese =
        {
            "prairie", "hedgehog", "groundhog", "guinea", "hotdog", "hot dog", "sea lion", "sealion",
            "pirate", "separate", "crate", "karate", "rattle", "grate", "pirat", "muskrat", "bearded",
            "dogfish", "catfish", "dragon", "lizard", "aardvark", "aardwolf",
        };

        public static string ModelFor(string fileName)
        {
            string n = Plain(fileName);
            foreach (var bad in NotThese)
                if (n.Contains(bad)) return null;
            foreach (var (model, words) in Keywords)
                foreach (var w in words)
                    if (n.Contains(w)) return model;
            return null;
        }

        static void Index()
        {
            if (_byModel != null) return;
            _byModel = new Dictionary<string, GameObject>();
            _folderOf = new Dictionary<string, string>();
            // a camada privada (pacote pago, fora do git) vem primeiro e nunca perde para o público
            string privateFolder = ArtLayerPaths.PrivatePath(Folder);
            var fromPrivate = new HashSet<string>();
            void Add(GameObject go, string folder, bool isPrivate)
            {
                if (go == null || go.name.Contains("@")) return; // "@Walk" é só animação
                // prefab já separado de um pacote ("Inimigo_Elefante", pasta Gerados) vence
                // qualquer outro arquivo do mesmo bicho da mesma camada
                string model = go.name.StartsWith("Inimigo_") ? go.name : ModelFor(go.name);
                if (model == null) return;
                if (!isPrivate && fromPrivate.Contains(model)) return;
                if (!_byModel.ContainsKey(model) || go.name.StartsWith("Inimigo_") || (isPrivate && !fromPrivate.Contains(model)))
                {
                    _byModel[model] = go;
                    _folderOf[model] = folder;
                    if (isPrivate) fromPrivate.Add(model);
                }
            }
            foreach (var go in ArtLayers.LoadAllPrivate<GameObject>(Folder)) Add(go, privateFolder, true);
            foreach (var go in Resources.LoadAll<GameObject>(Folder)) Add(go, Folder, false);
            _clips = ArtLayers.LoadAll<AnimationClip>(Folder);
            if (_byModel.Count > 0)
                Debug.Log($"[TDFende] bichos de verdade: {string.Join(", ", _byModel.Keys)}"
                    + (fromPrivate.Count > 0 ? $" (da camada privada: {string.Join(", ", fromPrivate)})" : ""));
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

            anim = inst.GetComponentInChildren<Animation>();
            if (anim == null)
            {
                // modelo com Animator (Mecanim): troca por Animation para tocar clipe pelo nome
                var animator = inst.GetComponentInChildren<Animator>();
                var host = animator != null ? animator.gameObject : inst;
                if (animator != null) Object.DestroyImmediate(animator);
                anim = host.AddComponent<Animation>();
            }
            AddClips(anim, prefab.name, _folderOf.TryGetValue(def.Name, out var folder) ? folder : Folder);
            anim.cullingType = AnimationCullingType.BasedOnRenderers;
            // pose do jogo ANTES de medir: a escala que vale é a da malha animada, não a do arquivo
            string pose = anim.GetClip("Walk") != null ? "Walk" : anim.GetClip("Idle") != null ? "Idle" : null;
            if (pose != null)
            {
                anim.Play(pose);
                anim.Sample();
            }

            // tamanho: altura do bicho no jogo = a do procedural (rato miúdo, elefante grande).
            // Mede a malha como ela é desenhada (RealBounds), não a caixa guardada no arquivo:
            // em cachorro, lobo, rato e águia a caixa dizia o tamanho certo enquanto a malha
            // animada encolhia a quase um ponto, e o bicho ficava invisível (BUG-01, 09/10/2026).
            var b = RealBounds(inst);
            bool flies = def.Anim == AnimKind.Glider;
            float target = flies ? 0.45f : def.Height;
            if (b.size.y > 1e-7f)
            {
                float k = target / b.size.y;
                // asa aberta mede pouco na altura: para ave, mede pela envergadura
                if (flies) k = 0.9f / Mathf.Max(b.size.x, b.size.z, 1e-7f);
                holder.localScale = Vector3.one * k;
                b = RealBounds(inst);
            }
            FitCullingBounds(inst);
            // pé no chão (ou no ar, para quem voa) e centro no pivô
            var local = root.InverseTransformPoint(b.center);
            float floor = root.InverseTransformPoint(b.min).y;
            float lift = flies ? 1.1f - (b.size.y * 0.5f) : 0f;
            holder.localPosition += new Vector3(-local.x, -floor + lift, -local.z);
            // animação que anda para a frente sozinha ("root motion") faria o bicho escorregar e
            // voltar: quem anda é a simulação, então o osso-raiz fica preso no lugar
            inst.AddComponent<RootLock>();
            // modelo sem animação de andar (só malha): balanço de passada no lugar da perna,
            // para não deslizar pelo campo feito estátua
            if (anim.GetClip("Walk") == null && anim.GetClip("Run") == null)
                holder.gameObject.AddComponent<GaitBob>().Init(Mathf.Max(def.Height, 0.2f));
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
        static void AddClips(Animation anim, string fileName, string folder)
        {
            string model = fileName.StartsWith("Inimigo_") ? fileName : ModelFor(fileName);
            var mine = new List<AnimationClip>(Resources.LoadAll<AnimationClip>(folder + "/" + fileName));
            // prefab separado de um pacote já traz os clipes do bicho no componente Animation:
            // tira e devolve cada um com o nome do estado (Walk, Run, Idle, Death)
            var existing = new List<AnimationClip>();
            foreach (AnimationState st in anim)
                if (st.clip != null) existing.Add(st.clip);
            foreach (var c in existing) anim.RemoveClip(c);
            mine.InsertRange(0, existing);
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
            // o clipe padrão do componente ainda apontava para um dos que saíram acima: cada vez
            // que o bicho religava, o Unity avisava "Default clip could not be found" (BUG-02)
            var first = anim.GetClip("Idle");
            if (first == null) first = anim.GetClip("Walk");
            anim.clip = first;
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
                    FixSurface(m);
                    m.EnableKeyword("_EMISSION");
                    if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
            }
        }

        /// <summary>
        /// Pelo e pena dos bichos realistas são cartões com textura recortada: a textura de cor
        /// termina em "_alfa" (Tools/ConverterBichos). Esses viram recorte (alpha clip, as duas
        /// faces); o resto fica opaco à força — o FBX às vezes chega marcado como transparente e
        /// o bicho sairia fantasma, com ordem de desenho errada. Brilho baixo: bicho não é vidro.
        /// </summary>
        static void FixSurface(Material m)
        {
            var tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap")
                : m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            bool cut = tex != null && tex.name.EndsWith("_alfa");
            // URP Lit
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 0f);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", cut ? 1f : 0f);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", cut ? 0f : 2f);
            // Built-in Standard: 0 = opaco, 1 = recorte
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", cut ? 1f : 0f);
            if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.45f);
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", 1f);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", 0f);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 1f);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            if (cut) m.EnableKeyword("_ALPHATEST_ON"); else m.DisableKeyword("_ALPHATEST_ON");
            m.SetOverrideTag("RenderType", cut ? "TransparentCutout" : "Opaque");
            m.renderQueue = cut ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", Mathf.Min(m.GetFloat("_Smoothness"), 0.25f));
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", Mathf.Min(m.GetFloat("_Glossiness"), 0.25f));
        }

        static readonly Mesh Baked = new Mesh { name = "MedidaBicho" };

        /// <summary>
        /// Caixa, em mundo, da malha como está agora (pose animada incluída). Para malha com
        /// esqueleto, "assa" a pose; a caixa guardada no renderer pode não ter nada a ver com o
        /// que é desenhado.
        /// </summary>
        static Bounds RealBounds(GameObject go)
        {
            bool any = false;
            var total = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                Bounds b;
                if (r is SkinnedMeshRenderer s)
                {
                    if (s.sharedMesh == null) continue;
                    s.BakeMesh(Baked, true);
                    b = WorldBounds(s.transform, Baked.bounds);
                }
                else b = r.bounds;
                if (!any) { total = b; any = true; }
                else total.Encapsulate(b);
            }
            return total;
        }

        static Bounds WorldBounds(Transform t, Bounds local)
        {
            var b = new Bounds(t.TransformPoint(local.center), Vector3.zero);
            var e = local.extents;
            for (int i = 0; i < 8; i++)
                b.Encapsulate(t.TransformPoint(local.center + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z)));
            return b;
        }

        /// <summary>
        /// Caixa de recorte de cada malha com esqueleto = a pose real, com folga para a
        /// animação: com a caixa velha do arquivo, o Unity descartava ou mantinha o bicho pelo
        /// tamanho errado.
        /// </summary>
        static void FitCullingBounds(GameObject go)
        {
            foreach (var s in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (s.sharedMesh == null) continue;
                s.BakeMesh(Baked, true);
                var world = WorldBounds(s.transform, Baked.bounds);
                var space = s.rootBone != null ? s.rootBone : s.transform;
                var c = space.InverseTransformPoint(world.center);
                var b = new Bounds(c, Vector3.zero);
                var e = world.extents * 1.5f; // folga: a perna estica e o corpo sobe ao andar
                for (int i = 0; i < 8; i++)
                    b.Encapsulate(space.InverseTransformPoint(world.center + new Vector3(
                        (i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z)));
                s.localBounds = b;
            }
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

    /// <summary>
    /// Passada fingida para bicho sem clipe de andar: sobe e desce a cada passo e balança de
    /// leve, no ritmo do quanto o bicho andou (parado, fica parado).
    /// </summary>
    public sealed class GaitBob : MonoBehaviour
    {
        float _height, _phase, _speed;
        Vector3 _restPos;
        Quaternion _restRot;
        Vector3 _lastWorld;

        public void Init(float height)
        {
            _height = height;
            _restPos = transform.localPosition;
            _restRot = transform.localRotation;
            _lastWorld = transform.parent != null ? transform.parent.position : transform.position;
        }

        void LateUpdate()
        {
            var p = transform.parent != null ? transform.parent.position : transform.position;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float moved = new Vector2(p.x - _lastWorld.x, p.z - _lastWorld.z).magnitude;
            _lastWorld = p;
            _speed = Mathf.Lerp(_speed, moved / dt, Mathf.Clamp01(8f * dt));
            float stride = _height * 0.9f;
            _phase += moved / Mathf.Max(0.05f, stride) * Mathf.PI * 2f;
            float amp = Mathf.Clamp01(_speed / (_height * 1.5f));
            float bob = Mathf.Abs(Mathf.Sin(_phase)) * _height * 0.035f * amp;
            transform.localPosition = _restPos + Vector3.up * bob;
            transform.localRotation = _restRot * Quaternion.Euler(Mathf.Sin(_phase * 2f) * 2.5f * amp, 0f,
                Mathf.Sin(_phase) * 3f * amp);
        }
    }
}
