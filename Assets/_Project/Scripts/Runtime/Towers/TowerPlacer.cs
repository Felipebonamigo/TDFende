using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Fantasma (célula verde/vermelha + anel de alcance) + colocação de torres.
    /// O clique vira célula via interseção raio-plano — sem physics, sem colliders.
    /// </summary>
    public class TowerPlacer
    {
        static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

        readonly GameController _gc;
        readonly PlacementGhost _ghost = new PlacementGhost();

        public TowerPlacer(GameController gc) => _gc = gc;

        public void Tick(IGameInput input, Camera cam)
        {
            if (DebugHud.PointerOverHud(input.PointerPos))
            {
                Hide();
                return;
            }

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

            // botão direito (ou X/Delete) numa torre: vende
            if (input.SellPressed && _gc.SellValueAt(cell) >= 0)
            {
                _gc.SellTower(cell);
                return;
            }

            bool valid = _gc.CanPlaceTower(cell);
            _ghost.Show(_gc.Map.CellToWorld(cell), valid ? Palette.GhostValid : Palette.GhostInvalid,
                GameConfig.TowerRange);

            if (valid && input.PlacePressed)
                _gc.PlaceTower(cell);
        }

        public void Hide() => _ghost.Hide();
    }
}
