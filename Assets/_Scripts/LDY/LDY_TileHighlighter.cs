using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY
{
    // 씬 배선: moveHighlightPrefab(이동 가능 칸, 보라색)과 attackHighlightPrefab(공격 가능 칸, 노란색)에
    // 하이라이트로 쓸 3D 프리팹을 연결할 것. BoardManager도 함께 연결할 것.
    public class LDY_TileHighlighter : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private GameObject moveHighlightPrefab;
        [SerializeField] private GameObject attackHighlightPrefab;

        // 겹칠 때 이동 하이라이트가 위로 보이도록 공격보다 살짝 더 높게 띄운다. 둘 다 타일 표면(y=0)보다는 위.
        [SerializeField] private float attackHeightOffset = 0.05f;
        [SerializeField] private float moveHeightOffset = 0.08f;

        private readonly List<GameObject> _activeHighlights = new List<GameObject>();

        public void ShowMoveHighlights(IEnumerable<Vector3Int> tiles)
        {
            Show(tiles, moveHighlightPrefab, moveHeightOffset);
        }

        public void ShowAttackHighlights(IEnumerable<Vector3Int> tiles)
        {
            Show(tiles, attackHighlightPrefab, attackHeightOffset);
        }

        public void ClearHighlights()
        {
            foreach (var go in _activeHighlights)
            {
                if (go != null)
                    Destroy(go);
            }
            _activeHighlights.Clear();
        }

        private void Show(IEnumerable<Vector3Int> tiles, GameObject prefab, float heightOffset)
        {
            if (prefab == null || board == null || tiles == null) return;

            foreach (var tile in tiles)
            {
                var worldPos = board.GridToWorld(tile) + Vector3.up * heightOffset;
                var go = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                _activeHighlights.Add(go);
            }
        }
    }
}
