using _Scripts.LDY;
using _Scripts.LSO.Manager;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>조개 깨기 발동에만 재생되는 연출 설정. 회복 판정은 AllHeal이 담당한다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LDY_Animal))]
public sealed class DLJ_OtterShellEffect : MonoBehaviour
{
    [Header("조개 에셋 (비워두어도 이후 연출은 재생)")]
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private Vector3 shellScale = Vector3.one;
    [SerializeField] private Vector3 shellRotation;
    [SerializeField] private Vector3 spawnOffset;
    [SerializeField] private Vector3 spinDegreesPerSecond = new Vector3(0f, 300f, 120f);

    [Header("상승 / 공중 폭발")]
    [SerializeField, Min(0.01f)] private float riseDuration = 0.55f;
    [SerializeField, Min(0f)] private float riseHeight = 1.6f;
    [SerializeField] private GameObject burstPrefab;
    [SerializeField, Min(0.01f)] private float burstScale = 0.25f;

    [Header("보드 전체 회복 이펙트 (LSO Heal)")]
    [SerializeField] private GameObject sacrificePrefab;
    [SerializeField, Min(0f)] private float boardBurstDelay = 0.12f;
    [Tooltip("원본 이펙트의 가로 폭. 보드 한 변의 실제 길이에 맞춰 확대")]
    [SerializeField, Min(0.01f)] private float sacrificeReferenceWidth = 1.25f;
    [Tooltip("보드용 입자의 최대 지름을 타일 크기에 대한 비율로 제한")]
    [SerializeField, Range(0.05f, 1f)] private float boardParticleSizeInCells = 0.65f;
    [Tooltip("보드 전체에 두 차례로 나누어 방출하는 총 입자 수")]
    [SerializeField, Range(16, 192)] private int boardParticleCount = 96;
    [SerializeField, Range(1f, 2f)] private float boardParticleBrightness = 1.15f;
    [SerializeField] private float boardHeightOffset = 0.08f;
    [Tooltip("루프 파티클도 이 시간이 지나면 방출을 멈추고 잔상을 정리")]
    [SerializeField, Min(0.01f)] private float emissionDuration = 0.35f;
    [Tooltip("Heal의 링 애니메이션이 끝나기 전에 파티클 수명만으로 제거하지 않도록 유지하는 시간")]
    [SerializeField, Min(0f)] private float minimumEffectLifetime = 1.5f;

    public void Play(Vector3 impactPosition, LDY_BoardManager board)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || board == null) return;

        float cellWidth = Vector3.Distance(board.GridToWorld(Vector3Int.zero),
            board.GridToWorld(Vector3Int.right));
        GameObject root = new GameObject("DLJ_OtterShellPlayback");
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        root.transform.position = impactPosition + spawnOffset;
        // 기물이 죽거나 움직여도 타격 지점과 전체 연출을 유지한다.
        root.AddComponent<DLJ_OtterShellPlayback>().Initialize(
            shellPrefab, shellScale, shellRotation, spinDegreesPerSecond,
            riseDuration, riseHeight, burstPrefab, burstScale, sacrificePrefab,
            board.BoardCenter + Vector3.up * boardHeightOffset,
            cellWidth * LDY_BoardManager.Size / Mathf.Max(0.01f, sacrificeReferenceWidth),
            boardBurstDelay, emissionDuration, minimumEffectLifetime,
            cellWidth * boardParticleSizeInCells, boardParticleCount, boardParticleBrightness);
    }

    [ContextMenu("Preview/Shell Burst (Play Mode)")]
    public void Preview()
    {
        LDY_BoardManager board = GameManager.HasInstance ? GameManager.Instance.Board : null;
        if (board == null) board = FindFirstObjectByType<LDY_BoardManager>();
        LDY_Animal animal = GetComponent<LDY_Animal>();
        Play(animal.modelTransform != null ? animal.modelTransform.position : transform.position, board);
    }
}
