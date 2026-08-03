using UnityEngine;

// 배경에 붙이는 저비용 별가루 파티클. ParticleSystem 컴포넌트가 있는 오브젝트에 추가하면
// Reset() 시점에 권장 설정이 자동 적용됨 (파티클 수/발광 없는 플랫한 트윙클만 사용)
[RequireComponent(typeof(ParticleSystem))]
public class LDY_StarfieldParticles : MonoBehaviour
{
    [SerializeField] private LDY_MapTheme theme;

    [Header("영역 (배경 캔버스/카메라 뷰 크기에 맞춰 조절)")]
    [SerializeField] private Vector2 areaSize = new Vector2(1920f, 1080f);

    [Header("반짝임 타이밍")]
    [SerializeField] private float minLifetime = 2.5f;
    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private float minSize = 2f;
    [SerializeField] private float maxSize = 5f;

    [Header("패럴랙스 드리프트")]
    [Tooltip("0이면 완전히 정적. Theme가 지정되어 있으면 그쪽 값을 우선 사용")]
    [SerializeField] private float driftSpeed = 3f;

    private void Reset()
    {
        Configure();
    }

    [ContextMenu("Apply Recommended Settings")]
    private void Configure()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        int maxStars = theme != null ? theme.maxStars : 60;
        float driftSpeed = theme != null ? theme.starDriftSpeed : this.driftSpeed;

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.maxParticles = maxStars;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new ParticleSystem.MinMaxGradient(
            theme != null ? theme.starColorA : new Color(0.85f, 0.66f, 0.30f),
            theme != null ? theme.starColorB : Color.white);

        var emission = ps.emission;
        emission.rateOverTime = maxStars / 4f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(areaSize.x, areaSize.y, 1f);

        // 패럴랙스 느낌: 카메라 이동 없이도 아주 느리게 흐르는 미세한 드리프트 (개체마다 방향/속도가 조금씩 달라 자연스러움)
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = driftSpeed > 0f;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-driftSpeed, driftSpeed);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-driftSpeed * 0.4f, driftSpeed * 0.4f);

        // 트윙클: Sin 형태에 가깝게 여러 번 밝기가 오르내리도록 알파 키를 배치 (발광 셰이더 없이 알파만 사용)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0.45f, 0.35f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0.4f, 0.7f),
                new GradientAlphaKey(0.85f, 0.85f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        // 크기도 밝기와 함께 살짝 오르내려 반짝임을 강조
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.5f), new Keyframe(0.15f, 1f), new Keyframe(0.35f, 0.6f),
            new Keyframe(0.5f, 1f), new Keyframe(0.7f, 0.55f), new Keyframe(0.85f, 0.9f), new Keyframe(1f, 0.4f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null) renderer.sortingOrder = -10;
    }
}
