using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기물이 죽을 때 서서히 파편화되며 사라지는 연출.
/// 기물 프리팹 루트에 붙이고, 사망 처리 시점에 Play() 를 호출한다.
///
/// 기존 머티리얼의 셰이더만 LDY/Dissolve 로 갈아끼우는 방식이라
/// _BaseMap / _BaseColor / _Metallic / _Smoothness 등 URP Lit 과
/// 이름이 같은 속성은 그대로 유지된다. 머티리얼을 새로 만들 필요 없음.
/// </summary>
public class LDY_DissolveEffect : MonoBehaviour
{
    [Header("타이밍")]
    [SerializeField] private float duration = 1.0f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("유언/계승 시스템이 timeScale 을 0 으로 세워도 연출이 재생되도록 함")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("완료 처리")]
    [SerializeField] private bool destroyOnComplete = true;

    [Header("셰이더")]
    [Tooltip("비워두면 Shader.Find(\"LDY/Dissolve\") 로 찾는다")]
    [SerializeField] private Shader dissolveShader;

    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

    /// <summary>현재 재생 중인 디졸브 개수. 턴 매니저의 애니메이션 대기 판정에 사용.</summary>
    public static int ActiveCount { get; private set; }

    public bool IsPlaying { get; private set; }

    // 도메인 리로드를 끈 플레이 모드에서 값이 남으면 LDY_TurnManager.IsAnimating()이
    // 영원히 true가 되어 턴이 넘어가지 않는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        ActiveCount = 0;
    }

    /// <summary>
    /// 대상에 디졸브를 붙여 재생한다. 컴포넌트가 없으면 붙인다.
    /// 즉시 Destroy 대신 이것을 부르면 destroyOnComplete가 연출이 끝난 뒤에 파괴한다.
    /// 비활성 오브젝트는 코루틴을 돌릴 수 없으므로 예전처럼 즉시 파괴한다.
    /// </summary>
    public static LDY_DissolveEffect PlayOn(GameObject target, Action onComplete = null)
    {
        if (target == null) return null;

        if (!target.activeInHierarchy)
        {
            onComplete?.Invoke();
            Destroy(target);
            return null;
        }

        if (!target.TryGetComponent(out LDY_DissolveEffect effect))
            effect = target.AddComponent<LDY_DissolveEffect>();

        effect.Play(onComplete);
        return effect;
    }

    private readonly List<Material> _materials = new List<Material>();
    private Coroutine _routine;

    /// <summary>
    /// 꺼지면 코루틴이 죽는다. 파괴가 아니라서 OnDestroy도 오지 않는다.
    ///
    /// 여기서 놓지 않으면 ActiveCount가 1 남고, 그 판 내내
    /// LDY_TurnManager.IsAnimating()이 true라 **턴을 넘길 수 없게 된다.**
    ///
    /// 실제로 이 길로 물렸다 — LSO_BoardRiders.Settle(hide) 가 판을 뒤집은 뒤
    /// 지난 판 기물을 끄는데, 회전 대기가 상한(3초)에 걸리면 아직 녹는 중인 기물이
    /// 그대로 꺼진다.
    /// </summary>
    private void OnDisable()
    {
        Release();
    }

    private void OnDestroy()
    {
        Release();

        foreach (Material m in _materials)
        {
            if (m != null) Destroy(m);
        }
        _materials.Clear();
    }

    /// <summary>
    /// 붙잡고 있던 카운트를 놓는다. 두 번 불려도 한 번만 놓는다.
    ///
    /// 코루틴도 끊는다. 안 끊으면 다시 켜졌을 때 옛 코루틴이 살아 돌아와
    /// 카운트를 한 번 더 깎는다.
    /// </summary>
    private void Release()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (!IsPlaying) return;

        IsPlaying = false;
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
    }

    /// <summary>디졸브를 시작한다. onComplete 는 연출이 끝난 뒤(오브젝트 파괴 전) 호출된다.</summary>
    public void Play(Action onComplete = null)
    {
        if (IsPlaying) return;

        Shader shader = dissolveShader != null ? dissolveShader : Shader.Find("LDY/Dissolve");
        if (shader == null)
        {
            Debug.LogError("[LDY_DissolveEffect] LDY/Dissolve 셰이더를 찾지 못했습니다. " +
                           "빌드에 포함되도록 Always Included Shaders 에 등록하거나 " +
                           "인스펙터에서 직접 지정하세요.", this);
            onComplete?.Invoke();
            if (destroyOnComplete) Destroy(gameObject);
            return;
        }

        SwapShaders(shader);

        IsPlaying = true;
        ActiveCount++;
        _routine = StartCoroutine(Co_Dissolve(onComplete));
    }

    private void SwapShaders(Shader shader)
    {
        _materials.Clear();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            // .materials 접근 시점에 렌더러별 인스턴스가 생성되므로
            // 다른 기물의 머티리얼에는 영향이 없다.
            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;

                if (m.shader != shader) m.shader = shader;
                m.SetFloat(DissolveAmountId, 0f);
                _materials.Add(m);
            }
            r.materials = mats;
        }
    }

    private IEnumerator Co_Dissolve(Action onComplete)
    {
        float elapsed = 0f;
        float length  = Mathf.Max(0.0001f, duration);

        while (elapsed < length)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float t = curve.Evaluate(Mathf.Clamp01(elapsed / length));
            for (int i = 0; i < _materials.Count; i++)
            {
                if (_materials[i] != null) _materials[i].SetFloat(DissolveAmountId, t);
            }

            yield return null;
        }

        for (int i = 0; i < _materials.Count; i++)
        {
            if (_materials[i] != null) _materials[i].SetFloat(DissolveAmountId, 1f);
        }

        IsPlaying = false;
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
        _routine = null;

        onComplete?.Invoke();

        if (destroyOnComplete) Destroy(gameObject);
    }
}
