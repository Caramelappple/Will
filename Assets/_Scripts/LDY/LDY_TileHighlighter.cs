using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

        // 종류나 생성 순서와 관계없이 모든 하이라이트를 같은 위치에 겹쳐 표시한다.
        [FormerlySerializedAs("attackHeightOffset")]
        [SerializeField] private float highlightHeightOffset = 0.05f;

        private sealed class Marks
        {
            public readonly List<GameObject> Objects = new List<GameObject>();
        }

        private readonly Dictionary<object, Marks> _highlightsByOwner = new Dictionary<object, Marks>();

        public void ShowMoveHighlights(object owner, IEnumerable<Vector3Int> tiles)
        {
            Show(owner, tiles, moveHighlightPrefab);
        }

        public void ShowAttackHighlights(object owner, IEnumerable<Vector3Int> tiles)
        {
            Show(owner, tiles, attackHighlightPrefab);
        }

        /// <summary>
        /// 공격 표시의 형태는 유지하면서 이 호출로 만든 인스턴스에만 색을 덮어쓴다.
        /// 공유 머티리얼은 바꾸지 않으므로 일반 공격 하이라이트 색에는 영향을 주지 않는다.
        /// </summary>
        public void ShowColoredAttackHighlights(
            object owner, IEnumerable<Vector3Int> tiles, Color color)
        {
            Show(owner, tiles, attackHighlightPrefab, color);
        }

        // owner가 이전에 띄운 하이라이트만 지운다. 다른 owner가 같은 컴포넌트를 공유해도 침범하지 않는다.
        public void ClearHighlights(object owner)
        {
            if (owner == null || !_highlightsByOwner.TryGetValue(owner, out var marks)) return;

            foreach (var go in marks.Objects)
            {
                if (go != null)
                    Destroy(go);
            }

            marks.Objects.Clear();
        }

        private void Show(
            object owner, IEnumerable<Vector3Int> tiles, GameObject prefab, Color? colorOverride = null)
        {
            if (owner == null || prefab == null || board == null || tiles == null) return;

            if (!_highlightsByOwner.TryGetValue(owner, out var marks))
            {
                marks = new Marks();
                _highlightsByOwner[owner] = marks;
            }

            foreach (var tile in tiles)
            {
                var worldPos = board.GridToWorld(tile) + Vector3.up * highlightHeightOffset;
                var go = Instantiate(prefab, worldPos, Quaternion.identity, transform);

                if (colorOverride.HasValue)
                    ApplyColor(go, colorOverride.Value);

                marks.Objects.Add(go);
            }
        }

        private static void ApplyColor(GameObject instance, Color color)
        {
            if (instance == null) return;

            Color emission = color * 1.5f;
            emission.a = color.a;

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (material == null) continue;

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);

                if (material.HasProperty("_BaseColor")) block.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) block.SetColor("_Color", color);
                if (material.HasProperty("_EmissionColor")) block.SetColor("_EmissionColor", emission);

                renderer.SetPropertyBlock(block);
            }
        }
    }
}
