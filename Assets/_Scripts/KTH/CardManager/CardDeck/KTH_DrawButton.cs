using System;
using _Scripts.LDY;
using _Scripts.LSO.UI;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class KTH_DrawButton : MonoBehaviour
{
    [SerializeField]private Button button;
    [SerializeField]private LSO_WillPanel willPanel;
    [SerializeField]private LDY_TurnManager turnManager;

    // 드로우 요청 이벤트 (KTH_SpawnCard에서 수신)
    public event Action OnDrawRequested;

    // 덱 상태(카드 없음 등)로 인한 활성/비활성 여부와
    // 턴 상태로 인한 활성/비활성 여부를 따로 추적해서
    // 둘 다 만족할 때만 버튼을 실제로 눌리게 한다.
    private bool _isDeckInteractable = true;
    private bool _isPlayerTurn = true;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }

        if (turnManager != null)
        {
            turnManager.OnTurnChanged += HandleTurnChanged;

            _isPlayerTurn =
                turnManager.CurrentTurn == LDY_Team.Player;
        }

        RefreshInteractable();
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }

        if (turnManager != null)
        {
            turnManager.OnTurnChanged -= HandleTurnChanged;
        }
    }

    private void HandleTurnChanged(LDY_Team team)
    {
        _isPlayerTurn = team == LDY_Team.Player;

        RefreshInteractable();
    }

    private void HandleClick()
    {
        if (willPanel.IsSelecting)
            return;

        // 내 턴이 아니면 드로우 요청을 보내지 않음
        if (turnManager != null &&
            turnManager.CurrentTurn != LDY_Team.Player)
        {
            return;
        }

        OnDrawRequested?.Invoke();
    }

    /// <summary>
    /// 덱에 카드가 없거나 드로우 불가 상태일 때 버튼 활성화 상태 전환
    /// </summary>
    public void SetInteractable(bool isInteractable)
    {
        _isDeckInteractable = isInteractable;

        RefreshInteractable();
    }

    /// <summary>
    /// 덱 상태와 턴 상태를 함께 고려해 버튼의 실제 interactable을 갱신한다.
    /// </summary>
    private void RefreshInteractable()
    {
        if (button == null)
        {
            return;
        }

        button.interactable =
            _isDeckInteractable && _isPlayerTurn;
    }
}