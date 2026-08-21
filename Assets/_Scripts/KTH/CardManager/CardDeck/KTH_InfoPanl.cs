
using _Scripts.LSO.Ability;
using _Scripts.LSO.Deck.Data;
using TMPro;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.UI;

public class KTH_InfoPanl : MonoBehaviour
{
    public static KTH_InfoPanl Instance { get; private set; }

    [SerializeField] private GameObject infoPanl;

    [Header("아이콘 이미지")]
    [SerializeField] private Image icon;

    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField]private TextMeshProUGUI willExplanationText;
    [SerializeField]private TextMeshProUGUI TraitExplanationText;

    [Header("스텟 텍스트")]
    [SerializeField] private TextMeshProUGUI attackStateText;
    [SerializeField] private TextMeshProUGUI hpStateText;
    [SerializeField]private TextMeshProUGUI costStateText;
    [SerializeField] private TextMeshProUGUI moveStateText;
    [SerializeField] private TextMeshProUGUI attackTypeStateText;
    private LSO_CardSO cardData;

    public LSO_CardSO CardData => cardData;

    private void Awake()
    {
        Instance = this;
        infoPanl.SetActive(false);
    }

    public void StartInfoPanl(LSO_CardSO data)
    {
        cardData = data;
        SetPanl(data);
        infoPanl.SetActive(true);
    }

    private void SetPanl(LSO_CardSO data)
    {
        if (!data.IsValid)
        {
            Debug.LogWarning("[KTH_InfoPanl] 유효하지 않은 카드 데이터입니다.");
            return;
        }

        icon.sprite = data.Image;
        titleText.text = data.AnimalName;

        attackStateText.text = $"{data.Damage}";
        hpStateText.text = $"{data.MaxHealth}";
        costStateText.text = $"{data.Cost}";
        moveStateText.text = $"{data.Animal.MoveRange}";
        attackTypeStateText.text = $"{data.Range}";

        TraitExplanationText.text = data.Description;

        // abilities는 enum 리스트라서 설명 문자열이 따로 없음.
        // 우선 첫 번째 특성의 enum 이름만 표시 (임시)
        var abilityTypes = data.AbilityTypes;
        willExplanationText.text = abilityTypes.Count > 0
            ? GetAbilityExplanation(abilityTypes[0])
            : string.Empty;
    }

    /// <summary>
    /// LSO_AbilityType enum에 대응하는 설명 텍스트를 반환한다.
    /// TODO: 실제 특성 설명 문구가 정해지면 이 딕셔너리를 채워 넣을 것.
    /// </summary>
    private string GetAbilityExplanation(LSO_AbilityType type)
    {
        return type switch
        {
            // LSO_AbilityType.예시특성 => "예시 특성 설명",
            _ => type.ToString() // 임시: enum 이름 그대로 표시
        };
    }

    public void CancleInfoPanl()
    {
        infoPanl.SetActive(false);
    }
}