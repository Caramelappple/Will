using System.Collections.Generic;
using System.Text;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Ability.Catalog;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 카드의 특성 칸에 손을 올리면 그 특성들의 설명을 띄운다.
    ///
    /// ── 왜 새로 만들지 않고 물려받았나 ────────────────────────
    /// 월드 오브젝트 위에 글을 띄우는 일은 DLJ_WorldValueTooltip 이 이미 한다.
    /// 호버 판정, UI에 가렸는지, 잉크가 번지는 연출, 사라질 때의 페이드가
    /// 전부 그쪽에 있다. 여기서 또 만들면 같은 일을 하는 툴팁이 둘이 되고,
    /// 한쪽만 고쳤을 때 카드와 양초의 글씨가 다르게 움직인다.
    ///
    /// 이 클래스가 새로 아는 것은 하나다 — <b>무슨 글을 띄울 것인가.</b>
    /// ─────────────────────────────────────────────────────────
    ///
    /// 특성 목록은 스스로 찾지 않는다. 카드를 그리는 쪽이 Show 로 넣어준다.
    /// 여기서 보상 구조를 뚫고 들어가면, 손패 카드에 같은 툴팁을 붙이고 싶을 때
    /// 보상 쪽을 아는 코드가 손패에까지 딸려온다.
    ///
    /// 씬 배선:
    ///   특성 이름이 적힌 칸에 Collider 를 맞춰 넣고 이 컴포넌트를 붙인다.
    ///   Font 와 Ink Material 은 유언 양초 툴팁과 같은 것을 쓰면 된다.
    ///   카드를 그리는 쪽(LSO_RewardPieceCard)이 Show/Clear 를 불러준다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_AbilityTooltip : DLJ_WorldValueTooltip
    {
        [Header("특성 설명")]
        [Tooltip("손을 올릴 자리. 특성 이름이 적힌 칸에 맞춘 Collider 를 넣는다.\n" +
                 "비우면 같은 오브젝트에서 찾는다.\n" +
                 "\n" +
                 "카드 전체가 아니라 특성 칸에만 맞출 것 — 카드 전체에 걸면\n" +
                 "이름이나 수치를 볼 때도 설명이 따라 뜬다.")]
        [SerializeField] private Collider hoverCollider;

        [Tooltip("설명을 이름보다 작게 띄운다. 100이면 같은 크기.\n" +
                 "유언 양초 툴팁과 같은 방식이라 두 화면의 글이 같은 모양으로 보인다.")]
        [SerializeField, Range(40, 100)] private int descriptionScale = 65;

        [Tooltip("특성이 여럿일 때 사이에 넣을 빈 줄 수.")]
        [SerializeField, Range(0, 2)] private int gapLines = 1;

        [Tooltip("설명이 길면 이 너비에서 줄을 바꾼다. 0이면 안 바꾼다.")]
        [SerializeField, Min(0f)] private float wrapWidth = 6f;

        [Header("진단")]
        [Tooltip("켜면 왜 안 뜨는지 콘솔에 찍는다.\n" +
                 "\n" +
                 "매 프레임이 아니라 상태가 바뀔 때만 한 줄씩 나온다 —\n" +
                 "호버는 초당 수십 번 판정되므로 그대로 찍으면 콘솔이 덮인다.")]
        [SerializeField] private bool logSteps;

        /// <summary>같은 말을 두 번 찍지 않으려고 마지막으로 찍은 것을 들고 있는다.</summary>
        private string _lastLog;

        /// <summary>
        /// 지금 띄울 특성들. 카드가 넣어준다.
        ///
        /// 목록을 통째로 받아두는 이유는 카드가 풀에서 돌려쓰이기 때문이다.
        /// 넘겨받은 목록을 그대로 들고 있으면 카드가 다른 기물로 바뀐 뒤에도
        /// 그 참조가 살아 있어 지난 기물의 특성이 뜬다.
        /// </summary>
        private readonly List<LSO_AbilityType> _types = new();

        /// <summary>
        /// 매 프레임 새 문자열을 만들지 않으려고 만들어 둔 것만 들고 있는다.
        ///
        /// TryGetText 는 호버가 붙어 있는 동안 계속 불린다. 여기서 매번 이어 붙이면
        /// 같은 글을 초당 수십 번 새로 만드는 셈이고, 바깥에서는 <b>글이 바뀐 것으로
        /// 보여</b> 잉크가 처음부터 다시 번진다(DLJ_WorldValueTooltip 의 글씨 경로 참고).
        /// </summary>
        private string _cached;

        protected override float TextWrapWidth => wrapWidth;

        /// <summary>
        /// 손을 올린 채로 카드가 다른 기물로 바뀌어도 잉크를 처음부터 돌리지 않는다.
        /// 보상 카드가 그렇게 바뀔 일은 드물지만, 바뀐다면 글만 갈아 끼우는 편이 읽기 좋다.
        /// </summary>
        protected override bool ReplayInkOnValueChange => false;

        protected override void Awake()
        {
            base.Awake();

            if (hoverCollider == null) hoverCollider = GetComponent<Collider>();

            if (hoverCollider != null) return;

            Debug.LogError(
                $"{name}: Collider 가 없어 특성 칸에 손을 올려도 설명이 뜨지 않습니다. " +
                "특성 이름 칸에 맞춘 Collider 를 넣어 주세요 : LSO_AbilityTooltip", this);

            enabled = false;
        }

        /// <summary>
        /// 이 카드가 가진 특성을 넣는다. 카드를 그릴 때 부른다.
        ///
        /// 같은 목록을 다시 넣어도 안전하다 — 글이 그대로면 잉크도 다시 돌지 않는다.
        /// </summary>
        public void Show(IReadOnlyList<LSO_AbilityType> types)
        {
            _types.Clear();

            if (types != null)
            {
                for (int i = 0; i < types.Count; i++)
                {
                    // None 은 "특성 없음"이라 설명할 것이 없다.
                    if (types[i] == LSO_AbilityType.None) continue;

                    _types.Add(types[i]);
                }
            }

            _cached = Build();

            Debug.Log(_types.Count == 0
                ? "Show — 넣을 특성이 없다. 이 기물은 특성이 없거나 전부 None 이다"
                : $"Show — 특성 {_types.Count}개 준비됨: {_cached?.Replace("\n", " / ")}");
        }

        /// <summary>
        /// 띄울 것을 비운다. 카드가 풀로 돌아가거나 빈 보상이 들어왔을 때 부른다.
        ///
        /// 안 비우면 다음에 이 카드로 나온 기물에 지난 기물의 특성 설명이 뜬다.
        /// </summary>
        public void Clear()
        {
            _types.Clear();
            _cached = null;
        }

        /// <summary>띄울 특성이 하나도 없으면 거짓. 그러면 툴팁 자체가 안 뜬다.</summary>
        protected override bool TryGetText(out string text)
        {
            text = _cached;

            if (!string.IsNullOrEmpty(text)) return true;

            // 콜라이더는 맞혔는데 여기서 거짓이 나오면 툴팁이 안 뜬다.
            // 둘을 구분해야 "손을 잘못 올린 것"과 "넣어줄 것이 없는 것"이 갈린다.
            Debug.Log("띄울 글이 없다 — Show 가 안 불렸거나 특성이 없는 기물이다");

            return false;
        }

        /// <summary>
        /// 특성 칸을 덮는 Collider 만 본다.
        ///
        /// 카드 전체 Collider(클릭용)를 쓰지 않는 이유는, 그쪽은 카드를 고르는 자리라
        /// 이름이나 수치를 보려고 지나가기만 해도 설명이 따라 뜨기 때문이다.
        /// </summary>
        protected override Transform FindHoveredTarget(Ray ray, UnityEngine.Camera camera, out float distance)
        {
            distance = camera.farClipPlane;

            if (hoverCollider == null || !hoverCollider.enabled)
            {
                Debug.Log("Hover Collider 가 없거나 꺼져 있다");
                return null;
            }

            if ((camera.cullingMask & (1 << gameObject.layer)) == 0)
            {
                Debug.Log($"이 오브젝트의 레이어({gameObject.layer})가 카메라 Culling Mask 에서 빠져 있다");
                return null;
            }

            if (!hoverCollider.Raycast(ray, out RaycastHit hit, distance))
            {
                Debug.Log("마우스가 Hover Collider 밖에 있다");
                return null;
            }

            if (hit.distance >= distance) return null;

            distance = hit.distance;

            Debug.Log($"Hover Collider 를 맞혔다 — '{hoverCollider.name}', 거리 {hit.distance:0.00}");

            // ── 이 transform 이 아니라 콜라이더의 transform 을 돌려주는 이유 ──
            // 부모 클래스는 돌려받은 것을 "가려졌는지" 판정의 기준으로 쓴다.
            // 물리 레이가 맞힌 것이 이 Transform 의 자식이 아니면 가려진 것으로 보고
            // 툴팁을 접는다(DLJ_WorldValueTooltip 의 FindHoveredTarget 참고).
            //
            // 콜라이더를 이 컴포넌트와 <b>다른 오브젝트</b>에 걸 수 있게 해뒀으므로,
            // 여기서 자기 transform 을 돌려주면 레이가 맞힌 콜라이더가 남남이 되어
            // 항상 가려진 것으로 판정된다 — 툴팁이 한 번도 안 뜬다.
            //
            // 기준을 콜라이더 쪽으로 맞추면 어디에 걸든 맞고, 진짜로 다른 것이
            // 앞을 막았을 때만 접힌다.
            return hoverCollider.transform;
        }

        /// <summary>특성 칸의 윗변. 글이 칸을 가리지 않도록 위로 띄운다.</summary>
        protected override Vector3 GetDefaultAnchor()
        {
            if (hoverCollider == null) return transform.position;

            Bounds bounds = hoverCollider.bounds;

            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }

        /// <summary>
        /// 띄울 글을 만든다.
        ///
        /// 이름과 설명을 가져오는 곳은 LSO_AbilityText 하나다. 카탈로그 에셋의 문구를
        /// 그대로 쓰므로, 카드에 적힌 특성 이름과 여기 뜨는 이름이 갈리지 않는다.
        /// </summary>
        private string Build()
        {
            if (_types.Count == 0) return null;

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < _types.Count; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');

                    for (int gap = 0; gap < gapLines; gap++) builder.Append('\n');
                }

                builder.Append(LSO_AbilityText.NameOf(_types[i]));

                string description = LSO_AbilityText.DescriptionOf(_types[i]);

                // 설명을 안 적어둔 특성도 이름은 띄운다.
                // 이름까지 빼면 손을 올렸는데 아무 일도 안 일어난 것처럼 보인다.
                if (string.IsNullOrEmpty(description)) continue;

                builder.Append("\n<size=").Append(descriptionScale).Append("%>")
                       .Append(description)
                       .Append("</size>");
            }

            return builder.ToString();
        }
    }
}
