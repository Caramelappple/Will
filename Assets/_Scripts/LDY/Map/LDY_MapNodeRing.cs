using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 맵 노드를 선택했을 때 해당 노드 위에 원이 그려지는 연출.
/// 링은 씬에 하나만 두고 선택된 노드 위치로 옮겨가며 재사용한다.
/// (원이 남지 않는 일회성 연출이라 노드마다 머티리얼을 뜰 필요가 없음)
/// </summary>
[RequireComponent(typeof(Image))]
public class LDY_MapNodeRing : MonoBehaviour
{
    [Header("링 크기")]
    [Tooltip("노드 크기 대비 '원의 지름' 배율. 1.55면 노드보다 1.55배 큰 원이 노드를 감싼다.\n" +
             "rect 배율이 아니라 실제로 보이는 원의 지름 기준이다.")]
    [SerializeField, Min(0.1f)] private float ringScale = 1.55f;

    [Header("그려지는 연출")]
    [Tooltip("원 한 바퀴를 다 긋는 데 걸리는 시간(초).")]
    [SerializeField] private float drawDuration = 0.5f;
    [SerializeField] private Ease  drawEase     = Ease.OutQuad;

    [Tooltip("켜면 클릭할 때마다 손떨림 모양이 바뀐다. 매번 새로 그린 원처럼 보인다.")]
    [SerializeField] private bool randomizeSeed = true;

    [Header("사라지는 연출")]
    [Tooltip("다 그려진 뒤 페이드아웃까지 대기 시간")]
    [SerializeField] private float holdDuration = 0.05f;
    [SerializeField] private float fadeDuration = 0.15f;

    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int RadiusId   = Shader.PropertyToID("_Radius");
    private static readonly int SeedId     = Shader.PropertyToID("_Seed");

    /// <summary>머티리얼에 _Radius가 없을 때 쓰는 값. 셰이더 기본값과 맞춰둔다.</summary>
    private const float FallbackRadius = 0.4f;

    private Image         _image;
    private RectTransform _rect;
    private Material      _material;
    private Sequence      _sequence;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rect  = (RectTransform)transform;

        // 인스펙터에 꽂힌 머티리얼을 직접 건드리면 에셋이 오염되므로 복제해서 사용
        _material      = new Material(_image.material);
        _image.material = _material;

        _image.raycastTarget = false;

        // GameObject 를 끄면 이벤트 구독(OnEnable)이 안 걸리므로 Image 만 끈다
        _image.enabled = false;
    }

    private void OnEnable()
    {
        LDY_MapNodeView.NodeSelected += HandleNodeSelected;
    }

    private void OnDisable()
    {
        LDY_MapNodeView.NodeSelected -= HandleNodeSelected;
    }

    private void OnDestroy()
    {
        _sequence?.Kill();
        if (_material != null) Destroy(_material);
    }

    private void HandleNodeSelected(LDY_MapNodeView node)
    {
        if (node == null) return;
        Play(node.transform as RectTransform);
    }

    /// <summary>선택된 노드 위에 링을 그린다.</summary>
    public void Play(RectTransform target)
    {
        if (target == null) return;

        _sequence?.Kill();

        _rect.position  = target.position;
        _rect.sizeDelta = CalculateSizeDelta(target);

        _material.SetFloat(ProgressId, 0f);

        // 클릭할 때마다 손떨림 위상을 바꿔서 매번 다른 원이 그려지게 한다
        if (randomizeSeed) _material.SetFloat(SeedId, Random.Range(0f, 10f));

        Color c = _image.color;
        c.a          = 1f;
        _image.color = c;

        _image.enabled = true;

        // SetLink: 씬 전환 중 트윈이 살아남아 DOTween 경고를 내는 것 방지
        // SetUpdate(true): 유언/계승 시스템이 timeScale 을 0 으로 세워도 연출은 재생되도록
        _sequence = DOTween.Sequence()
                           .SetLink(gameObject)
                           .SetUpdate(true);

        _sequence.Append(
            DOTween.To(() => _material.GetFloat(ProgressId),
                       v  => _material.SetFloat(ProgressId, v),
                       1f, drawDuration)
                   .SetEase(drawEase));

        _sequence.AppendInterval(holdDuration);
        _sequence.Append(_image.DOFade(0f, fadeDuration));
        _sequence.OnComplete(() => _image.enabled = false);
    }

    /// <summary>
    /// 노드를 ringScale 배로 감싸는 원이 나오도록 링 rect 크기를 구한다.
    ///
    /// 두 번 어긋날 자리가 있어서 그냥 rect.size를 쓰면 안 된다.
    /// 1) 노드 프리팹 루트에 localScale 1.2가 걸려 있다. rect.size(80)는 화면에 보이는 크기가 아니다.
    /// 2) 셰이더는 rect 안에서 반지름 _Radius(UV 기준, 기본 0.4)로 원을 그린다.
    ///    즉 원의 지름은 rect의 2*_Radius(=0.8)배라서, rect를 그만큼 되레 키워줘야 한다.
    ///    손떨림/겹침(_Wobble, _Drift) 때문에 실제 지름은 여기서 구한 값보다 몇 % 크다. 눈대중용이다.
    ///
    /// 링과 노드가 같은 부모(NodeContainer) 아래에 있다는 전제다. 부모가 다르고 스케일도 다르면
    /// 로컬 단위가 달라져 크기가 어긋난다.
    /// </summary>
    private Vector2 CalculateSizeDelta(RectTransform target)
    {
        Vector3 targetScale = target.localScale;

        Vector2 nodeSize = new Vector2(
            target.rect.size.x * Mathf.Abs(targetScale.x),
            target.rect.size.y * Mathf.Abs(targetScale.y));

        float radius = _material.HasProperty(RadiusId)
            ? _material.GetFloat(RadiusId)
            : FallbackRadius;

        // _Radius가 0이면 0으로 나눈다. 머티리얼이 링 셰이더가 아닐 때 대비.
        if (radius < 0.01f) radius = FallbackRadius;

        return nodeSize * (ringScale / (2f * radius));
    }

    /// <summary>연출을 즉시 중단하고 링을 감춘다.</summary>
    public void Stop()
    {
        _sequence?.Kill();
        _image.enabled = false;
    }
}
