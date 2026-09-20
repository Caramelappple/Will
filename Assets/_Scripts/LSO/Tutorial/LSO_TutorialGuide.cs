using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Tutorial.Data;
using UnityEngine;

namespace _Scripts.LSO.Tutorial
{
    /// <summary>
    /// 대본이 가리키는 칸을 물들인다. 이미 있는 LDY_TileHighlighter 를 빌려 쓴다.
    ///
    /// ── owner 를 자기 자신으로 넘기는 것이 중요하다 ───────────
    /// LDY_TileHighlighter 는 누가 켠 표시인지로 나눠 들고 있다.
    /// owner 를 안 나누면 LDY_SelectionController 가 자기 표시를 지울 때
    /// 튜토리얼 가이드까지 함께 지워진다 — 기물을 한 번 누르면 노란 칸이 사라진다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 씬 배선: 아무 곳에나 하나. Highlighter 는 비워두면 찾는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_TutorialGuide : MonoBehaviour
    {
        [SerializeField] private LDY_TileHighlighter highlighter;

        private void Awake()
        {
            if (highlighter == null) highlighter = FindAnyObjectByType<LDY_TileHighlighter>();

            if (highlighter == null)
                Debug.LogWarning(
                    $"{name}: LDY_TileHighlighter 를 못 찾아 칸을 물들이지 못합니다. " +
                    "대본이 어디에 놓으라는 것인지 화면에 안 나옵니다.", this);
        }

        public void Show(LSO_TutorialGuideKind kind, IEnumerable<Vector3Int> tiles)
        {
            if (highlighter == null) return;

            highlighter.ClearHighlights(this);

            if (kind == LSO_TutorialGuideKind.None || tiles == null) return;

            switch (kind)
            {
                // 붉은색. 공격 표시를 빌려 쓴다 — 색을 새로 만들지 않는다.
                case LSO_TutorialGuideKind.Place:
                    highlighter.ShowAttackHighlights(this, tiles);
                    break;

                // 노란색. 평소 이동 표시와 같은 색이라 플레이어가 이미 아는 뜻이다.
                case LSO_TutorialGuideKind.Move:
                case LSO_TutorialGuideKind.PlaceYellow:
                    highlighter.ShowMoveHighlights(this, tiles);
                    break;
            }
        }

        public void Clear()
        {
            if (highlighter != null) highlighter.ClearHighlights(this);
        }
    }
}
