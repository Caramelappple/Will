using System;
using System.Collections;
using _Scripts.LDY;
using _Scripts.LSO;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class DLJ_WillSystem : MonoBehaviour, DLJ_IWillActivation
{
    [SerializeField] private LSO_AnimalSO animalSo;
    
    [SerializeField] private LDY_BoardManager board;

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
    private static DLJ_WillSystem successionSource;
    
    [SerializeField] private GameObject testObject;

    public void WillActivate()
    {
        if (animalSo == null)
        {
            Debug.LogError($"{name}: AnimalSo가 비어 있음", this);
            return;
        }

        if (successionSource != null)
        {
            successionSource.CompleteSuccession(this);
            successionSource = null;
            return;
        }

        /*switch (animalSo.willType)
        {
            case LSO_WillType.Curse:
                ActivateCurse();
                break;

            case LSO_WillType.Rage:
                ActivateRage();
                break;

            case LSO_WillType.Succession:
                BeginSuccession();
                break;
        }*/
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
        
        //한칸의 크기 구하기
        float cellWidth = Vector3.Distance(centerWorld, verticalWorld);
        float cellDepth = Vector3.Distance(centerWorld, horizontalWorld);
        
        //이펙트 크기 구하기
        Vector3 targetScale = new Vector3(cellWidth * 3, effectHeight, cellDepth * 3);
        
        //색 바꾸기
        Renderer render = curseObject.GetComponent<Renderer>();
        render.material.color = Color.purple;
        
        //크기 초기화
        curseObject.transform.position = centerWorld + Vector3.up * (effectHeight * 0.5f);
        curseObject.transform.localScale = Vector3.zero;
        
        curseObject.SetActive(true);
        
        //이펙트
        curseSequence = DOTween.Sequence()
            .Append(curseObject.transform.DOScale(targetScale, curseExpandTime).SetEase(Ease.Linear))
            .AppendInterval(curseHoldTime)
            .Append(curseObject.transform.DOScale(Vector3.zero, curseExpandTime).SetEase(Ease.Linear))
            .OnComplete(() => curseObject.SetActive(false));
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
        
        //한칸의 크기 구하기
        float cellWidth = Vector3.Distance(centerWorld, verticalWorld);
        float cellDepth = Vector3.Distance(centerWorld, horizontalWorld);
        
        //이펙트 크기 구하기
        Vector3 targetScale = new Vector3(cellWidth * 3, effectHeight, cellDepth * 3);
        
        //크기 초기화
        rageObject.transform.position = centerWorld + Vector3.up * (effectHeight * 0.5f);
        rageObject.transform.localScale = Vector3.zero;
        
        rageObject.SetActive(true);
        
        //이펙트
        rageSequence = DOTween.Sequence()
            .Append(rageObject.transform.DOScale(targetScale, rageExpandTime).SetEase(Ease.Linear))
            .AppendInterval(rageHoldTime)
            .Append(rageObject.transform.DOScale(Vector3.zero, rageExpandTime).SetEase(Ease.Linear))
            .OnComplete(() => rageObject.SetActive(false));
        Debug.Log("Rage Activated");
    }

    private void BeginSuccession()
    {
        successionSource = this;
        Debug.Log("Pick Target");
    }

    private void CompleteSuccession(DLJ_WillSystem target)
    {
        if (target == this || !target.CompareTag("Ally"))
        {
            Debug.LogWarning("Failed");
            return;
        }

        if (target.animalSo == null)
        {
            Debug.LogError("No Target");
            return;
        }

        target.animalSo.maxHealth += animalSo.maxHealth;
        target.animalSo.damage += animalSo.damage;

        animalSo.maxHealth = 0;
        animalSo.damage = 0;

        Debug.Log("Succession Finished");
    }

    private IEnumerator CurseAnimation()
    {
        GameObject effectObj = Instantiate(testObject, gameObject.transform.position, Quaternion.identity);
        Renderer render = effectObj.GetComponent<Renderer>();
        render.material.color = Color.purple;
        testObject.transform.DOScale(new Vector3(4.3f, 0.12f, 4.3f), 0.5f);
        yield return new WaitForSeconds(3f);
        testObject.transform.DOScale(Vector3.zero, 0.5f);
    }
}
