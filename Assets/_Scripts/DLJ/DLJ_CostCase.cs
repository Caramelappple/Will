using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 코스트 코인 5개를 관리하는 DLJ 전용 케이스.
/// LSO 코스트 코드에 의존하지 않는다.
/// </summary>
public class DLJ_CostCase : MonoBehaviour
{
    [Header("Coins")]
    [Tooltip("코인을 찾기 시작할 부모. 비워두면 자신의 모든 자식을 검색한다.")]
    [SerializeField] private Transform coinRoot;

    [Tooltip("자동 수집할 코인 오브젝트 이름에 포함된 문자열.")]
    [SerializeField] private string coinNameFilter = "CostCoin";

    [Tooltip("필요하면 자동 수집 대신 코인을 순서대로 직접 넣어도 된다.")]
    [SerializeField] private List<Transform> coins = new List<Transform>();

    [Header("Coin Entrance Path")]
    [Tooltip("화면 좌표를 계산할 카메라. 비워두면 Main Camera를 사용한다.")]
    [SerializeField] private Camera screenCamera;

    [Tooltip("코인이 출발할 화면 X 위치. 0이 화면 왼쪽 끝이며 음수면 화면 밖이다.")]
    [SerializeField] private float offscreenViewportX = -0.1f;

    [Tooltip("코인이 케이스 위에서 대기할 화면 높이. 화면 높이의 비율로 계산한다.")]
    [SerializeField, Min(0f)] private float aboveViewportOffset = 0.08f;

    [Header("Coin Entrance Timing")]
    [Tooltip("코인 하나가 케이스 위까지 수평 이동하는 기본 시간.")]
    [SerializeField, Min(0.01f)] private float horizontalDuration = 0.3f;

    [Tooltip("케이스 위에서 아래로 들어가는 기본 시간.")]
    [SerializeField, Min(0.01f)] private float dropDuration = 0.18f;

    [Tooltip("다음 코인이 출발하기까지의 기본 간격.")]
    [SerializeField, Min(0f)] private float coinInterval = 0.1f;

    [Tooltip("첫 코인 출발부터 마지막 코인 도착까지 허용할 최대 시간. 넘으면 이동 시간과 간격을 비례 축소한다.")]
    [SerializeField, Min(0.01f)] private float maxEntranceDuration = 1.2f;

    [SerializeField] private AnimationCurve horizontalEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve dropEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Coin Entrance Playback")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("케이스 진입 연출이 끝난 다음 코인을 넣는다.")]
    [SerializeField] private bool waitForCaseEntrance = true;

    [Tooltip("케이스 진입 연출이 끝난 뒤 첫 코인이 출발하기 전까지 추가로 기다릴 시간.")]
    [SerializeField, Min(0f)] private float afterCaseEntranceDelay;

    [SerializeField] private bool ignoreTimeScale;

    private readonly List<Transform> _found = new List<Transform>();
    private readonly List<Transform> _coinHomeParents = new List<Transform>();
    private readonly List<Vector3> _restLocalPositions = new List<Vector3>();
    private readonly List<Quaternion> _restLocalRotations = new List<Quaternion>();
    private readonly List<Vector3> _restLocalScales = new List<Vector3>();
    private readonly Dictionary<Transform, Sequence> _coinSequences = new Dictionary<Transform, Sequence>();
    private Transform _coinAnimationRoot;
    private bool _initialized;
    private bool _started;

    private Camera EntranceCamera => screenCamera != null ? screenCamera : Camera.main;

    public int Capacity => coins.Count;
    public int FilledCount { get; private set; }

    private Transform SearchRoot => coinRoot != null ? coinRoot : transform;

    private void Awake()
    {
        Initialize();

        // 첫 화면이 그려지기 전부터 숨긴다.
        // Start에서 숨기면 케이스와 함께 한 프레임 보일 수 있다.
        if (playOnStart)
            HideAllCoinsForEntrance();
    }

    private void Start()
    {
        _started = true;

        if (playOnStart)
            PlayFilledCoinsEntrance();
    }

    /// <summary>직접 연결된 코인이 없으면 이름 필터로 자식 코인을 수집한다.</summary>
    public void Initialize()
    {
        if (_initialized) return;

        RemoveMissingCoins();
        if (coins.Count == 0)
            CollectCoins();

        _coinHomeParents.Clear();
        _restLocalPositions.Clear();
        _restLocalRotations.Clear();
        _restLocalScales.Clear();
        foreach (Transform coin in coins)
        {
            _coinHomeParents.Add(coin != null ? coin.parent : null);
            _restLocalPositions.Add(coin != null ? coin.localPosition : Vector3.zero);
            _restLocalRotations.Add(coin != null ? coin.localRotation : Quaternion.identity);
            _restLocalScales.Add(coin != null ? coin.localScale : Vector3.one);
        }

        FilledCount = coins.Count;
        _initialized = true;

        if (coins.Count == 0)
            Debug.LogError($"{name}: 이름에 '{coinNameFilter}'이 포함된 코인을 찾지 못했습니다.", this);
    }

    [ContextMenu("Collect Cost Coins")]
    public void CollectCoins()
    {
        coins.Clear();
        _found.Clear();
        SearchRoot.GetComponentsInChildren(true, _found);

        foreach (Transform candidate in _found)
        {
            if (candidate == null || candidate == SearchRoot) continue;
            if (string.IsNullOrEmpty(coinNameFilter)) continue;
            if (candidate.name.IndexOf(coinNameFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;

            coins.Add(candidate);
        }
    }

    /// <summary>앞에서부터 count개의 코인을 채우고 나머지를 비운다.</summary>
    public void SetFilled(int count)
    {
        Initialize();
        count = Mathf.Clamp(count, 0, coins.Count);
        int previousCount = FilledCount;

        for (int i = 0; i < coins.Count; i++)
        {
            Transform coin = coins[i];
            if (coin == null) continue;

            bool filled = i < count;

            // 사용 연출은 없다. 줄어든 코인은 즉시 숨긴다.
            if (!filled)
            {
                KillCoinSequence(coin);
                RestoreCoinToSlot(i, false);
            }
            else if (!_started && playOnStart)
            {
                // 케이스 진입 전에 부모를 분리해야 비활성 코인도 케이스 이동을 따라가지 않는다.
                DetachCoinForEntrance(i);
                coin.gameObject.SetActive(false);
            }
            else if (i < previousCount || !_started)
            {
                // 이미 등장 중인 코인은 같은 값이 다시 들어와도 슬롯으로 순간 이동시키지 않는다.
                if (!_coinSequences.ContainsKey(coin))
                    RestoreCoinToSlot(i, true);
            }
        }

        FilledCount = count;

        // 새로 충전된 코인만 왼쪽 → 케이스 위 → 아래 경로로 들어온다.
        if (_started && count > previousCount)
            PlayCoinRange(previousCount, count, 0f);
    }

    private void HideAllCoinsForEntrance()
    {
        for (int i = 0; i < coins.Count; i++)
        {
            Transform coin = coins[i];
            if (coin == null) continue;

            DetachCoinForEntrance(i);
            coin.gameObject.SetActive(false);
        }
    }

    [ContextMenu("Play Filled Coin Entrance")]
    public void PlayFilledCoinsEntrance()
    {
        Initialize();

        float delay = afterCaseEntranceDelay;
        if (waitForCaseEntrance)
        {
            DLJ_CostAnimation caseAnimation = GetComponentInParent<DLJ_CostAnimation>();
            if (caseAnimation != null)
                delay += caseAnimation.TotalDuration;
        }

        PlayCoinRange(0, FilledCount, delay);
    }

    private void PlayCoinRange(int startIndex, int endIndex, float initialDelay)
    {
        startIndex = Mathf.Clamp(startIndex, 0, coins.Count);
        endIndex = Mathf.Clamp(endIndex, startIndex, coins.Count);
        int animatedCount = endIndex - startIndex;
        if (animatedCount <= 0) return;

        float rawDuration = horizontalDuration + dropDuration + coinInterval * (animatedCount - 1);
        float timingScale = rawDuration > maxEntranceDuration
            ? maxEntranceDuration / rawDuration
            : 1f;

        float actualHorizontalDuration = horizontalDuration * timingScale;
        float actualDropDuration = dropDuration * timingScale;
        float actualInterval = coinInterval * timingScale;

        for (int i = startIndex; i < endIndex; i++)
        {
            Transform coin = coins[i];
            if (coin == null) continue;

            int order = i - startIndex;

            KillCoinSequence(coin);
            DetachCoinForEntrance(i);
            coin.gameObject.SetActive(false);

            int coinIndex = i;
            Sequence sequence = DOTween.Sequence()
                .SetDelay(initialDelay + actualInterval * order)
                .AppendCallback(() => BeginCoinEntrance(
                    coinIndex,
                    coin,
                    actualHorizontalDuration,
                    actualDropDuration))
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject);

            _coinSequences[coin] = sequence;
            sequence.OnKill(() =>
            {
                if (_coinSequences.TryGetValue(coin, out Sequence tracked) && tracked == sequence)
                    _coinSequences.Remove(coin);
            });
        }
    }

    /// <summary>
    /// 케이스 이동이 끝난 시점의 좌표로 경로를 계산한다.
    /// 미리 계산하면 움직이기 전 케이스 위치가 코인의 도착점으로 굳는다.
    /// </summary>
    private void BeginCoinEntrance(
        int coinIndex,
        Transform coin,
        float actualHorizontalDuration,
        float actualDropDuration)
    {
        if (coin == null || coinIndex < 0 || coinIndex >= _restLocalPositions.Count) return;

        Vector3 restLocalPosition = _restLocalPositions[coinIndex];
        Transform homeParent = _coinHomeParents[coinIndex];
        Vector3 restWorldPosition = homeParent != null
            ? homeParent.TransformPoint(restLocalPosition)
            : restLocalPosition;

        Camera cameraForEntrance = EntranceCamera;
        if (cameraForEntrance == null)
        {
            Debug.LogError($"{name}: 코인 등장 경로를 계산할 카메라가 없습니다.", this);
            coin.localPosition = restLocalPosition;
            coin.gameObject.SetActive(true);
            return;
        }

        Vector3 restViewportPosition = cameraForEntrance.WorldToViewportPoint(restWorldPosition);
        Vector3 aboveViewportPosition = new Vector3(
            restViewportPosition.x,
            restViewportPosition.y + aboveViewportOffset,
            restViewportPosition.z);
        Vector3 startViewportPosition = new Vector3(
            offscreenViewportX,
            aboveViewportPosition.y,
            restViewportPosition.z);

        Vector3 aboveWorldPosition = cameraForEntrance.ViewportToWorldPoint(aboveViewportPosition);
        Vector3 startWorldPosition = cameraForEntrance.ViewportToWorldPoint(startViewportPosition);

        coin.position = startWorldPosition;
        coin.gameObject.SetActive(true);

        Sequence movement = DOTween.Sequence()
            .Append(coin.DOMove(aboveWorldPosition, actualHorizontalDuration).SetEase(horizontalEase))
            .Append(coin.DOMove(restWorldPosition, actualDropDuration).SetEase(dropEase))
            .SetUpdate(ignoreTimeScale)
            .SetLink(gameObject)
            .OnComplete(() => RestoreCoinToSlot(coinIndex, true));

        _coinSequences[coin] = movement;
        movement.OnKill(() =>
        {
            if (_coinSequences.TryGetValue(coin, out Sequence tracked) && tracked == movement)
                _coinSequences.Remove(coin);
        });
    }

    private void RemoveMissingCoins()
    {
        for (int i = coins.Count - 1; i >= 0; i--)
        {
            if (coins[i] == null)
                coins.RemoveAt(i);
        }
    }

    /// <summary>
    /// 코인을 케이스 계층 밖으로 옮긴다.
    /// 케이스가 움직여도 코인의 월드 위치가 따라가지 않게 만드는 핵심 단계다.
    /// </summary>
    private void DetachCoinForEntrance(int coinIndex)
    {
        if (coinIndex < 0 || coinIndex >= coins.Count) return;

        Transform coin = coins[coinIndex];
        if (coin == null) return;

        EnsureCoinAnimationRoot();
        if (_coinAnimationRoot != null && coin.parent != _coinAnimationRoot)
            coin.SetParent(_coinAnimationRoot, true);
    }

    /// <summary>코인을 원래 케이스 슬롯의 부모와 로컬 Transform으로 되돌린다.</summary>
    private void RestoreCoinToSlot(int coinIndex, bool active)
    {
        if (coinIndex < 0 || coinIndex >= coins.Count) return;
        if (coinIndex >= _coinHomeParents.Count ||
            coinIndex >= _restLocalPositions.Count ||
            coinIndex >= _restLocalRotations.Count ||
            coinIndex >= _restLocalScales.Count)
            return;

        Transform coin = coins[coinIndex];
        if (coin == null) return;

        Transform homeParent = _coinHomeParents[coinIndex];
        coin.SetParent(homeParent, false);
        coin.localPosition = _restLocalPositions[coinIndex];
        coin.localRotation = _restLocalRotations[coinIndex];
        coin.localScale = _restLocalScales[coinIndex];
        coin.gameObject.SetActive(active);
    }

    private void EnsureCoinAnimationRoot()
    {
        if (_coinAnimationRoot != null) return;

        GameObject rootObject = new GameObject($"{name}_CoinAnimationRoot");
        _coinAnimationRoot = rootObject.transform;

        // 런타임 생성된 추가 케이스도 자신이 속한 씬 안에서 정리되게 한다.
        if (gameObject.scene.IsValid() && rootObject.scene != gameObject.scene)
            SceneManager.MoveGameObjectToScene(rootObject, gameObject.scene);
    }

    private void KillCoinSequence(Transform coin)
    {
        if (coin == null || !_coinSequences.TryGetValue(coin, out Sequence sequence)) return;

        _coinSequences.Remove(coin);
        sequence.Kill();
    }

    private void OnDisable()
    {
        Sequence[] sequences = new Sequence[_coinSequences.Count];
        _coinSequences.Values.CopyTo(sequences, 0);
        _coinSequences.Clear();

        foreach (Sequence sequence in sequences)
            sequence?.Kill();

        for (int i = 0; i < coins.Count && i < _restLocalPositions.Count; i++)
        {
            if (coins[i] == null) continue;

            bool wasActive = coins[i].gameObject.activeSelf;
            RestoreCoinToSlot(i, wasActive);
        }
    }

    private void OnDestroy()
    {
        if (_coinAnimationRoot != null)
            Destroy(_coinAnimationRoot.gameObject);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        horizontalDuration = Mathf.Max(0.01f, horizontalDuration);
        dropDuration = Mathf.Max(0.01f, dropDuration);
        coinInterval = Mathf.Max(0f, coinInterval);
        maxEntranceDuration = Mathf.Max(0.01f, maxEntranceDuration);
        aboveViewportOffset = Mathf.Max(0f, aboveViewportOffset);
        afterCaseEntranceDelay = Mathf.Max(0f, afterCaseEntranceDelay);

        if (horizontalEase == null || horizontalEase.length == 0)
            horizontalEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        if (dropEase == null || dropEase.length == 0)
            dropEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
#endif

}
