using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Câmera RTS: pan com WASD/setas ou arrastando com o botão do meio, zoom no scroll.
    /// O "rig" é um pivô no chão; a câmera fica pendurada nele, inclinada.
    /// </summary>
    public class CameraRigDriver
    {
        const float PanSpeed = 12f;
        const float DragPanFactor = 0.05f; // pixels -> unidades de mundo, escala com o zoom
        const float ZoomStep = 2.5f;
        const float MinDist = 8f;
        // Inclinação acompanha o zoom (Total War, RoN): longe, olha de cima e lê o
        // tabuleiro; perto, deita e mostra a torre, o soldado e o horizonte.
        const float PitchFar = 58f;
        const float PitchNear = 36f;

        readonly Transform _rig;
        readonly Camera _cam;
        readonly Vector3 _boundsMin;
        readonly Vector3 _boundsMax;
        readonly float _maxDist;
        float _dist;
        float _targetDist;

        public Camera Camera => _cam;

        /// <param name="startDist">
        /// Distância inicial da câmera. O padrão enquadra um tabuleiro de uma lane;
        /// o Tower Wars empilha duas e precisa de mais recuo, senão nasceria mostrando
        /// só a sua metade. O teto de zoom acompanha o valor pedido.
        /// </param>
        public CameraRigDriver(Camera cam, Vector3 center, Vector3 worldSize, float startDist = 16f)
        {
            _cam = cam;
            _rig = new GameObject("CameraRig").transform;
            _rig.position = center;
            cam.transform.SetParent(_rig, false);
            _boundsMin = center - worldSize * 0.6f;
            _boundsMax = center + worldSize * 0.6f;
            _dist = _targetDist = startDist;
            _maxDist = Mathf.Max(30f, startDist * 1.25f);
            Apply();
        }

        public void Tick(IGameInput input, float dt)
        {
            float zoomScale = _dist / 16f;
            var move = new Vector3(input.PanAxis.x, 0f, input.PanAxis.y) * (PanSpeed * zoomScale * dt);
            move += new Vector3(-input.DragPanDelta.x, 0f, -input.DragPanDelta.y) * (DragPanFactor * zoomScale);

            var p = _rig.position + move;
            p.x = Mathf.Clamp(p.x, _boundsMin.x, _boundsMax.x);
            p.z = Mathf.Clamp(p.z, _boundsMin.z, _boundsMax.z);
            _rig.position = p;

            _targetDist = Mathf.Clamp(_targetDist - input.ZoomDelta * ZoomStep, MinDist, _maxDist);
            _dist = Mathf.Lerp(_dist, _targetDist, 10f * dt);
            Apply();
        }

        void Apply()
        {
            float t = Mathf.InverseLerp(MinDist, _maxDist, _dist);
            float pitch = Mathf.Lerp(PitchNear, PitchFar, Mathf.Sqrt(t)); // deita só no zoom bem perto
            var rot = Quaternion.Euler(pitch, 0f, 0f);
            _cam.transform.localRotation = rot;
            _cam.transform.localPosition = rot * new Vector3(0f, 0f, -_dist) + Juice.ShakeOffset;
        }
    }
}
