using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Personagem de verdade (Mixamo ou qualquer FBX com esqueleto) no lugar do boneco
    /// procedural. Convenção de arquivos em Assets/Resources/TDFende/Personagens/:
    ///
    ///   Inimigo_Recruta.fbx          o modelo com pele (o nome é o do ModelDef)
    ///   Inimigo_Recruta@Walk.fbx     animação de andar (sem pele)
    ///   Inimigo_Recruta@Run.fbx      correr (opcional: usado quando o inimigo é rápido)
    ///   Inimigo_Recruta@Idle.fbx     parado (opcional)
    ///
    /// O importador (Editor/CharacterImportRules) põe tudo em Legacy e dá a cada clipe o
    /// nome do arquivo; aqui só é preciso achar os clipes pelo prefixo. Sem o arquivo, o
    /// jogo segue com o modelo procedural — nada quebra.
    /// </summary>
    public static class CharacterLoader
    {
        public const string Folder = "TDFende/Personagens/";

        static AnimationClip[] _allClips;

        /// <summary>Tenta montar o personagem. Devolve null se não houver arquivo para ele.</summary>
        public static GameObject TrySpawn(ModelDef def, Transform root, Color team, out Animation anim)
        {
            anim = null;
            var prefab = Resources.Load<GameObject>(Folder + def.Name);
            if (prefab == null) return null;

            var inst = Object.Instantiate(prefab, root, false);
            inst.name = "Personagem";

            // Mixamo vem em centímetros e cada personagem tem uma altura: normaliza pela
            // altura que o ModelDef usa, para o resto do jogo (barra de vida, mira) bater
            float h = MeasureHeight(inst);
            if (h > 0.0001f) inst.transform.localScale *= def.Height / 1.02f / h;

            anim = inst.GetComponentInChildren<Animation>();
            if (anim == null) anim = inst.AddComponent<Animation>();
            foreach (var clip in ClipsFor(def.Name))
            {
                string state = clip.name.Substring(def.Name.Length + 1); // depois do "@"
                clip.legacy = true;
                anim.AddClip(clip, state);
                if (state == "Walk" || state == "Run" || state == "Idle")
                    anim[state].wrapMode = WrapMode.Loop;
            }
            anim.cullingType = AnimationCullingType.BasedOnRenderers;
            if (anim.GetClip("Idle") != null) anim.Play("Idle");
            else if (anim.GetClip("Walk") != null) anim.Play("Walk");

            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = false;
            }

            AddTeamRing(root, team, def.Height);
            return inst;
        }

        static IEnumerable<AnimationClip> ClipsFor(string name)
        {
            if (_allClips == null) _allClips = Resources.LoadAll<AnimationClip>(Folder.TrimEnd('/'));
            string prefix = name + "@";
            foreach (var c in _allClips)
                if (c != null && c.name.StartsWith(prefix)) yield return c;
        }

        static float MeasureHeight(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return 0f;
            // pose de ligação (T-pose) medida com o objeto na origem do pai
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b.size.y;
        }

        /// <summary>
        /// Anel no chão com a cor do time. O personagem baixado não tem tabardo tingível
        /// como o procedural; sem isto, não daria para saber de quem ele é.
        /// </summary>
        static void AddTeamRing(Transform root, Color team, float height)
        {
            var ring = Overlays.MakeObject("AnelTime", Overlays.Ring(0.22f), root, new Color(team.r, team.g, team.b, 0.75f));
            float r = Mathf.Max(0.16f, height * 0.32f);
            ring.transform.localScale = new Vector3(r, 1f, r);
            ring.transform.localPosition = Vector3.up * 0.012f;
        }
    }
}
