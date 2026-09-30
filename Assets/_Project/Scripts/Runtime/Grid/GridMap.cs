using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Grid lógico do mapa: células bloqueadas e conversão mundo &lt;-&gt; célula.
    /// O grid fica centralizado na origem do mundo, no plano Y=0.
    ///
    /// Em pé (<see cref="Upright"/>): o eixo X do grid (a marcha, do acampamento para a
    /// base) aponta para -Z de mundo — o inimigo desce da parte de cima da tela para a de
    /// baixo. É o mesmo giro de 90° das lanes do Tower Wars. Quem pensa em células não
    /// percebe diferença: só a conversão mundo &lt;-&gt; célula gira.
    /// </summary>
    public class GridMap
    {
        public readonly int Width;
        public readonly int Height;
        public readonly float CellSize;
        public readonly bool Upright;

        readonly bool[] _blocked;
        readonly Vector3 _origin; // canto da célula (0,0) em coordenadas de mundo

        public GridMap(int width, int height, float cellSize, bool upright = false)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            Upright = upright;
            _blocked = new bool[width * height];
            _origin = new Vector3(-width * cellSize * 0.5f, 0f, -height * cellSize * 0.5f);
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool IsBlocked(int x, int y) => _blocked[y * Width + x];
        public bool IsBlocked(Vector2Int c) => IsBlocked(c.x, c.y);
        public bool IsWalkable(int x, int y) => InBounds(x, y) && !IsBlocked(x, y);

        public void SetBlocked(Vector2Int c, bool value) => _blocked[c.y * Width + c.x] = value;
        public void ClearAllBlocked() => System.Array.Clear(_blocked, 0, _blocked.Length);

        // Euler(0, 90, 0): local (x, z) -> mundo (z, -x); a volta é mundo (x, z) -> local (-z, x)
        Vector3 ToWorld(Vector3 local) => Upright ? new Vector3(local.z, local.y, -local.x) : local;
        Vector3 ToLocal(Vector3 world) => Upright ? new Vector3(-world.z, world.y, world.x) : world;

        public Vector2Int WorldToCell(Vector3 world)
        {
            var local = ToLocal(world) - _origin;
            return new Vector2Int(
                Mathf.FloorToInt(local.x / CellSize),
                Mathf.FloorToInt(local.z / CellSize));
        }

        /// <summary>Centro da célula em coordenadas de mundo (Y=0).</summary>
        public Vector3 CellToWorld(int x, int y) =>
            ToWorld(_origin + new Vector3((x + 0.5f) * CellSize, 0f, (y + 0.5f) * CellSize));

        public Vector3 CellToWorld(Vector2Int c) => CellToWorld(c.x, c.y);

        /// <summary>Tamanho no referencial DO GRID (X = comprimento da marcha), sem o giro.</summary>
        public Vector3 WorldSize => new Vector3(Width * CellSize, 0f, Height * CellSize);

        /// <summary>Quanto o grid ocupa em X e Z de mundo, já girado.</summary>
        public Vector3 Extent => Upright ? new Vector3(Height * CellSize, 0f, Width * CellSize) : WorldSize;

        /// <summary>Sentido da marcha (+X do grid) no mundo.</summary>
        public Vector3 MarchDir => Upright ? new Vector3(0f, 0f, -1f) : new Vector3(1f, 0f, 0f);

        /// <summary>Giro do grid em Y, em graus: quem desenha a grade como filho de um transform usa.</summary>
        public float Yaw => Upright ? 90f : 0f;
    }
}
