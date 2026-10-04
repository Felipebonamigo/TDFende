using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TDFende
{
    /// <summary>
    /// Ponte entre a arte pura (ModelLib, ProcTex) e o motor: vira ModelDef em
    /// GameObject com Mesh e Material de verdade. Tudo em cache — cada textura é
    /// gerada uma vez, cada malha uma vez, cada material uma vez por cor de time.
    ///
    /// Porta de saída para asset de verdade: se existir um prefab em
    /// Resources/TDFende/&lt;nome do modelo&gt; ou Resources/TDFende/Torres/&lt;nome&gt;
    /// (ex.: Resources/TDFende/Torres/Torre_Canhao), ele entra no lugar do modelo procedural.
    /// Prefab sem nenhuma peça com nome conhecido substitui o modelo inteiro. Prefab com
    /// peças nomeadas (Turret, Barrel, Shaft, Top, Flag, LegL...) é híbrido: troca só essas,
    /// e as que ele não trouxe saem do código — a torre do Meshy traz Shaft e Top (o corpo de
    /// pedra) e continua com a torreta, o cano, a base e os enfeites de nível daqui.
    /// Assim dá para trocar peça por peça sem mexer em código.
    /// </summary>
    public static class ArtFactory
    {
        static readonly Dictionary<ArtMat, Texture2D> Albedo = new Dictionary<ArtMat, Texture2D>();
        static readonly Dictionary<ArtMat, Texture2D> Normal = new Dictionary<ArtMat, Texture2D>();
        /// <summary>Materiais que acharam textura fotográfica em Resources (escala própria).</summary>
        static readonly Dictionary<ArtMat, float> ExternalTile = new Dictionary<ArtMat, float>();
        const string TexturePath = "TDFende/Textures/";
        const string TowerPath = "TDFende/Torres/";
        /// <summary>Material do corpo de cada modelo baixado (null: não tem textura própria).</summary>
        static readonly Dictionary<string, Material> BodyMats = new Dictionary<string, Material>();
        static readonly Dictionary<(ArtMat, Color), Material> Mats = new Dictionary<(ArtMat, Color), Material>();
        static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();

        static Shader _lit;
        static bool _urp;
        static int _baseColor, _baseMap, _smoothness, _emission;

        public static int EmissionProperty
        {
            get
            {
                EnsureShader();
                return _emission;
            }
        }

        static void EnsureShader()
        {
            if (_lit != null) return;
            _urp = GraphicsSettings.currentRenderPipeline != null;
            _lit = Shader.Find(_urp ? "Universal Render Pipeline/Lit" : "Standard");
            _baseColor = Shader.PropertyToID(_urp ? "_BaseColor" : "_Color");
            _baseMap = Shader.PropertyToID(_urp ? "_BaseMap" : "_MainTex");
            _smoothness = Shader.PropertyToID(_urp ? "_Smoothness" : "_Glossiness");
            _emission = Shader.PropertyToID("_EmissionColor");
        }

        // ------------------------------------------------------------- texturas

        /// <summary>
        /// Gera TODAS as texturas de uma vez, em paralelo, antes de montar o mundo.
        /// A geração é C# puro (ProcTex), então roda nos outros núcleos; só a subida
        /// para a GPU volta para a thread principal. Sem isto, cada material novo
        /// travaria um frame na primeira vez que aparecesse.
        /// </summary>
        public static void Preload()
        {
            var all = (ArtMat[])System.Enum.GetValues(typeof(ArtMat));
            var t0 = Time.realtimeSinceStartup;
            // foto primeiro (Resources só roda na thread principal); o que faltar é gerado
            int photos = 0;
            foreach (var m in all)
                if (!Albedo.ContainsKey(m) && TryLoadExternal(m)) photos++;

            var pending = new List<ArtMat>();
            foreach (var m in all)
                if (!Albedo.ContainsKey(m)) pending.Add(m);
            if (photos == 0 && pending.Count == 0) return; // já carregado (segunda partida)

            var data = new TexData[pending.Count];
            System.Threading.Tasks.Parallel.For(0, pending.Count, i => data[i] = ProcTex.Generate(pending[i]));
            for (int i = 0; i < pending.Count; i++) Upload(pending[i], data[i]);
            Debug.Log($"[TDFende] {photos} texturas fotográficas + {pending.Count} procedurais em " +
                      $"{(Time.realtimeSinceStartup - t0) * 1000f:0} ms");
        }

        static void EnsureTextures(ArtMat m)
        {
            // o time só muda a cor; a textura de pano é uma só
            if (Albedo.ContainsKey(m)) return;
            if (TryLoadExternal(m)) return;
            Upload(m, ProcTex.Generate(m));
        }

        /// <summary>
        /// Carrega a foto do material (JPG guardado como .bytes, para o importador do Unity
        /// não mexer nele). A normal entra como textura LINEAR e já na convenção do Unity
        /// (OpenGL, Y para cima) — o script que preparou os arquivos virou o verde das que
        /// vieram no padrão DirectX. JPG não tem alfa (= 1), o que serve tanto para o
        /// caminho RGB quanto para o "AG" do UnpackNormal, igual às normais procedurais.
        /// </summary>
        /// <summary>
        /// Casa a cor MÉDIA da foto com a cor do material no MatSpec. Foto baixada vem com
        /// a luz e o tom de quem fotografou; sem isto, telha de um site e pedra de outro
        /// não combinam na mesma cena. Mantém o detalhe, troca só o tom médio. Na foto que
        /// já veio preparada, a média já bate e nada muda.
        /// </summary>
        static void GradeToPalette(Texture2D tex, Color target)
        {
            var px = tex.GetPixels32();
            double r = 0, g = 0, b = 0;
            for (int i = 0; i < px.Length; i++) { r += px[i].r; g += px[i].g; b += px[i].b; }
            double n = System.Math.Max(1, px.Length) * 255.0;
            float kr = target.r / (float)System.Math.Max(0.02, r / n);
            float kg = target.g / (float)System.Math.Max(0.02, g / n);
            float kb = target.b / (float)System.Math.Max(0.02, b / n);
            if (Mathf.Abs(kr - 1f) < 0.02f && Mathf.Abs(kg - 1f) < 0.02f && Mathf.Abs(kb - 1f) < 0.02f)
            {
                tex.Apply(true, true);
                return;
            }
            for (int i = 0; i < px.Length; i++)
            {
                px[i].r = (byte)Mathf.Min(255f, px[i].r * kr);
                px[i].g = (byte)Mathf.Min(255f, px[i].g * kg);
                px[i].b = (byte)Mathf.Min(255f, px[i].b * kb);
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
        }

        static bool TryLoadExternal(ArtMat m)
        {
            var ext = MatSpec.External(m);
            if (ext == null) return false;
            var albedoBytes = Resources.Load<TextAsset>(TexturePath + ext.Value.albedo);
            var normalBytes = Resources.Load<TextAsset>(TexturePath + ext.Value.normal);
            if (albedoBytes == null || normalBytes == null) return false;

            var albedo = new Texture2D(2, 2, TextureFormat.RGBA32, true, false) { name = $"{m}_Albedo" };
            var normal = new Texture2D(2, 2, TextureFormat.RGBA32, true, true) { name = $"{m}_Normal" };
            if (!albedo.LoadImage(albedoBytes.bytes, false) || !normal.LoadImage(normalBytes.bytes, true))
            {
                Debug.LogWarning($"[TDFende] textura de {m} não abriu; usando a procedural");
                return false;
            }
            GradeToPalette(albedo, MatSpec.Of(m).Base);
            foreach (var t in new[] { albedo, normal })
            {
                t.wrapMode = TextureWrapMode.Repeat;
                t.filterMode = FilterMode.Trilinear;
                t.anisoLevel = 8;
            }
            Albedo[m] = albedo;
            Normal[m] = normal;
            ExternalTile[m] = ext.Value.unitsPerTile;
            return true;
        }

        static void Upload(ArtMat m, TexData data)
        {

            var albedo = new Texture2D(data.Size, data.Size, TextureFormat.RGBA32, true, false)
            {
                name = $"{m}_Albedo", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4
            };
            albedo.SetPixels32(data.Albedo);
            albedo.Apply(true, true); // true: libera a cópia da CPU, a textura fica só na GPU

            var normal = new Texture2D(data.Size, data.Size, TextureFormat.RGBA32, true, true)
            {
                name = $"{m}_Normal", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4
            };
            normal.SetPixels32(data.Normal);
            normal.Apply(true, true);

            Albedo[m] = albedo;
            Normal[m] = normal;
        }

        // ------------------------------------------------------------ materiais

        /// <summary>Material PBR de um ArtMat. <paramref name="team"/> só vale para ClothTeam.</summary>
        public static Material Mat(ArtMat m, Color team)
        {
            EnsureShader();
            var key = (m, m == ArtMat.ClothTeam ? team : Color.clear);
            if (Mats.TryGetValue(key, out var mat) && mat != null) return mat;

            EnsureTextures(m);
            var spec = MatSpec.Of(m);
            mat = new Material(_lit) { name = m.ToString(), enableInstancing = true };
            mat.SetTexture(_baseMap, Albedo[m]);
            float tile = ExternalTile.TryGetValue(m, out var et) ? et : spec.UnitsPerTile;
            mat.mainTextureScale = Vector2.one / tile;
            mat.SetColor(_baseColor, m == ArtMat.ClothTeam ? team : Color.white);
            mat.SetTexture("_BumpMap", Normal[m]);
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat(_smoothness, spec.Smoothness);
            mat.SetFloat("_Metallic", spec.Metallic);
            // emissão ligada em todos: é por ela que o inimigo pisca ao levar tiro e
            // fica gelado sob atrito, sem apagar a cor de time do tabardo
            mat.EnableKeyword("_EMISSION");
            mat.SetColor(_emission, spec.Emission);
            // brasa: a emissão segue a própria cor (só a rachadura clara acende, o carvão não)
            if (m == ArtMat.Ember) mat.SetTexture("_EmissionMap", Albedo[m]);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            Mats[key] = mat;
            return mat;
        }

        // --------------------------------------------------------------- malhas

        /// <summary>Converte um MeshBuilder em Mesh, com os vértices relativos a <paramref name="pivot"/>.</summary>
        public static Mesh ToMesh(MeshBuilder mb, Vector3 pivot, string name)
        {
            var mesh = new Mesh { name = name };
            if (mb.Vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            var verts = new List<Vector3>(mb.Vertices.Count);
            for (int i = 0; i < mb.Vertices.Count; i++) verts.Add(mb.Vertices[i] - pivot);
            mesh.SetVertices(verts);
            mesh.SetNormals(mb.Normals);
            mesh.SetUVs(0, mb.Uvs);
            mesh.subMeshCount = mb.Triangles.Count;
            for (int s = 0; s < mb.Triangles.Count; s++) mesh.SetTriangles(mb.Triangles[s], s, false);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents(); // normal map precisa de tangente
            mesh.UploadMeshData(true);
            return mesh;
        }

        static Mesh PartMesh(ModelDef def, ModelPart part)
        {
            string key = def.Name + "/" + part.Name;
            if (!Meshes.TryGetValue(key, out var mesh) || mesh == null)
                Meshes[key] = mesh = ToMesh(part.Mesh, part.Pivot, key);
            return mesh;
        }

        static Material[] MatsFor(MeshBuilder mb, Color team)
        {
            var arr = new Material[mb.Materials.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = Mat(mb.Materials[i], team);
            return arr;
        }

        // -------------------------------------------------------------- objetos

        /// <summary>
        /// Estágio cujo modelo baixado existe para a torre neste nível (Torres/&lt;torre&gt;_&lt;estágio&gt;),
        /// ou 0 se não houver — aí vale o modelo único ou o do código.
        /// </summary>
        public static int StageFor(ModelDef def, int level)
        {
            int stage = TowerStages.ForLevel(level);
            string key = TowerStages.ModelName(def.Name, stage);
            if (!StageModels.TryGetValue(key, out bool has))
                StageModels[key] = has = Resources.Load<GameObject>(TowerPath + key) != null;
            return has ? stage : 0;
        }

        static readonly Dictionary<string, bool> StageModels = new Dictionary<string, bool>();

        /// <summary>
        /// Instancia um modelo (procedural, ou o prefab que o substitui) e devolve o rig.
        /// <paramref name="stage"/> &gt; 0 pede o modelo daquele estágio da torre (ver <see cref="StageFor"/>).
        /// </summary>
        public static ModelRig Spawn(ModelDef def, Color team, Transform parent, string name = null, int stage = 0)
        {
            var root = new GameObject(name ?? def.Name);
            if (parent != null) root.transform.SetParent(parent, false);
            var parts = new Dictionary<string, Transform>();

            // bicho de verdade (modelo baixado em Resources/TDFende/Bichos), se houver
            if (def.Name != null && def.Name.StartsWith("Inimigo_"))
            {
                var animal = AnimalLoader.TrySpawn(def, root.transform, team, out var animalClips);
                if (animal != null)
                {
                    var animalRig = root.AddComponent<ModelRig>();
                    animalRig.Bind(def, parts);
                    animalRig.UseClips(animalClips);
                    return animalRig;
                }
            }

            // personagem com esqueleto e animação de verdade (Mixamo), se houver
            if (def.Anim == AnimKind.Walker)
            {
                var character = CharacterLoader.TrySpawn(def, root.transform, team, out var anim);
                if (character != null)
                {
                    var charRig = root.AddComponent<ModelRig>();
                    charRig.Bind(def, parts);
                    charRig.UseClips(anim);
                    return charRig;
                }
            }

            GameObject prefab = null;
            if (stage > 0) prefab = Resources.Load<GameObject>(TowerPath + TowerStages.ModelName(def.Name, stage));
            if (prefab == null)
            {
                stage = 0;
                prefab = Resources.Load<GameObject>("TDFende/" + def.Name);
                if (prefab == null) prefab = Resources.Load<GameObject>(TowerPath + def.Name);
            }
            var fromPrefab = new HashSet<string>();
            if (prefab != null)
            {
                var inst = Object.Instantiate(prefab, root.transform, false);
                foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                    if (def.Find(t.name) != null && fromPrefab.Add(t.name)) parts[t.name] = t;
                SkinBody(prefab.name, inst); // textura pelo nome do arquivo: cada estágio tem a sua
            }
            if (prefab == null || fromPrefab.Count > 0)
            {
                foreach (var part in def.Parts)
                {
                    if (parts.ContainsKey(part.Name)) continue;
                    // enfeite de nível no fuste do modelo baixado: foi medido para o fuste do
                    // código (raio, altura) e sobraria dentro da parede ou flutuando quando a
                    // torre cresce. Os do topo e da base servem como estão.
                    if (part.Parent == ModelLib.Shaft && fromPrefab.Contains(ModelLib.Shaft)
                        && part.Name.StartsWith("Lv")) continue;
                    // modelo de estágio: a evolução já está no modelo (o seguinte é outro), e os
                    // enfeites do topo, medidos para o topo do código, cobririam o dele
                    if (stage > 0 && part.Parent == ModelLib.Top && part.Name.StartsWith("Lv")) continue;
                    var go = new GameObject(part.Name);
                    Transform parentT = root.transform;
                    var parentPivot = Vector3.zero;
                    if (part.Parent != null && parts.TryGetValue(part.Parent, out var pt))
                    {
                        parentT = pt;
                        parentPivot = def.Find(part.Parent).Pivot;
                    }
                    go.transform.SetParent(parentT, false);
                    go.transform.localPosition = part.Pivot - parentPivot;
                    if (part.Mesh.VertexCount > 0)
                    {
                        go.AddComponent<MeshFilter>().sharedMesh = PartMesh(def, part);
                        go.AddComponent<MeshRenderer>().sharedMaterials = MatsFor(part.Mesh, team);
                    }
                    if (part.StartHidden) go.SetActive(false);
                    parts[part.Name] = go.transform;
                }
            }

            var rig = root.AddComponent<ModelRig>();
            rig.Bind(def, parts);
            rig.Stage = stage;
            if (stage > 0) rig.GrowFromLevel = TowerStages.FirstLevel(stage);
            // fuste do modelo baixado tem a altura dele: o topo sobe na medida certa ao crescer
            if (fromPrefab.Contains(ModelLib.Shaft) && parts.TryGetValue(ModelLib.Top, out var topT))
            {
                var r = root.transform;
                rig.ShaftHeight = r.InverseTransformPoint(topT.position).y
                                  - r.InverseTransformPoint(parts[ModelLib.Shaft].position).y;
            }
            return rig;
        }

        /// <summary>
        /// Veste o modelo baixado com a textura dele, se houver em Resources/TDFende/Torres/Textures
        /// (&lt;nome&gt;_cor e &lt;nome&gt;_normal, JPG em .bytes — o Unity não recomprime, como as
        /// fotos de TDFende/Textures). Material do mesmo shader dos procedurais, com emissão
        /// ligada: é por ela que passa o brilho de <see cref="ModelRig.SetGlow"/>.
        /// </summary>
        static void SkinBody(string name, GameObject inst)
        {
            if (!BodyMats.TryGetValue(name, out var mat))
            {
                mat = null;
                var cor = Resources.Load<TextAsset>(TowerPath + "Textures/" + name + "_cor");
                var albedo = cor != null ? LoadBodyTex(cor, name + "_cor", false) : null;
                if (albedo != null)
                {
                    EnsureShader();
                    mat = new Material(_lit) { name = name, enableInstancing = true };
                    mat.SetTexture(_baseMap, albedo);
                    mat.SetColor(_baseColor, Color.white);
                    var nrm = Resources.Load<TextAsset>(TowerPath + "Textures/" + name + "_normal");
                    var normal = nrm != null ? LoadBodyTex(nrm, name + "_normal", true) : null;
                    if (normal != null)
                    {
                        mat.SetTexture("_BumpMap", normal);
                        mat.SetFloat("_BumpScale", 1f);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                    mat.SetFloat(_smoothness, 0.15f); // pedra velha: quase fosca
                    mat.SetFloat("_Metallic", 0f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor(_emission, Color.black);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
                BodyMats[name] = mat;
            }
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                if (mat != null)
                {
                    var arr = r.sharedMaterials;
                    for (int i = 0; i < arr.Length; i++) arr[i] = mat;
                    r.sharedMaterials = arr;
                }
                r.shadowCastingMode = ShadowCastingMode.On;
            }
        }

        static Texture2D LoadBodyTex(TextAsset bytes, string texName, bool linear)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear) { name = texName };
            if (!tex.LoadImage(bytes.bytes, false))
            {
                Debug.LogWarning($"[TDFende] textura {texName} não abriu");
                return null;
            }
            tex.wrapMode = TextureWrapMode.Clamp; // atlas: repetir só traria a borda do outro lado
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 8;
            tex.Apply(true, true);
            return tex;
        }

        /// <summary>Objeto estático de uma malha só (chão, cenário, mureta), sem rig.</summary>
        public static GameObject Static(string name, MeshBuilder mb, Transform parent, Color team,
            bool castShadows = true)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = ToMesh(mb, Vector3.zero, name);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = MatsFor(mb, team);
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        // ---------------------------------------------------- overlays sem luz

        static Material _overlay;

        /// <summary>
        /// Material transparente sem luz, cor por vértice (o shader da fronteira).
        /// Serve para barra de vida, fantasma de construção, anel de alcance e grade.
        /// </summary>
        public static Material Overlay
        {
            get
            {
                if (_overlay != null) return _overlay;
                var shader = Shader.Find("TDFende/TerritoryOverlay");
                _overlay = shader != null ? new Material(shader) : MaterialFactory.Get(Color.white);
                _overlay.name = "Overlay";
                return _overlay;
            }
        }
    }
}
