/*
using System.Collections;
using _Scripts.LDY;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(LDY_Animal))]
public class DLJ_WillSystem : MonoBehaviour, DLJ_IWillActivation
{
    public enum WillType
    {
        Curse,
        Rage,
        Succession
    }
    
    [SerializeField] private LSO_AnimalSO animalSo;
    
    [SerializeField] private LDY_BoardManager board;
    
    [SerializeField] private LDY_TurnManager turnManager;

    [SerializeField] private LDY_AttackSystem attackSystem;
    
    [FormerlySerializedAs("Will")]
    [SerializeField] private WillType willType;

    [Header("Rage")] 
    [SerializeField] private GameObject rageObject;
    [SerializeField] private float rageExpandTime = 0.25f;
    [SerializeField] private float rageHoldTime = 0.3f;
    [SerializeField] private float effectHeight = 0.12f;
    private Sequence rageSequence;
    
    [Header("Curse")]
    [SerializeField] private GameObject curseObject;
    [SerializeField] private float curseExpandTime = 0.25f;
    [SerializeField] private float curseHoldTime = 3f;
    [SerializeField] private float curseEffectHeight = 0.12f;
    private Sequence curseSequence;

    [Header("Succession")]
    [SerializeField] private GameObject SuccesionObject;
    private static bool isWaitingForSuccessionTarget;
    private static bool isCompletingSuccession;
    private static LDY_Team successionTeam;
    private static int successionHealthBonus;
    private static int successionAttackBonus;
    private static float timeScaleBeforeSuccession = 1f;
    private static DLJ_WillSystem successionSource;
    private LDY_Animal animal;

    public static bool IsWaitingForSuccessionTarget =>
        isWaitingForSuccessionTarget && !isCompletingSuccession;

    public bool ShouldDeferDestruction =>
        willType == WillType.Succession &&
        isWaitingForSuccessionTarget &&
        successionSource == this;

    [SerializeField] private GameObject testObject;

    private void Awake()
    {
        animal = GetComponent<LDY_Animal>();

        if (board == null)
            board = FindFirstObjectByType<LDY_BoardManager>();

        if (willType == WillType.Succession && SuccesionObject != null)
            SuccesionObject.SetActive(false);
    }

    public void WillActivate()
    {
        if (animalSo == null && willType == WillType.Succession)
        {
            Debug.LogError($"{name}: AnimalSo가 비어 있음", this);
            return;
        }

        if (isWaitingForSuccessionTarget)
        {
            TrySelectSuccessionTarget();
            return;
        }

        switch (willType)
        {
            case WillType.Curse:
                ActivateCurse();
                break;

            case WillType.Rage:
                ActivateRage();
                break;

            case WillType.Succession:
                BeginSuccession();
                break;
        }
    }

    private void ActivateCurse()
    {
        if (board == null)
        {
            Debug.LogError("보드 없음");
            return;
        }

        if (curseObject == null)
        {
            Debug.LogError("오브젝트 없음");
            return;
        }
        
        Vector3Int center = board.WorldToGrid(transform.position);
        
        if (!board.IsInside(center))
        {
            Debug.LogError("기물이 보드 밖에 있음");
            return;
        }
        
        //월드 좌표 -> 그리드
        Vector3 centerWorld = board.GridToWorld(center);
        Vector3 verticalWorld = board.GridToWorld(center + new Vector3Int(0, 0, 1));
        Vector3 horizontalWorld = board.GridToWorld(center + new Vector3Int(1, 0, 0));
        
        GameObject curseInstance = Instantiate(
            curseObject,
            centerWorld,
            Quaternion.identity
        );
        
        //한칸의 크기 구하기
        float cellWidth = Vector3.Distance(centerWorld, verticalWorld);
        float cellDepth = Vector3.Distance(centerWorld, horizontalWorld);
        
        //이펙트 크기 구하기
        Vector3 targetScale = new Vector3(cellWidth * 3, effectHeight, cellDepth * 3);
        
        //색 바꾸기
        //Renderer render = curseObject.GetComponent<Renderer>();
        //render.material.color = Color.purple;
        
        //크기 초기화
        curseInstance.transform.position = centerWorld + Vector3.up * (effectHeight * 0.5f);
        curseInstance.transform.localScale = Vector3.zero;
        
        curseObject.SetActive(true);
        
        DLJ_CurseSystem curseSystem = curseInstance.GetComponent<DLJ_CurseSystem>();
        
        if (turnManager != null)
            curseSystem.Initialize(turnManager, board, center, attackSystem);
        
        //이펙트
        curseSequence = DOTween.Sequence()
            .Append(curseInstance.transform.DOScale(targetScale, curseExpandTime).SetEase(Ease.Linear));
        Debug.Log("Curse Activated");
    }

    private void ActivateRage()
    {
        if (board == null)
        {
            Debug.LogError("보드 없음");
            return;
        }

        if (rageObject == null)
        {
            Debug.LogError("오브젝트 없음");
            return;
        }
        
        Vector3Int center = board.WorldToGrid(transform.position);
        
        if (!board.IsInside(center))
        {
            Debug.LogError("기물이 보드 밖에 있음");
            return;
        }

        //월드 좌표 -> 그리드
        Vector3 centerWorld = board.GridToWorld(center);
        Vector3 verticalWorld = board.GridToWorld(center + new Vector3Int(0, 0, 1));
        Vector3 horizontalWorld = board.GridToWorld(center + new Vector3Int(1, 0, 0));
        
        GameObject rageInstance = Instantiate(
            rageObject,
            centerWorld,
            Quaternion.identity
        );
        
        //한칸의 크기 구하기
        float cellWidth = Vector3.Distance(centerWorld, verticalWorld);
        float cellDepth = Vector3.Distance(centerWorld, horizontalWorld);
        
        //이펙트 크기 구하기
        Vector3 targetScale = new Vector3(cellWidth * 3, effectHeight, cellDepth * 3);
        
        //크기 초기화
        rageInstance.transform.position = centerWorld + Vector3.up * (effectHeight * 0.5f);
        rageInstance.transform.localScale = Vector3.zero;
        
        rageInstance.SetActive(true);
        
        DLJ_RageSystem curseSystem = rageInstance.GetComponent<DLJ_RageSystem>();
        
        if (turnManager != null)
            curseSystem.Initialize(turnManager, board, center, attackSystem);
        
        //이펙트
        rageSequence = DOTween.Sequence()
            .Append(rageInstance.transform.DOScale(targetScale, rageExpandTime).SetEase(Ease.Linear))
            .AppendInterval(rageHoldTime)
            .Append(rageInstance.transform.DOScale(Vector3.zero, rageExpandTime).SetEase(Ease.Linear))
            .OnComplete(() => rageInstance.SetActive(false));
        Debug.Log("Rage Activated");
    }

    private void BeginSuccession()
    {
        if (isWaitingForSuccessionTarget)
            return;

        successionSource = this;
        successionTeam = animal.team;
        successionHealthBonus = animalSo != null ? animalSo.maxHealth : 1;
        successionAttackBonus = animalSo != null ? animalSo.damage : animal.baseAtk;
        timeScaleBeforeSuccession = Time.timeScale;
        isCompletingSuccession = false;
        isWaitingForSuccessionTarget = true;

        if (SuccesionObject != null)
        {
            SuccesionObject.transform.DOKill();
            SuccesionObject.transform.position = transform.position;
            SuccesionObject.SetActive(true);
        }

        Time.timeScale = 0f;
        Debug.Log("Pick Target");
    }

    public bool TrySelectSuccessionTarget()
    {
        if (!IsWaitingForSuccessionTarget || animal == null || animal.IsDead)
        {
            Debug.LogWarning("Failed");
            return false;
        }

        if (animal.team != successionTeam)
        {
            Debug.LogError("No Target");
            return false;
        }

        CompleteSuccession(this);
        return true;
    }

    private static void CompleteSuccession(DLJ_WillSystem target)
    {
        isCompletingSuccession = true;

        GameObject successionEffect =
            successionSource != null ? successionSource.SuccesionObject : null;

        if (successionEffect == null)
        {
            Debug.LogWarning("Succession effect is missing. Applying the bonus immediately.");
            ApplySuccession(target);
            return;
        }

        successionEffect.transform.DOMove(target.transform.position, 1f)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() => ApplySuccession(target));

        Debug.Log("Succession Finished");
    }

    private static void ApplySuccession(DLJ_WillSystem target)
    {
        DLJ_WillSystem sourceToDestroy = successionSource;

        if (target != null && target.animal != null && !target.animal.IsDead)
        {
            target.animal.hp += successionHealthBonus;
            target.animal.baseAtk += successionAttackBonus;
        }

        if (successionSource != null && successionSource.SuccesionObject != null)
            successionSource.SuccesionObject.SetActive(false);

        isWaitingForSuccessionTarget = false;
        isCompletingSuccession = false;
        successionSource = null;
        Time.timeScale = timeScaleBeforeSuccession;

        if (sourceToDestroy != null)
            Destroy(sourceToDestroy.gameObject);
    }
    
    
}
*/
