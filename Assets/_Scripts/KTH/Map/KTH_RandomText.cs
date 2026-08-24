using UnityEngine;

public class KTH_RandomText : MonoBehaviour
{
    [Header("랜덤으로 뽑을 문구 목록")]
    [TextArea]
    [SerializeField] private string[] messages;

    /// <summary>
    /// 등록된 문구 중 하나를 무작위로 반환한다.
    /// 목록이 비어있으면 빈 문자열을 반환한다.
    /// </summary>
    public string GetRandomText()
    {
        if (messages == null || messages.Length == 0)
        {
            Debug.LogWarning("[KTH_RandomText] 등록된 문구가 없습니다.", this);
            return string.Empty;
        }

        int index = Random.Range(0, messages.Length);
        return messages[index];
    }
}