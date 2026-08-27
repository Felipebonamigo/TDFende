using UnityEngine;

namespace TDFende
{
    /// <summary>Implementação desktop: mouse + teclado (WASD/setas, scroll, botão do meio).</summary>
    public class DesktopInput : IGameInput
    {
        Vector2 _lastMouse;

        public Vector2 PanAxis { get; private set; }
        public Vector2 DragPanDelta { get; private set; }
        public float ZoomDelta { get; private set; }
        public Vector2 PointerPos { get; private set; }
        public bool PlacePressed { get; private set; }
        public bool UpgradePressed { get; private set; }
        public bool CallWavePressed { get; private set; }
        public bool RestartPressed { get; private set; }

        public void Tick()
        {
            PointerPos = Input.mousePosition;
            PanAxis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            ZoomDelta = Input.mouseScrollDelta.y;
            DragPanDelta = Input.GetMouseButton(2) ? PointerPos - _lastMouse : Vector2.zero;
            _lastMouse = PointerPos;
            PlacePressed = Input.GetMouseButtonDown(0);
            UpgradePressed = Input.GetMouseButtonDown(1);
            CallWavePressed = Input.GetKeyDown(KeyCode.Space);
            RestartPressed = Input.GetKeyDown(KeyCode.R);
        }
    }
}
