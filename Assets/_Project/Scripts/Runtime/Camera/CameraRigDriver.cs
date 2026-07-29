using UnityEngine;

namespace FrontierTD
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
        const float MaxDist = 30f;
        const float Pitch = 55f;

        readonly Transform _rig;
        readonly Camera _cam;
        readonly Vector3 _boundsMin;
        readonly Vector3 _boundsMax;
        float _dist = 16f;
        float _targetDist = 16f;

        public Camera Camera => _cam;

        public CameraRigDriver(Camera cam, Vector3 center, Vector3 worldSize)
        {
            _cam = cam;
            _rig = new GameObject("CameraRig").transform;
            _rig.position = center;
            cam.transform.SetParent(_rig, false);
            _boundsMin = center - worldSize * 0.6f;
            _boundsMax = center + worldSize * 0.6f;
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

            _targetDist = Mathf.Clamp(_targetDist - input.ZoomDelta * ZoomStep, MinDist, MaxDist);
            _dist = Mathf.Lerp(_dist, _targetDist, 10f * dt);
            Apply();
        }

        void Apply()
        {
            var rot = Quaternion.Euler(Pitch, 0f, 0f);
            _cam.transform.localRotation = rot;
            _cam.transform.localPosition = rot * new Vector3(0f, 0f, -_dist);
        }
    }
}
