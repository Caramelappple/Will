using System;
using _Scripts.LDY;
using _Scripts.LSO.UI.Input;
using _Scripts.LSO.UI.Panel;
using UnityEngine;

// 스크린샷 구조 그대로: Collider + LSO_ButtonClickHandler + (이 스크립트).
// LSO_ButtonClickHandler가 콜라이더 클릭을 감지해서
// 같은 오브젝트에 붙은 LSO_IClickEffect 전부(이 스크립트 포함)에게 OnClick()을 전달한다.
// 씬에 EventSystem + 카메라에 Physics Raycaster가 있어야 3D 클릭이 감지된다.
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(LSO_ButtonClickHandler))]
public class KTH_DrawButton : MonoBehaviour, LSO_IClickEffect
{
    [SerializeField] private LSO_WillPanel willPanel;
    [SerializeField] private LDY_TurnManager turnManager;

    [Header("비활성 시 시각 표시 (선택)")]
    [Tooltip("드로우 불가 상태일 때 반투명 처리할 렌더러들. 비워두면 시각 처리는 하지 않고 콜라이더만 꺼진다.")]
    [SerializeField] private Renderer[] visualRenderers;
    [SerializeField] private float disabledAlpha = 0.4f;

    private Collider _collider;

    // 드로우 요청 이벤트 (KTH_SpawnCard에서 수신)
    public event Action OnDrawRequested;

    // 덱 상태(카드 없음 등)로 인한 활성/비활성 여부와
    // 턴 상태로 인한 활성/비활성 여부를 따로 추적해서
    // 둘 다 만족할 때만 실제로 클릭이 먹히게 한다.
    private bool _isDeckInteractable = true;
    private bool _isPlayerTurn = true;

    public bool IsInteractable =>
        _isDeckInteractable && _isPlayerTurn;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
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

    /// <summary>
    /// LSO_ButtonClickHandler가 콜라이더 클릭을 감지하면 호출한다.
    /// </summary>
    public void OnClick()
    {
        if (willPanel != null &&
            willPanel.IsSelecting)
        {
            return;
        }

        // 내 턴이 아니면 드로우 요청을 보내지 않음
        if (turnManager != null &&
            turnManager.CurrentTurn != LDY_Team.Player)
        {
            return;
        }

        OnDrawRequested?.Invoke();
    }

    /// <summary>
    /// 덱에 카드가 없거나 드로우 불가 상태일 때 활성 상태 전환
    /// </summary>
    public void SetInteractable(bool isInteractable)
    {
        _isDeckInteractable = isInteractable;

        RefreshInteractable();
    }

    /// <summary>
    /// 덱 상태와 턴 상태를 함께 고려해 실제 클릭 가능 여부를 갱신한다.
    ///
    /// LSO_ButtonClickHandler의 "Respect Interactable" 옵션은
    /// UI.Selectable 기준이라 3D 콜라이더 오브젝트에는 적용되지 않는다.
    /// 그래서 여기서는 콜라이더 자체를 켜고 끄는 방식으로 클릭을 막는다.
    /// </summary>
    private void RefreshInteractable()
    {
        if (_collider != null)
        {
            _collider.enabled = IsInteractable;
        }

        if (visualRenderers == null)
        {
            return;
        }

        foreach (Renderer r in visualRenderers)
        {
            if (r == null)
            {
                continue;
            }

            Material mat = r.material;

            if (!mat.HasProperty("_Color"))
            {
                continue;
            }

            Color c = mat.color;
            c.a = IsInteractable ? 1f : disabledAlpha;
            mat.color = c;
        }
    }
}
