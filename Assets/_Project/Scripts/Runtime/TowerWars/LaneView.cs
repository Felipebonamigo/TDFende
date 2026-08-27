using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Desenho de UMA lane. Não guarda estado de jogo: lê o LaneSim todo frame e
    /// espelha na tela. A simulação é a fonte da verdade única — assim a lane do
    /// adversário usa exatamente o mesmo código de desenho da sua, e no
    /// multiplayer ela vira só um LaneSim alimentado pela rede.
    ///
    /// Tudo vive sob um pai deslocado e usa coordenadas LOCAIS do grid, para as
    /// duas lanes não se desenharem uma em cima da outra.
    /// </summary>
    public class LaneView
    {
        readonly LaneSim _sim;
        readonly Transform _root;
        readonly Transform _enemyRoot;
        readonly Transform _towerRoot;
        readonly TerritoryRenderer _territory;

        readonly List<Transform> _enemyPool = new List<Transform>(64);
        readonly List<Renderer> _enemyRenderers = new List<Renderer>(64);
        readonly List<Transform> _towerObjects = new List<Transform>(32);
        readonly MaterialPropertyBlock _mpb = new MaterialPropertyBlock();

        int _drawnTowers;
        int _lastTerritoryStamp = -1;

        public Transform Root => _root;
        public LaneSim Sim => _sim;

        public LaneView(LaneSim sim, Vector3 offset, string name, Color groundTint)
        {
            _sim = sim;
            _root = new GameObject(name).transform;
            _root.position = offset;

            BuildGround(groundTint);
            _enemyRoot = new GameObject("Inimigos").transform;
            _enemyRoot.SetParent(_root, false);
            _towerRoot = new GameObject("Torres").transform;
            _towerRoot.SetParent(_root, false);
            _territory = new TerritoryRenderer(_root);
        }

        void BuildGround(Color tint)
        {
            var size = _sim.Map.WorldSize;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Chao";
            Object.Destroy(ground.GetComponent<Collider>());
            ground.transform.SetParent(_root, false);
            ground.transform.localScale = new Vector3(size.x, 0.1f, size.z);
            ground.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            ground.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGround(
                Palette.GroundDark * tint, Palette.GroundLight * tint, _sim.Map.Width, _sim.Map.Height);

            var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseGo.name = "Base";
            Object.Destroy(baseGo.GetComponent<Collider>());
            baseGo.transform.SetParent(_root, false);
            baseGo.transform.localPosition = _sim.Map.CellToWorld(_sim.GoalCell) + Vector3.up * 0.6f;
            baseGo.transform.localScale = new Vector3(1.1f, 1.2f, 1.1f);
            baseGo.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(Palette.BaseGold);

            foreach (var s in _sim.SpawnCells)
            {
                var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
                m.name = "Spawn";
                Object.Destroy(m.GetComponent<Collider>());
                m.transform.SetParent(_root, false);
                m.transform.localPosition = _sim.Map.CellToWorld(s) + Vector3.up * 0.05f;
                m.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);
                m.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(Palette.SpawnMagenta);
            }
        }

        /// <summary>Espelha o estado da simulação. Chamar uma vez por frame, depois do Tick.</summary>
        public void Sync()
        {
            SyncTowers();
            SyncEnemies();
        }

        void SyncTowers()
        {
            // torres só nascem, nunca somem: basta criar as que faltam
            for (int i = _drawnTowers; i < _sim.TowerCount; i++)
                _towerObjects.Add(CreateTower(_sim.TowerCell(i)));
            _drawnTowers = _sim.TowerCount;

            // altura do canhão mostra o nível — leitura de força sem número na tela
            for (int i = 0; i < _towerObjects.Count; i++)
            {
                int level = _sim.TowerLevel(i);
                var head = _towerObjects[i].GetChild(1);
                head.localPosition = new Vector3(0f, 0.95f + 0.10f * (level - 1), 0f);
                head.localScale = new Vector3(0.35f, 0.25f, 0.6f + 0.08f * (level - 1));
            }

            // o território muda junto com as torres; reconstruir só quando isso acontece
            if (_lastTerritoryStamp != _sim.TowerCount)
            {
                _lastTerritoryStamp = _sim.TowerCount;
                _territory.Rebuild(_sim.Territory, _sim.Map);
            }
        }

        Transform CreateTower(Vector2Int cell)
        {
            var root = new GameObject("Torre").transform;
            root.SetParent(_towerRoot, false);
            root.localPosition = _sim.Map.CellToWorld(cell);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root, false);
            body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            body.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            body.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(Palette.TowerBody);

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(root, false);
            head.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            head.transform.localScale = new Vector3(0.35f, 0.25f, 0.6f);
            head.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(Palette.TowerHead);

            return root;
        }

        void SyncEnemies()
        {
            int used = 0;
            int slots = _sim.EnemySlotCount;

            for (int s = 0; s < slots; s++)
            {
                if (!_sim.TryGetEnemy(s, out var e)) continue;

                var t = RentEnemy(used);
                t.localPosition = e.Pos;
                t.localScale = Vector3.one * (0.45f + 0.25f * SizeOf(e.TypeId));

                var c = Color.Lerp(Palette.EnemyHurt, TypeColor(e.TypeId), e.MaxHp > 0f ? e.Hp / e.MaxHp : 1f);
                // dentro do território, puxa pro ciano: dá pra VER o atrito agindo
                if (e.AttritionScale > 0f && _sim.Territory.Contains(e.Pos))
                    c = Color.Lerp(c, Palette.EnemyDrained, 0.5f);
                _mpb.SetColor(MaterialFactory.ColorProperty, c);
                _enemyRenderers[used].SetPropertyBlock(_mpb);

                used++;
            }

            for (int i = used; i < _enemyPool.Count; i++)
                if (_enemyPool[i].gameObject.activeSelf)
                    _enemyPool[i].gameObject.SetActive(false);
        }

        // silhueta por tipo: gordo, enxame, veloz — legível sem modelo 3D
        static float SizeOf(int typeId)
        {
            var u = SendCatalog.Get(typeId);
            if (u.Count > 1) return 0.25f;
            if (u.Hp >= 400f) return 1.6f;
            if (u.Hp >= 150f) return 1.15f;
            return 0.7f;
        }

        static Color TypeColor(int typeId)
        {
            var u = SendCatalog.Get(typeId);
            if (u.IgnoresTerritory) return Palette.TowerHead; // voador: azul claro, destoa do chão
            if (u.Hp >= 400f) return Palette.TextDanger;
            return Palette.EnemyFull;
        }

        Transform RentEnemy(int index)
        {
            while (_enemyPool.Count <= index)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "Inimigo";
                Object.Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(_enemyRoot, false);
                go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(Palette.EnemyFull);
                _enemyPool.Add(go.transform);
                _enemyRenderers.Add(go.GetComponent<Renderer>());
            }

            var t = _enemyPool[index];
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            return t;
        }

        /// <summary>Converte ponto do mundo para célula desta lane (desfaz o deslocamento do pai).</summary>
        public Vector2Int WorldToCell(Vector3 world) => _sim.Map.WorldToCell(_root.InverseTransformPoint(world));

        public Vector3 CellToWorld(Vector2Int cell) => _root.TransformPoint(_sim.Map.CellToWorld(cell));
    }
}
