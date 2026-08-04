using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY
{
    // 씬 배선: moveHighlightPrefab(이동 가능 칸, 보라색)과 attackHighlightPrefab(공격 가능 칸, 노란색)에
    // 하이라이트로 쓸 3D 프리팹을 연결할 것. BoardManager도 함께 연결할 것.
    // SelectionController/CardPlacer처럼 여러 시스템이 같은 인스턴스를 공유해도 서로의 하이라이트를
    // 지우지 않도록, 호출한 쪽(owner, 보통 this)별로 하이라이트를 따로 관리한다.
    public class LDY_TileHighlighter : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private GameObject moveHighlightPrefab;
        [SerializeField] private GameObject attackHighlightPrefab;

        // 겹칠 때 이동 하이라이트가 위로 보이도록 공격보다 살짝 더 높게 띄운다. 둘 다 타일 표면(y=0)보다는 위.
        [SerializeField] private float attackHeightOffset = 0.05f;
        [SerializeField] private float moveHeightOffset = 0.08f;

        private readonly Dictionary<object, List<GameObject>> _highlightsByOwner = new Dictionary<object, List<GameObject>>();

        public void ShowMoveHighlights(object owner, IEnumerable<Vector3Int> tiles)
        {
            Show(owner, tiles, moveHighlightPrefab, moveHeightOffset);
        }

        public void ShowAttackHighlights(object owner, IEnumerable<Vector3Int> tiles)
        {
            Show(owner, tiles, attackHighlightPrefab, attackHeightOffset);
        }

        // owner가 이전에 띄운 하이라이트만 지운다. 다른 owner가 같은 컴포넌트를 공유해도 침범하지 않는다.
        public void ClearHighlights(object owner)
        {
            if (owner == null || !_highlightsByOwner.TryGetValue(owner, out var list)) return;

            foreach (var go in list)
            {
                if (go != null)
                    Destroy(go);
            }
            list.Clear();
        }

        private void Show(object owner, IEnumerable<Vector3Int> tiles, GameObject prefab, float heightOffset)
        {
            if (owner == null || prefab == null || board == null || tiles == null) return;

            if (!_highlightsByOwner.TryGetValue(owner, out var list))
            {
                list = new List<GameObject>();
                _highlightsByOwner[owner] = list;
            }

            foreach (var tile in tiles)
            {
                var worldPos = board.GridToWorld(tile) + Vector3.up * heightOffset;
                var go = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                list.Add(go);
            }
        }
    }
}
