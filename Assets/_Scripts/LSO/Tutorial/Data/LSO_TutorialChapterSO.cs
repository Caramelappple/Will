using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LSO.Tutorial.Data
{
    /// <summary>
    /// 걸음 묶음 하나. 기획서의 "1. 게임 설명" 같은 단위다.
    ///
    /// 챕터로 나누는 이유는 건너뛰기와 테스트 때문이다. 만들다 보면
    /// "4번만 다시 보고 싶다"가 반복되는데, 걸음이 한 목록에 다 있으면 그걸 할 수 없다.
    /// </summary>
    [CreateAssetMenu(fileName = "TutChapter", menuName = "SO/Tutorial/Chapter")]
    public sealed class LSO_TutorialChapterSO : ScriptableObject
    {
        [Tooltip("콘솔에 찍을 이름. \"3. 덱\" 처럼.")]
        public string title;

        public List<LSO_TutorialStepSO> steps = new();
    }
}
