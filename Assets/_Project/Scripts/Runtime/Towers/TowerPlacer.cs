using UnityEngine;

namespace FrontierTD
{
    /// <summary>
    /// Preview (fantasma verde/vermelho) + colocação de torres.
    /// O clique vira célula via interseção raio-plano — sem physics, sem colliders.
    /// </summary>
    public class TowerPlacer
    {
        static readonly Color ValidColor = new Color(0.2f, 0.9f, 0.3f);
        static readonly Color InvalidColor = new Color(0.95f, 0.25f, 0.2f);
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
            _mpb.SetColor(MaterialFactory.ColorProperty, valid ? ValidColor : InvalidColor);
            _ghostRenderer.SetPropertyBlock(_mpb);

            if (valid && input.PlacePressed)
                _gc.PlaceTower(cell);
        }

        public void Hide() => _ghost.gameObject.SetActive(false);
    }
}
