using System;
using System.Collections;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

/// <summary>
/// 더블클릭 이벤트가 정상적으로 전달되는지 확인하는 간이 테스트 구독자.
/// 실제 표시 담당 코드가 완성되면 이 컴포넌트는 제거한다.
/// </summary>
public sealed class DLJ_InfoPanelDoubleClickTest : MonoBehaviour
{
    [Tooltip("테스트할 인포창. 비워두면 DLJ_InfoPanel.Instance를 사용한다.")]
    [SerializeField] private DLJ_InfoPanel infoPanel;

    private UnityEngine.Object _displayedTarget;
    private Coroutine _switchRoutine;

    private void OnEnable()
    {
        DLJ_InfoPanelEvents.PieceDoubleClicked += HandlePieceDoubleClicked;
        DLJ_InfoPanelEvents.CardDoubleClicked += HandleCardDoubleClicked;
    }

    private void OnDisable()
    {
        DLJ_InfoPanelEvents.PieceDoubleClicked -= HandlePieceDoubleClicked;
        DLJ_InfoPanelEvents.CardDoubleClicked -= HandleCardDoubleClicked;

        if (_switchRoutine != null)
        {
            StopCoroutine(_switchRoutine);
            _switchRoutine = null;
        }
    }

    private void HandlePieceDoubleClicked(LDY_Animal unit)
    {
        DLJ_InfoPanel panel = ResolvePanel();
        if (panel == null)
        {
            Debug.LogWarning("[DLJ_InfoPanelDoubleClickTest] DLJ_InfoPanel을 찾을 수 없습니다.", this);
            return;
        }

        ShowOrSwitch(panel, unit, () => panel.Show(unit));
    }

    private void HandleCardDoubleClicked(LSO_CardSO card)
    {
        DLJ_InfoPanel panel = ResolvePanel();
        if (panel == null)
        {
            Debug.LogWarning("[DLJ_InfoPanelDoubleClickTest] DLJ_InfoPanel을 찾을 수 없습니다.", this);
            return;
        }

        ShowOrSwitch(panel, card, () => panel.Show(card));
    }

    private void ShowOrSwitch(
        DLJ_InfoPanel panel,
        UnityEngine.Object target,
        Action showTarget)
    {
        if (_switchRoutine != null)
        {
            StopCoroutine(_switchRoutine);
            _switchRoutine = null;
        }

        bool canShowImmediately =
            _displayedTarget == null ||
            _displayedTarget == target ||
            panel.IsHidden;

        if (canShowImmediately)
        {
            showTarget();
            _displayedTarget = target;
            return;
        }

        _switchRoutine = StartCoroutine(SwitchTarget(panel, target, showTarget));
    }

    private IEnumerator SwitchTarget(
        DLJ_InfoPanel panel,
        UnityEngine.Object target,
        Action showTarget)
    {
        panel.Hide();

        while (panel != null && !panel.IsHidden)
            yield return null;

        if (panel == null)
        {
            _switchRoutine = null;
            yield break;
        }

        showTarget();
        _displayedTarget = target;
        _switchRoutine = null;
    }

    private DLJ_InfoPanel ResolvePanel() =>
        infoPanel != null ? infoPanel : DLJ_InfoPanel.Instance;
}
