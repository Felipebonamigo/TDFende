using UnityEngine;

namespace FrontierTD
{
    /// <summary>
    /// Grid lógico do mapa: células bloqueadas e conversão mundo &lt;-&gt; célula.
    /// O grid fica centralizado na origem do mundo, no plano Y=0.
    /// </summary>
    public class GridMap
    {
        public readonly int Width;
        public readonly int Height;
        public readonly float CellSize;

        readonly bool[] _blocked;
        readonly Vector3 _origin; // canto da célula (0,0) em coordenadas de mundo

        public GridMap(int width, int height, float cellSize)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            _blocked = new bool[width * height];
            _origin = new Vector3(-width * cellSize * 0.5f, 0f, -height * cellSize * 0.5f);
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool IsBlocked(int x, int y) => _blocked[y * Width + x];
        public bool IsBlocked(Vector2Int c) => IsBlocked(c.x, c.y);
        public bool IsWalkable(int x, int y) => InBounds(x, y) && !IsBlocked(x, y);

        public void SetBlocked(Vector2Int c, bool value) => _blocked[c.y * Width + c.x] = value;
        public void ClearAllBlocked() => System.Array.Clear(_blocked, 0, _blocked.Length);

        public Vector2Int WorldToCell(Vector3 world)
        {
            var local = world - _origin;
            return new Vector2Int(
                Mathf.FloorToInt(local.x / CellSize),
                Mathf.FloorToInt(local.z / CellSize));
        }

        /// <summary>Centro da célula em coordenadas de mundo (Y=0).</summary>
        public Vector3 CellToWorld(int x, int y) =>
            _origin + new Vector3((x + 0.5f) * CellSize, 0f, (y + 0.5f) * CellSize);

        public Vector3 CellToWorld(Vector2Int c) => CellToWorld(c.x, c.y);

        public Vector3 WorldSize => new Vector3(Width * CellSize, 0f, Height * CellSize);
    }
}
