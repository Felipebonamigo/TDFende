using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Preview (fantasma verde/vermelho) + colocação de torres.
    /// O clique vira célula via interseção raio-plano — sem physics, sem colliders.
    /// </summary>
    public class TowerPlacer
    {
        static readonly Color ValidColor = Palette.GhostValid;
        static readonly Color InvalidColor = Palette.GhostInvalid;
        static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

        readonly GameController _gc;
        readonly Transform _ghost;
        readonly Renderer _ghostRenderer;
        readonly MaterialPropertyBlock _mpb = new MaterialPropertyBlock();

        public TowerPlacer(GameController gc)
        {
            _gc = gc;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "GhostTorre";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(0.95f, 0.1f, 0.95f);
            _ghost = go.transform;
            _ghostRenderer = go.GetComponent<Renderer>();
            _ghostRenderer.sharedMaterial = MaterialFactory.Get(Color.white);
            go.SetActive(false);
        }

        public void Tick(IGameInput input, Camera cam)
        {
            var ray = cam.ScreenPointToRay(input.PointerPos);
            if (!GroundPlane.Raycast(ray, out float dist))
            {
                Hide();
                return;
            }

            var cell = _gc.Map.WorldToCell(ray.GetPoint(dist));
            if (!_gc.Map.InBounds(cell.x, cell.y))
            {
                Hide();
                return;
            }

            bool valid = _gc.CanPlaceTower(cell);
            _ghost.gameObject.SetActive(true);
            _ghost.position = _gc.Map.CellToWorld(cell) + Vector3.up * 0.05f;
            // pulsa devagar: o fantasma não se confunde com o chão
            var c = valid ? ValidColor : InvalidColor;
            _mpb.SetColor(MaterialFactory.ColorProperty,
                c * (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f)));
            _ghostRenderer.SetPropertyBlock(_mpb);

            if (valid && input.PlacePressed)
                _gc.PlaceTower(cell);
        }

        public void Hide() => _ghost.gameObject.SetActive(false);
    }
}
