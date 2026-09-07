using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LSO.Ability.Catalog
{
    /// <summary>
    /// 특성의 이름·설명·아이콘을 아는 유일한 곳.
    ///
    /// ── 왜 모았나 ─────────────────────────────────────────────
    /// 예전에는 이름은 LSO_DisplayNames의 switch가, 설명은 KTH 정보창의
    /// 인스펙터 리스트가 따로 들고 있었다. 특성 하나를 고치려면 두 곳을
    /// 맞춰야 했고, 어긋났을 때 어느 쪽이 맞는지 정할 방법이 없었다.
    ///
    /// 이제 문구를 아는 곳은 이 에셋 하나다. LSO_DisplayNames도 여기를 읽는다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 배치: Assets/Resources/LSO_AbilityCatalog.asset
    /// 이름과 위치가 정확해야 Resources.Load가 찾는다.
    ///
    /// 만들기: 프로젝트 창 우클릭 &gt; Create &gt; LSO &gt; 특성 사전
    /// 만든 뒤 컨텍스트 메뉴의 "빠진 특성 채우기"를 누르면 enum에 있는데
    /// 목록에 없는 것들이 빈 줄로 추가된다. 설명만 적어 넣으면 된다.
    /// </summary>
    [CreateAssetMenu(
        fileName = ResourcePath,
        menuName = "LSO/특성 사전",
        order = 1)]
    public class LSO_AbilityCatalogSO : ScriptableObject
    {
        /// <summary>Resources.Load가 찾을 수 있는 유일한 경로. 파일 이름과 같아야 한다.</summary>
        public const string ResourcePath = "LSO_AbilityCatalog";

        [Tooltip("특성 하나에 한 줄. 순서는 상관없다 — 찾을 때는 표로 바꿔서 본다.")]
        [SerializeField] private List<LSO_AbilityInfo> abilities = new List<LSO_AbilityInfo>();

        // 목록을 매번 훑으면 특성이 늘수록 느려진다. 처음 찾을 때 한 번만 표로 만든다.
        private Dictionary<LSO_AbilityType, LSO_AbilityInfo> _lookup;

        /// <summary>적어둔 전부. 도감처럼 통째로 훑을 때 쓴다.</summary>
        public IReadOnlyList<LSO_AbilityInfo> All => abilities;

        /// <summary>
        /// 적어둔 것이 있으면 꺼낸다.
        ///
        /// 없으면 false를 돌려준다. 이름만 필요하면 LSO_AbilityText를 쓰는 편이 낫다 —
        /// 그쪽은 빠진 것을 enum 이름으로 메워준다.
        /// </summary>
        public bool TryGet(LSO_AbilityType type, out LSO_AbilityInfo info)
        {
            BuildLookupIfNeeded();

            return _lookup.TryGetValue(type, out info);
        }

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<LSO_AbilityType, LSO_AbilityInfo>(abilities.Count);

            foreach (LSO_AbilityInfo info in abilities)
            {
                // 같은 특성을 두 줄 적으면 어느 쪽이 맞는지 알 수 없다. 조용히 덮지 않고 짚는다.
                if (_lookup.ContainsKey(info.type))
                {
                    Debug.LogWarning(
                        $"{name}: '{info.type}'이 두 번 적혀 있습니다. 먼저 나온 줄을 씁니다.", this);
                    continue;
                }

                _lookup[info.type] = info;
            }
        }

        /// <summary>
        /// 인스펙터에서 목록을 고치면 표를 버린다. 다음 조회 때 다시 만든다.
        /// 이게 없으면 플레이 중에 설명을 고쳐도 화면이 안 바뀐다.
        /// </summary>
        private void OnValidate()
        {
            _lookup = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// enum에는 있는데 목록에 없는 특성을 빈 줄로 채운다.
        ///
        /// 새 특성을 만들고 여기 적는 것을 잊으면 화면에 영문 이름이 나온다.
        /// 그걸 눈으로 찾는 대신 이 버튼으로 한 번에 드러낸다.
        /// 이미 적어둔 줄은 건드리지 않는다.
        /// </summary>
        [ContextMenu("빠진 특성 채우기")]
        private void FillMissing()
        {
            var known = new HashSet<LSO_AbilityType>();

            foreach (LSO_AbilityInfo info in abilities)
                known.Add(info.type);

            int added = 0;

            foreach (LSO_AbilityType type in System.Enum.GetValues(typeof(LSO_AbilityType)))
            {
                if (type == LSO_AbilityType.None) continue;
                if (known.Contains(type)) continue;

                abilities.Add(new LSO_AbilityInfo { type = type });
                added++;
            }

            _lookup = null;

            UnityEditor.EditorUtility.SetDirty(this);

            Debug.Log(added > 0
                    ? $"{name}: 빠진 특성 {added}개를 빈 줄로 추가했습니다. 설명을 채워 주세요."
                    : $"{name}: 빠진 특성이 없습니다.",
                this);
        }

        /// <summary>설명이 비어 있는 줄을 모아 짚는다. 채우다 만 것을 찾는 용도다.</summary>
        [ContextMenu("설명 빠진 것 짚기")]
        private void ReportEmpty()
        {
            var empty = new List<string>();

            foreach (LSO_AbilityInfo info in abilities)
            {
                if (string.IsNullOrWhiteSpace(info.description))
                    empty.Add(info.ResolvedName);
            }

            Debug.Log(empty.Count > 0
                    ? $"{name}: 설명이 빈 특성 {empty.Count}개 — {string.Join(", ", empty)}"
                    : $"{name}: 설명이 다 채워져 있습니다.",
                this);
        }
#endif
    }
}
