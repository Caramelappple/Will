using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class KTH_DrawButton : MonoBehaviour
{
    [SerializeField]private Button button;

    // 드로우 요청 이벤트 (KTH_SpawnCard에서 수신)
    public event Action OnDrawRequested;

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
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        OnDrawRequested?.Invoke();
    }

    /// <summary>
    /// 덱에 카드가 없거나 드로우 불가 상태일 때 버튼 활성화 상태 전환
    /// </summary>
    public void SetInteractable(bool isInteractable)
    {
        if (button != null)
        {
            button.interactable = isInteractable;
        }
    }
}