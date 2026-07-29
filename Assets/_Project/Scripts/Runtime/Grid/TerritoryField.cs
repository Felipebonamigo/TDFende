using System.Collections.Generic;
using UnityEngine;

namespace FrontierTD
{
    /// <summary>
    /// Território do jogador: células dentro do raio de fronteira de qualquer torre.
    /// Inimigos dentro dele sofrem atrito — a mecânica-teste deste projeto,
    /// herdada do Rise of Nations. Se ela for divertida aqui, o RTS tem alma.
    /// </summary>
    public class TerritoryField
    {
        readonly GridMap _map;
        readonly bool[] _inside;

        public TerritoryField(GridMap map)
        {
            _map = map;
            _inside = new bool[map.Width * map.Height];
        }

        public bool Contains(int x, int y) => _map.InBounds(x, y) && _inside[y * _map.Width + x];

        public bool Contains(Vector3 worldPos)
        {
            var c = _map.WorldToCell(worldPos);
            return Contains(c.x, c.y);
        }

        /// <summary>Célula de dentro com pelo menos um vizinho ortogonal de fora — a linha da fronteira.</summary>
        public bool IsEdge(int x, int y) =>
            Contains(x, y) &&
            (!Contains(x + 1, y) || !Contains(x - 1, y) || !Contains(x, y + 1) || !Contains(x, y - 1));

        /// <summary>Recalcula o território (distância em células; raio circular por torre).</summary>
        public void Rebuild(IReadOnlyList<Vector2Int> towerCells, float radius)
        {
            System.Array.Clear(_inside, 0, _inside.Length);
            if (towerCells.Count == 0) return;

            float r2 = radius * radius;
            for (int y = 0; y < _map.Height; y++)
            for (int x = 0; x < _map.Width; x++)
            {
                for (int t = 0; t < towerCells.Count; t++)
                {
                    float dx = x - towerCells[t].x;
                    float dy = y - towerCells[t].y;
                    if (dx * dx + dy * dy <= r2)
                    {
                        _inside[y * _map.Width + x] = true;
                        break;
                    }
                }
            }
        }
    }
}
