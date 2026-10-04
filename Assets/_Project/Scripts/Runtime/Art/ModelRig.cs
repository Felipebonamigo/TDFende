using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Movimento de um modelo: torre mira, dá coice e cresce com o nível; inimigo
    /// anda, galopa, rola ou plana. Sem Update próprio — quem desenha (LaneView,
    /// Tower, Enemy) chama os métodos no ritmo dele, então objeto parado não custa nada.
    /// </summary>
    public class ModelRig : MonoBehaviour
    {
        public ModelDef Def { get; private set; }

        Transform _shaft, _top, _turret, _barrel, _flag, _body, _wing, _rotor, _wingL, _wingR;
        float _rotorAngle, _rotorBoost;
        float _speedAvg; // velocidade suavizada: a perna não "desliga" num frame sem passo
        Animation _clips; // personagem baixado: animação de verdade no lugar da perna procedural
        string _clipState;
        Transform[] _legs = new Transform[0];
        // enfeites de nível ("Lv{n}_{pai}"): aparecem quando a torre chega ao nível n
        readonly List<(Transform T, int Level)> _tiers = new List<(Transform, int)>();
        // chamas extras de nível (torre de Fogo): peça que carrega e ponto local a ela
        readonly List<(Transform T, Vector3 Local, int Level)> _firePoints = new List<(Transform, Vector3, int)>();
        Transform _armL, _armR;
        readonly List<Transform> _wheels = new List<Transform>();
        Vector3 _topRest, _barrelRest, _bodyRest;
        Vector3 _barrelPivot, _turretPivot;

        Renderer[] _renderers;
        MaterialPropertyBlock _mpb;
        bool _glowing;

        float _phase, _wheelAngle, _recoil, _time;
        int _level = 1;
        HealthBar _bar;

        /// <summary>
        /// Altura do fuste: quanto o topo sobe por "unidade" de crescimento. Vem do ModelDef; o
        /// ArtFactory troca pela medida do modelo baixado quando o fuste é dele.
        /// </summary>
        internal float ShaftHeight;

        internal void Bind(ModelDef def, Dictionary<string, Transform> parts)
        {
            Def = def;
            ShaftHeight = def.ShaftHeight;
            Transform Get(string n) => parts.TryGetValue(n, out var t) ? t : null;
            _shaft = Get(ModelLib.Shaft);
            _top = Get(ModelLib.Top);
            _turret = Get(ModelLib.Turret);
            _barrel = Get(ModelLib.Barrel);
            _flag = Get(ModelLib.Flag);
            _body = Get(ModelLib.Body);
            _wing = Get(ModelLib.Wing);
            _wingL = Get("WingL");
            _wingR = Get("WingR");
            _rotor = Get(ModelLib.Rotor);
            _armL = Get(ModelLib.ArmL);
            _armR = Get(ModelLib.ArmR);

            var legs = new List<Transform>();
            foreach (var n in new[] { ModelLib.LegL, ModelLib.LegR, "LegFL", "LegFR", "LegBL", "LegBR" })
                if (Get(n) != null) legs.Add(Get(n));
            _legs = legs.ToArray();
            for (int i = 0; i < 8; i++)
                if (Get("Wheel" + i) != null) _wheels.Add(Get("Wheel" + i));

            if (_top != null) _topRest = _top.localPosition;
            if (_barrel != null) _barrelRest = _barrel.localPosition;
            if (_body != null) _bodyRest = _body.localPosition;
            _barrelPivot = def.Find(ModelLib.Barrel)?.Pivot ?? Vector3.zero;
            _turretPivot = def.Find(ModelLib.Turret)?.Pivot ?? Vector3.zero;
            foreach (var kv in parts)
                if (kv.Key.StartsWith("Lv") && kv.Key.Length > 2 && char.IsDigit(kv.Key[2]))
                    _tiers.Add((kv.Value, kv.Key[2] - '0'));
            foreach (var (pos, lvl, parent) in def.FirePoints)
            {
                var t = Get(parent);
                if (t == null) continue;
                var parentPivot = def.Find(parent)?.Pivot ?? Vector3.zero;
                _firePoints.Add((t, pos - parentPivot, lvl));
            }
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        /// <summary>Quantas chamas extras a torre tem (acesas ou não).</summary>
        public int FirePointCount => _firePoints.Count;

        /// <summary>Posição de mundo da chama extra <paramref name="i"/>; false se o nível ainda não a acendeu.</summary>
        public bool TryGetFirePoint(int i, out Vector3 world)
        {
            var (t, local, lvl) = _firePoints[i];
            world = t.TransformPoint(local);
            return _level >= lvl && t.gameObject.activeInHierarchy;
        }

        // ================================================================ torre

        /// <summary>Nível sobe o fuste (a torre cresce) e hasteia a bandeira do time a partir do 2.</summary>
        public void SetLevel(int level)
        {
            if (level == _level) return;
            _level = level;
            float grow = 0.14f * (level - 1);
            if (_shaft != null) _shaft.localScale = new Vector3(1f, 1f + grow, 1f);
            if (_top != null) _top.localPosition = _topRest + Vector3.up * (ShaftHeight * grow);
            if (_flag != null) _flag.gameObject.SetActive(level >= 2);
            // cada nível acende o seu conjunto de enfeites (e os de baixo continuam)
            foreach (var (t, lvl) in _tiers) t.gameObject.SetActive(level >= lvl);
        }

        /// <summary>Gira a torreta para o alvo (só no plano, como reparo de verdade).</summary>
        public void AimAt(Vector3 world, float dt, float speed = 10f)
        {
            if (_turret == null) return;
            var look = world - _turret.position;
            look.y = 0f;
            if (look.sqrMagnitude < 0.0001f) return;
            _turret.rotation = Quaternion.Slerp(_turret.rotation, Quaternion.LookRotation(look), speed * dt);
        }

        /// <summary>Coice do tiro: o cano recua e volta sozinho em <see cref="TickTower"/>.</summary>
        public void Kick()
        {
            _recoil = 1f;
            _rotorBoost = 1f;
        }

        public void TickTower(float dt)
        {
            if (_rotor != null)
            {
                // pás giram sempre; a rajada acelera e a inércia devolve ao giro de cruzeiro
                _rotorBoost = Mathf.Max(0f, _rotorBoost - 0.8f * dt);
                _rotorAngle += (60f + 540f * _rotorBoost) * dt;
                _rotor.localRotation = Quaternion.Euler(0f, 0f, -_rotorAngle);
            }
            if (_barrel == null) return;
            _recoil = Mathf.Max(0f, _recoil - 5f * dt);
            _barrel.localPosition = _barrelRest - Vector3.forward * (_recoil * _recoil * 0.06f);
        }

        /// <summary>Onde a chama do braseiro nasce (torre de Fogo), em mundo. Nulo se não houver.</summary>
        public Vector3? BrazierWorld =>
            Def.Brazier.HasValue && _top != null
                ? _top.TransformPoint(Def.Brazier.Value - Def.Find(ModelLib.Top).Pivot)
                : (Vector3?)null;

        /// <summary>Boca do cano em coordenadas de mundo (onde nasce o clarão e o tiro).</summary>
        public Vector3 MuzzleWorld
        {
            get
            {
                if (_barrel != null) return _barrel.TransformPoint(Def.Muzzle - _barrelPivot);
                if (_turret != null) return _turret.TransformPoint(Def.Muzzle - _turretPivot);
                return transform.TransformPoint(Def.Muzzle);
            }
        }

        // ============================================================= inimigo

        /// <summary>
        /// Posiciona (coordenada local ao pai), vira para onde andou e anima pelo
        /// deslocamento: perna acompanha o chão percorrido, não o relógio — parado não
        /// marcha no lugar, e o lento anda devagar de verdade.
        /// </summary>
        public void Follow(Vector3 localPos, float dt, bool snap = false)
        {
            var delta = snap ? Vector3.zero : localPos - transform.localPosition;
            transform.localPosition = localPos;
            delta.y = 0f;
            if (delta.sqrMagnitude > 1e-7f)
                transform.localRotation = Quaternion.Slerp(transform.localRotation,
                    Quaternion.LookRotation(delta), Mathf.Clamp01(10f * dt));
            Animate(delta.magnitude, dt);
        }

        /// <summary>Personagem com clipes (CharacterLoader): Walk, Run, Idle.</summary>
        internal void UseClips(Animation anim)
        {
            _clips = anim;
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        /// <summary>
        /// Escolhe o clipe pela velocidade e acelera/desacelera a animação para o pé não
        /// patinar. Referência: um passo de caminhada cobre ~0,8 da altura do boneco por
        /// segundo, uma corrida ~2 alturas — na escala do jogo, o inimigo anda depressa.
        /// </summary>
        void AnimateClips(float distance, float dt)
        {
            if (dt > 0f) _speedAvg = Mathf.Lerp(_speedAvg, distance / dt, Mathf.Clamp01(8f * dt));
            float h = Mathf.Max(0.1f, Def.Height);
            string want;
            float refSpeed;
            if (_speedAvg < 0.05f && _clips.GetClip("Idle") != null) { want = "Idle"; refSpeed = 0f; }
            else if (_speedAvg > 1.3f * h && _clips.GetClip("Run") != null) { want = "Run"; refSpeed = 2.0f * h; }
            else { want = "Walk"; refSpeed = 0.8f * h; }
            if (_clips.GetClip(want) == null) return;

            if (want != _clipState)
            {
                _clips.CrossFade(want, 0.2f);
                _clipState = want;
            }
            if (refSpeed > 0f) _clips[want].speed = Mathf.Clamp(_speedAvg / refSpeed, 0.5f, 2.2f);
        }

        /// <summary>
        /// Morte com a animação do próprio modelo (bicho baixado que tem clipe "Death").
        /// False = não tem: quem chamou faz o tombo genérico.
        /// </summary>
        public bool PlayDeath()
        {
            if (_clips == null || _clips.GetClip("Death") == null) return false;
            _clips["Death"].speed = 1f;
            _clips.CrossFade("Death", 0.12f);
            _clipState = "Death";
            return true;
        }

        /// <summary>Anima por <paramref name="distance"/> percorrida neste frame.</summary>
        public void Animate(float distance, float dt)
        {
            _time += dt;
            if (_clips != null)
            {
                AnimateClips(distance, dt);
                return;
            }
            switch (Def.Anim)
            {
                case AnimKind.Walker: Walk(distance, dt); break;
                case AnimKind.Horse: Gallop(distance, dt); break;
                case AnimKind.Wheels: Roll(distance); break;
                case AnimKind.Glider: Glide(); break;
            }
        }

        void Walk(float distance, float dt)
        {
            // decide "andando" pela velocidade MÉDIA, não pelo frame: um frame com
            // deslocamento zero não pode fechar as pernas e reabrir no seguinte
            if (dt > 0f) _speedAvg = Mathf.Lerp(_speedAvg, distance / dt, Mathf.Clamp01(8f * dt));
            bool moving = _speedAvg > 0.05f;
            _phase += distance / Mathf.Max(0.05f, Def.Stride) * Mathf.PI;
            float swing = moving ? Mathf.Sin(_phase) : 0f;
            float blend = Mathf.Clamp01(12f * dt);
            SwingX(_legs.Length > 0 ? _legs[0] : null, swing * 32f, blend);
            SwingX(_legs.Length > 1 ? _legs[1] : null, -swing * 32f, blend);
            SwingX(_armL, -swing * 20f, blend);
            SwingX(_armR, swing * 10f, blend); // braço da arma balança menos
            if (_body != null)
                _body.localPosition = _bodyRest + Vector3.up * (moving ? Mathf.Abs(Mathf.Cos(_phase)) * 0.012f : 0f);
        }

        void Gallop(float distance, float dt)
        {
            _phase += distance / Mathf.Max(0.05f, Def.Stride) * Mathf.PI * 2f;
            float blend = Mathf.Clamp01(12f * dt);
            if (dt > 0f) _speedAvg = Mathf.Lerp(_speedAvg, distance / dt, Mathf.Clamp01(8f * dt));
            bool moving = _speedAvg > 0.05f;
            float a = moving ? 1f : 0f;
            // galope: patas da frente e de trás defasadas, cada par levemente desencontrado
            if (_legs.Length >= 4)
            {
                SwingX(_legs[0], a * Mathf.Sin(_phase) * 34f, blend);
                SwingX(_legs[1], a * Mathf.Sin(_phase + 0.7f) * 34f, blend);
                SwingX(_legs[2], a * Mathf.Sin(_phase + Mathf.PI) * 30f, blend);
                SwingX(_legs[3], a * Mathf.Sin(_phase + Mathf.PI + 0.7f) * 30f, blend);
            }
            if (_body != null)
            {
                _body.localPosition = _bodyRest + Vector3.up * (a * Mathf.Abs(Mathf.Sin(_phase)) * 0.02f);
                _body.localRotation = Quaternion.Euler(a * Mathf.Sin(_phase * 2f) * 2.5f, 0f, 0f);
            }
        }

        void Roll(float distance)
        {
            _wheelAngle += distance / Mathf.Max(0.02f, Def.Stride) * Mathf.Rad2Deg;
            for (int i = 0; i < _wheels.Count; i++) _wheels[i].localRotation = Quaternion.Euler(_wheelAngle, 0f, 0f);
            // torre de cerco balança pesada ao rolar
            if (_body != null) _body.localRotation = Quaternion.Euler(Mathf.Sin(_wheelAngle * 0.05f) * 1.2f, 0f, 0f);
        }

        void Glide()
        {
            if (_body != null)
                _body.localPosition = _bodyRest + Vector3.up * (Mathf.Sin(_time * 1.7f + _phase) * 0.05f);
            if (_wing != null)
                _wing.localRotation = Quaternion.Euler(Mathf.Sin(_time * 0.9f) * 2f, 0f, Mathf.Sin(_time * 1.1f) * 7f);
            // ave: bate as asas em rajadas e plana entre elas
            if (_wingL != null && _wingR != null)
            {
                float burst = Mathf.Clamp01(Mathf.Sin(_time * 0.8f + _phase) * 1.6f + 0.4f);
                float flap = Mathf.Sin(_time * 9f) * 32f * burst + 6f;
                _wingL.localRotation = Quaternion.Euler(0f, 0f, -flap);
                _wingR.localRotation = Quaternion.Euler(0f, 0f, flap);
            }
        }

        static void SwingX(Transform t, float deg, float blend)
        {
            if (t == null) return;
            t.localRotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(deg, 0f, 0f), blend);
        }

        // ============================================================ feedback

        /// <summary>
        /// Brilho emissivo por cima do material (clarão do impacto, gelo do atrito).
        /// Preto desliga e devolve o renderer ao material puro — e ao batching.
        /// </summary>
        public void SetGlow(Color c)
        {
            bool on = c.r + c.g + c.b > 0.001f;
            if (!on && !_glowing) return;
            _glowing = on;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (!on)
                {
                    _renderers[i].SetPropertyBlock(null);
                    continue;
                }
                _mpb.SetColor(ArtFactory.EmissionProperty, c);
                _renderers[i].SetPropertyBlock(_mpb);
            }
        }

        /// <summary>Barra de vida sobre a cabeça; some com vida cheia (tela limpa com muito inimigo).</summary>
        public void SetHealth(float fraction, Camera cam)
        {
            if (fraction >= 0.999f)
            {
                if (_bar != null) _bar.Hide();
                return;
            }
            if (_bar == null) _bar = new HealthBar(transform, Def.Height + 0.08f);
            _bar.Show(fraction, cam);
        }

        /// <summary>Volta ao estado de fábrica ao sair do pool: sem barra, sem brilho.</summary>
        public void ResetState()
        {
            SetGlow(Color.black);
            _bar?.Hide();
            _recoil = 0f;
            // saiu do pool depois de morrer com clipe: volta andando, não deitado
            if (_clips != null && _clipState == "Death")
            {
                _clips.Stop();
                _clipState = null;
                if (_clips.GetClip("Walk") != null) _clips.Play("Walk");
            }
        }
    }

    /// <summary>Duas tiras sem luz, viradas para a câmera: fundo escuro e preenchimento colorido.</summary>
    public class HealthBar
    {
        const float Width = 0.42f, Height = 0.05f;
        static Mesh _back, _green, _yellow, _red;

        readonly Transform _root;
        readonly Transform _fill;
        readonly MeshFilter _fillFilter;

        public HealthBar(Transform owner, float height)
        {
            EnsureMeshes();
            _root = new GameObject("Vida").transform;
            _root.SetParent(owner, false);
            _root.localPosition = Vector3.up * height;

            var back = new GameObject("Fundo");
            back.transform.SetParent(_root, false);
            back.AddComponent<MeshFilter>().sharedMesh = _back;
            Setup(back.AddComponent<MeshRenderer>());

            var fill = new GameObject("Barra");
            fill.transform.SetParent(_root, false);
            fill.transform.localPosition = new Vector3(-Width * 0.5f, 0f, -0.002f);
            _fillFilter = fill.AddComponent<MeshFilter>();
            _fillFilter.sharedMesh = _green;
            Setup(fill.AddComponent<MeshRenderer>());
            _fill = fill.transform;
        }

        static void Setup(MeshRenderer mr)
        {
            mr.sharedMaterial = ArtFactory.Overlay;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        public void Show(float fraction, Camera cam)
        {
            if (!_root.gameObject.activeSelf) _root.gameObject.SetActive(true);
            fraction = Mathf.Clamp01(fraction);
            _fill.localScale = new Vector3(Mathf.Max(0.001f, fraction), 1f, 1f);
            _fillFilter.sharedMesh = fraction > 0.5f ? _green : fraction > 0.25f ? _yellow : _red;
            if (cam != null) _root.rotation = cam.transform.rotation;
        }

        public void Hide()
        {
            if (_root.gameObject.activeSelf) _root.gameObject.SetActive(false);
        }

        static void EnsureMeshes()
        {
            if (_back != null) return;
            _back = Quad(-Width * 0.5f - 0.01f, Width * 0.5f + 0.01f, Height * 0.5f + 0.01f, new Color(0.05f, 0.05f, 0.05f, 0.6f));
            // preenchimento ancorado na esquerda: escalar X encolhe para a esquerda
            _green = Quad(0f, Width, Height * 0.5f, new Color(0.36f, 0.78f, 0.30f, 0.95f));
            _yellow = Quad(0f, Width, Height * 0.5f, new Color(0.92f, 0.74f, 0.22f, 0.95f));
            _red = Quad(0f, Width, Height * 0.5f, new Color(0.86f, 0.25f, 0.18f, 0.95f));
        }

        static Mesh Quad(float x0, float x1, float hy, Color c)
        {
            var m = new Mesh { name = "BarraVida" };
            m.SetVertices(new List<Vector3>
                { new Vector3(x0, -hy, 0f), new Vector3(x0, hy, 0f), new Vector3(x1, hy, 0f), new Vector3(x1, -hy, 0f) });
            c = Palette.ForVertex(c); // espaço Linear: cor de vértice não é convertida sozinha
            m.SetColors(new List<Color> { c, c, c, c });
            m.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
