using System;
using UnityEngine;

namespace _Scripts.LDY.Stage
{
    /// <summary>
    /// 스테이지 시작을 지휘하기만 한다. 적을 놓거나 자원을 정하는 등의 실제 작업은 전혀 하지 않고,
    /// 같은 오브젝트에 붙어 있는 LDY_IStageSetupStep 구현들을 위에서부터 차례로 호출한다.
    /// 준비 절차를 늘리고 싶으면 새 스텝 컴포넌트를 붙이면 되며 이 클래스는 고칠 필요가 없다.
    ///
    /// 씬 배선:
    /// - 이 컴포넌트와 스텝 컴포넌트들을 같은 오브젝트(예: LDY_StageDirector)에 붙일 것.
    /// - 실행 순서는 인스펙터의 컴포넌트 순서를 따르므로 BoardClear → EnemyPlacement → 규칙 스텝 순으로 정렬할 것.
    /// - defaultStage에 테스트용 스테이지를 넣어두면 그 씬을 단독 실행할 때 쓰인다.
    ///   맵에서 LDY_StageSelection.Select()로 골라 넘어온 경우에는 그쪽이 우선한다.
    /// </summary>
    public class LDY_StageDirector : MonoBehaviour
    {
        [SerializeField] private LDY_StageSO defaultStage;
        [SerializeField] private bool loadOnStart = true;

        public LDY_StageSO CurrentStage { get; private set; }
        public event Action<LDY_StageSO> OnStageLoaded;

        private LDY_IStageSetupStep[] _steps;

        private void Awake()
        {
            _steps = GetComponents<LDY_IStageSetupStep>();

            if (_steps.Length == 0)
                Debug.LogWarning($"{name}: 스테이지 준비 스텝이 하나도 붙어 있지 않습니다.", this);
        }

        private void Start()
        {
            if (!loadOnStart) return;

            LDY_StageSO stage = LDY_StageSelection.Consume() ?? defaultStage;
            if (stage == null)
            {
                Debug.LogWarning($"{name}: 시작할 스테이지가 없습니다. defaultStage를 지정하거나 LDY_StageSelection.Select()를 호출하세요.", this);
                return;
            }

            LoadStage(stage);
        }

        /// <summary>스테이지를 실제로 시작한다. 같은 씬에서 다음 스테이지로 넘어갈 때도 이 메서드를 부르면 된다.</summary>
        public void LoadStage(LDY_StageSO stage)
        {
            if (stage == null)
            {
                Debug.LogWarning($"{name}: null 스테이지는 시작할 수 없습니다.", this);
                return;
            }

            CurrentStage = stage;

            foreach (LDY_IStageSetupStep step in _steps)
                step.Setup(stage);

            OnStageLoaded?.Invoke(stage);
        }
    }
}
