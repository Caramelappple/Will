using System.Text;
using TMPro;
using UnityEngine;

namespace _Scripts.LSO.UI.Text
{
    /// <summary>
    /// 글자를 세로로 흐르게 한다. 세로로 세우는 것 외의 책임은 갖지 않는다.
    ///
    /// TMP에는 세로쓰기 모드가 없다. RectTransform을 90도 돌리면 글자까지 눕는다.
    /// 그래서 글자를 한 줄에 하나씩 놓아 위에서 아래로 읽히게 만든다.
    ///
    /// 열이 여럿일 때는 글자를 가로로 뒤집어 넣는다.
    /// 첫 줄에 각 열의 첫 글자를, 둘째 줄에 각 열의 둘째 글자를 놓는 식이다.
    /// 줄바꿈만으로는 열을 만들 수 없기 때문이다 — 모든 글자가 이미 한 줄씩 차지하고 있어서
    /// 줄을 더 넣어봐야 빈 줄이 생길 뿐 옆으로 가지 않는다.
    ///
    /// 열을 맞추려면 글자 폭이 같아야 하므로 mspace 태그를 쓴다.
    /// 한글·한자는 원래 폭이 고르지만 영문과 숫자가 섞이면 이것 없이는 열이 어긋난다.
    ///
    /// 원문은 이 컴포넌트가 들고 있고, TMP의 text는 편집한 결과만 담는다.
    /// 두 값을 같은 곳에 두면 이미 세로인 문자열을 다시 세로로 펴는 사고가 난다.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [ExecuteAlways]
    public class LSO_VerticalText : MonoBehaviour
    {
        [Header("원문")]
        [Tooltip("가로로 쓴 원래 문장. 세로로 세우는 것은 이 컴포넌트가 한다.")]
        [SerializeField, TextArea(10, 10)] private string sourceText = "";

        [Header("열 나누기")]
        [Tooltip("한 열에 들어갈 글자 수. 0이면 나누지 않고 한 열로만 쭉 내려간다.")]
        [SerializeField, Min(0)] private int charactersPerColumn;

        [Tooltip("켜면 오른쪽 열부터 읽는다(전통 세로쓰기).\n" +
                 "끄면 왼쪽에서 시작해 오른쪽으로 넘어간다.")]
        [SerializeField] private bool rightToLeftColumns;

        [Tooltip("열과 열 사이에 둘 빈 칸 수.")]
        [SerializeField, Min(0)] private int columnSpacing = 1;

        [Header("글자 폭")]
        [Tooltip("켜면 mspace 태그로 글자 폭을 고정해 열을 맞춘다. 열이 하나면 필요 없다.")]
        [SerializeField] private bool forceMonospace = true;

        [Tooltip("고정할 글자 폭. 1em이 글꼴의 기본 한 칸이다.")]
        [SerializeField, Min(0.1f)] private float monospaceEm = 1f;

        [Header("공백 처리")]
        [Tooltip("켜면 원문의 띄어쓰기를 빈 칸 한 줄로 남긴다. 끄면 없앤다.")]
        [SerializeField] private bool keepSpaces;

        private TMP_Text _label;

        // 글자 수만큼 문자열을 이어붙이므로 갱신마다 새로 만들면 쓰레기가 쌓인다.
        private readonly StringBuilder _builder = new StringBuilder();

        // 공백과 줄바꿈을 걷어낸 뒤의 글자들. 열을 나누려면 최종 길이를 먼저 알아야 한다.
        private readonly StringBuilder _cleaned = new StringBuilder();

        /// <summary>
        /// 원문. 넣으면 즉시 세로로 세워 화면에 반영한다.
        ///
        /// TMP의 text가 아니라 이쪽에 넣어야 한다. TMP에 직접 넣으면
        /// 다음 Refresh 때 덮어써진다.
        /// </summary>
        public string Text
        {
            get => sourceText;
            set
            {
                if (sourceText == value) return;

                sourceText = value;
                Refresh();
            }
        }

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>원문을 다시 세운다. 바깥에서 설정을 바꾼 뒤 부르면 된다.</summary>
        public void Refresh()
        {
            if (_label == null)
                _label = GetComponent<TMP_Text>();

            if (_label == null) return;

            _label.text = BuildVertical(sourceText);
        }

        private string BuildVertical(string source)
        {
            _builder.Clear();

            string text = Clean(source);
            if (text.Length == 0) return string.Empty;

            if (forceMonospace && charactersPerColumn > 0)
                _builder.Append($"<mspace={monospaceEm}em>");

            if (charactersPerColumn <= 0)
                AppendSingleColumn(text);
            else
                AppendColumns(text);

            return _builder.ToString();
        }

        /// <summary>공백·줄바꿈을 설정대로 정리한다. 열을 세려면 최종 글자 수가 먼저 필요하다.</summary>
        private string Clean(string source)
        {
            _cleaned.Clear();

            if (string.IsNullOrEmpty(source)) return string.Empty;

            foreach (char c in source)
            {
                if (c == '\r') continue;

                if (c == ' ' || c == '\n')
                {
                    // 원문의 줄바꿈도 띄어쓰기와 같이 친다. 그대로 흘려보내면
                    // 빈 줄이 하나 더 들어가 열의 글자 수가 어긋난다.
                    if (keepSpaces) _cleaned.Append(' ');
                    continue;
                }

                _cleaned.Append(c);
            }

            return _cleaned.ToString();
        }

        private void AppendSingleColumn(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0) _builder.Append('\n');

                _builder.Append(text[i]);
            }
        }

        /// <summary>
        /// 열을 가로로 뒤집어 붙인다.
        ///
        /// 줄 r에는 각 열의 r번째 글자가 나란히 들어간다. 그 줄들을 위에서 아래로 쌓으면
        /// 화면에서는 글자가 열마다 세로로 내려가는 것으로 보인다.
        ///
        /// 마지막 열이 짧으면 그 자리에 빈 칸을 넣는다. 건너뛰면 그 줄만 글자가 당겨져
        /// 열이 어긋난다.
        /// </summary>
        private void AppendColumns(string text)
        {
            int columnCount = Mathf.CeilToInt((float)text.Length / charactersPerColumn);

            for (int row = 0; row < charactersPerColumn; row++)
            {
                if (row > 0) _builder.Append('\n');

                for (int slot = 0; slot < columnCount; slot++)
                {
                    // 읽는 순서와 그리는 순서가 다르다.
                    // 오른쪽부터 읽는 세로쓰기는 첫 열이 화면 오른쪽 끝에 와야 한다.
                    int column = rightToLeftColumns ? columnCount - 1 - slot : slot;

                    if (slot > 0) _builder.Append(' ', columnSpacing);

                    int index = column * charactersPerColumn + row;

                    _builder.Append(index < text.Length ? text[index] : ' ');
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 인스펙터에서 원문이나 설정을 바꾸는 즉시 씬 뷰에 반영한다.
            // OnValidate에서 곧바로 TMP를 건드리면 경고가 나므로 한 박자 미룬다.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;

                Refresh();
            };
        }
#endif
    }
}
