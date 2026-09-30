using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Campo de fluxo: custo integrado (Dijkstra a partir da base) + direção por célula.
    /// Todos os inimigos compartilham o mesmo campo — o custo do rebuild é O(células),
    /// independente da quantidade de inimigos. É a mesma técnica que o RTS vai usar.
    /// </summary>
    public class FlowField
    {
        public const int Unreachable = int.MaxValue;

        readonly GridMap _map;
        readonly int[] _cost;
        readonly Vector3[] _dir;
        readonly int[] _scratchCost; // buffer para testes de posicionamento (não corrompe o campo real)
        Vector2Int _goal;

        // 4 ortogonais + 4 diagonais; custo 10/14 (aprox. de 1 e raiz de 2)
        static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        static readonly int[] Dy = { 0, 0, 1, -1, 1, -1, 1, -1 };
        static readonly int[] MoveCost = { 10, 10, 10, 10, 14, 14, 14, 14 };

        public FlowField(GridMap map)
        {
            _map = map;
            _cost = new int[map.Width * map.Height];
            _dir = new Vector3[map.Width * map.Height];
            _scratchCost = new int[map.Width * map.Height];
        }

        public void Rebuild(Vector2Int goal)
        {
            _goal = goal;
            ComputeCosts(goal, _cost);
            ComputeDirections();
        }

        public bool IsReachable(Vector2Int cell) =>
            _map.InBounds(cell.x, cell.y) && _cost[Index(cell.x, cell.y)] != Unreachable;

        /// <summary>true se bloquear <paramref name="cell"/> deixaria algum spawn sem caminho até a base.</summary>
        public bool PlacementBlocksPath(Vector2Int cell, IReadOnlyList<Vector2Int> spawns)
        {
            _map.SetBlocked(cell, true);
            ComputeCosts(_goal, _scratchCost);
            bool blocks = false;
            for (int i = 0; i < spawns.Count; i++)
            {
                if (_scratchCost[Index(spawns[i].x, spawns[i].y)] == Unreachable)
                {
                    blocks = true;
                    break;
                }
            }
            _map.SetBlocked(cell, false);
            return blocks;
        }

        /// <summary>Custo integrado até a base (10 por passo reto), ou Unreachable.</summary>
        public int CostAt(Vector2Int cell) =>
            _map.InBounds(cell.x, cell.y) ? _cost[Index(cell.x, cell.y)] : Unreachable;

        /// <summary>
        /// Custo de <paramref name="from"/> até a base SE <paramref name="cell"/> fosse bloqueada
        /// (Unreachable = fecharia o caminho). A diferença para CostAt é quanto uma torre ali
        /// alonga a marcha — a medida do "maze" que a IA usa para escolher onde construir.
        /// </summary>
        public int CostIfBlocked(Vector2Int cell, Vector2Int from)
        {
            bool was = _map.IsBlocked(cell);
            _map.SetBlocked(cell, true);
            ComputeCosts(_goal, _scratchCost);
            _map.SetBlocked(cell, was);
            return _scratchCost[Index(from.x, from.y)];
        }

        /// <summary>
        /// Caminho de <paramref name="from"/> até a base descendo o custo, célula a célula —
        /// é por onde a marcha passa agora. Preenche <paramref name="into"/> (limpa antes).
        /// </summary>
        public void Path(Vector2Int from, List<Vector2Int> into)
        {
            into.Clear();
            if (CostAt(from) == Unreachable) return;
            var c = from;
            for (int guard = 0; guard < _cost.Length && c != _goal; guard++)
            {
                into.Add(c);
                int best = _cost[Index(c.x, c.y)];
                var next = c;
                for (int n = 0; n < 8; n++)
                {
                    int nx = c.x + Dx[n], ny = c.y + Dy[n];
                    if (!_map.InBounds(nx, ny)) continue;
                    int nc = _cost[Index(nx, ny)];
                    if (nc < best) { best = nc; next = new Vector2Int(nx, ny); }
                }
                if (next == c) break;
                c = next;
            }
            into.Add(_goal);
        }

        /// <summary>Direção de movimento na posição de mundo dada (plano Y=0).</summary>
        public Vector3 SampleDirection(Vector3 worldPos)
        {
            var c = _map.WorldToCell(worldPos);
            if (!_map.InBounds(c.x, c.y) || c == _goal)
                return Flat(_map.CellToWorld(_goal) - worldPos);

            var d = _dir[Index(c.x, c.y)];
            if (d == Vector3.zero) // célula inalcançável (bolsão fechado por torres): anda reto até a base
                return Flat(_map.CellToWorld(_goal) - worldPos);
            return d;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.zero;
        }

        int Index(int x, int y) => y * _map.Width + x;

        void ComputeCosts(Vector2Int goal, int[] cost)
        {
            for (int i = 0; i < cost.Length; i++) cost[i] = Unreachable;
            var heap = new MinHeap(cost.Length);
            cost[Index(goal.x, goal.y)] = 0;
            heap.Push(Index(goal.x, goal.y), 0);

            while (heap.Count > 0)
            {
                heap.Pop(out int idx, out int c);
                if (c > cost[idx]) continue; // entrada obsoleta no heap
                int cx = idx % _map.Width, cy = idx / _map.Width;

                for (int n = 0; n < 8; n++)
                {
                    int nx = cx + Dx[n], ny = cy + Dy[n];
                    if (!_map.IsWalkable(nx, ny)) continue;
                    // diagonal só se os dois vizinhos ortogonais estiverem livres (não cortar quina de torre)
                    if (n >= 4 && (!_map.IsWalkable(cx + Dx[n], cy) || !_map.IsWalkable(cx, cy + Dy[n]))) continue;

                    int nIdx = Index(nx, ny);
                    int nc = c + MoveCost[n];
                    if (nc < cost[nIdx])
                    {
                        cost[nIdx] = nc;
                        heap.Push(nIdx, nc);
                    }
                }
            }
        }

        void ComputeDirections()
        {
            for (int y = 0; y < _map.Height; y++)
            for (int x = 0; x < _map.Width; x++)
            {
                int idx = Index(x, y);
                _dir[idx] = Vector3.zero;
                if (_cost[idx] == Unreachable || _map.IsBlocked(x, y)) continue;

                // aponta para o vizinho de menor custo integrado
                int best = _cost[idx];
                int bx = x, by = y;
                for (int n = 0; n < 8; n++)
                {
                    int nx = x + Dx[n], ny = y + Dy[n];
                    if (!_map.IsWalkable(nx, ny)) continue;
                    if (n >= 4 && (!_map.IsWalkable(x + Dx[n], y) || !_map.IsWalkable(x, y + Dy[n]))) continue;

                    int nIdx = Index(nx, ny);
                    if (_cost[nIdx] < best)
                    {
                        best = _cost[nIdx];
                        bx = nx;
                        by = ny;
                    }
                }
                if (bx != x || by != y)
                    _dir[idx] = (_map.CellToWorld(bx, by) - _map.CellToWorld(x, y)).normalized;
            }
        }

        /// <summary>Heap binário mínimo (índice de célula, prioridade). Sem alocação após construção.</summary>
        class MinHeap
        {
            int[] _idx;
            int[] _pri;
            int _count;

            public int Count => _count;

            public MinHeap(int capacity)
            {
                _idx = new int[capacity];
                _pri = new int[capacity];
            }

            public void Push(int idx, int pri)
            {
                if (_count == _idx.Length)
                {
                    System.Array.Resize(ref _idx, _count * 2);
                    System.Array.Resize(ref _pri, _count * 2);
                }
                _idx[_count] = idx;
                _pri[_count] = pri;
                int i = _count++;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (_pri[p] <= _pri[i]) break;
                    Swap(i, p);
                    i = p;
                }
            }

            public void Pop(out int idx, out int pri)
            {
                idx = _idx[0];
                pri = _pri[0];
                _count--;
                _idx[0] = _idx[_count];
                _pri[0] = _pri[_count];
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1, r = l + 1, s = i;
                    if (l < _count && _pri[l] < _pri[s]) s = l;
                    if (r < _count && _pri[r] < _pri[s]) s = r;
                    if (s == i) break;
                    Swap(i, s);
                    i = s;
                }
            }

            void Swap(int a, int b)
            {
                (_idx[a], _idx[b]) = (_idx[b], _idx[a]);
                (_pri[a], _pri[b]) = (_pri[b], _pri[a]);
            }
        }
    }
}
