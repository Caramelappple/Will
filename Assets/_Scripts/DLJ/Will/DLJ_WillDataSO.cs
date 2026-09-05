using UnityEngine;

namespace _Scripts.LSO.Will
{
    /// <summary>Common metadata shared by every will data asset.</summary>
    public abstract class DLJ_WillDataSO : ScriptableObject
    {
        public abstract LSO_WillType WillType { get; }

        [Header("Name")]
        [Tooltip("화면에 띄울 한글 이름. 예: 저주, 계약\n" +
                 "\n" +
                 "비워두면 영문 enum 이름이 그대로 나온다.\n" +
                 "에셋 파일 이름은 영문으로 둘 것 — 한글 파일명은 윈도우와 맥이\n" +
                 "다르게 저장해서 Git이 같은 파일을 두 벌로 본다.")]
        public string displayName;

        [Header("Tool Tip")]
        [TextArea(3, 10)]
        public string description;

        [Header("Icon")]
        public Sprite icon;

        [Header("Effect")]
        public GameObject effectPrefab;

        [Header("Camera")]
        [Min(0f)] public float cameraHoldDuration = 1.8f;

        /// <summary>
        /// 화면에 띄울 이름. 안 적어뒀으면 영문 enum 이름이 나온다.
        ///
        /// 한글이 아니라 영문이 보이면 이 에셋의 Display Name이 빈 것이다.
        /// "이름 없음" 같은 자리채움을 넣지 않는 이유는, 그러면 어느 에셋이
        /// 비었는지 화면만 보고는 알 수 없기 때문이다.
        /// </summary>
        public string DisplayName =>
            string.IsNullOrWhiteSpace(displayName) ? WillType.ToString() : displayName;

        public virtual int DisplayDamage => 0;
        public virtual int DisplayRange => 0;
        public virtual int DisplayDuration => 0;
        public virtual int DisplayBuffAmount => 0;
        public virtual int DisplayDebuffAmount => 0;
    }
}
