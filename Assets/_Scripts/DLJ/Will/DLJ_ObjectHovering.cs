using System.Collections;
using System.Collections.Generic;
using _Scripts.LDY;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(LDY_SelectionController))]
public class DLJ_ObjectHovering : MonoBehaviour
{
    [Header("Hover Settings")]
    [SerializeField, Min(0f)] private float hoverHeight = 0.2f;
    [SerializeField, Min(0f)] private float riseDuration = 0.15f;
    [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] private float fallDuration = 0.2f;
    [SerializeField] private AnimationCurve fallCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private LDY_BoardManager _board;
    private Camera _targetCamera;
    private LDY_Animal _hoveredAnimal;
    private readonly Dictionary<Transform, Vector3> _originalPositions = new();
    private readonly Dictionary<Transform, Coroutine> _activeAnimations = new();

    private void Awake()
    {
        _board = GetComponent<LDY_BoardManager>();
        if (_board == null)
            _board = FindFirstObjectByType<LDY_BoardManager>();

        _targetCamera = Camera.main;
    }

    private void Update()
    {
        LDY_Animal nextHovered = FindAnimalUnderMouse();
        if (_hoveredAnimal == nextHovered)
            return;

        LDY_Animal previousHovered = _hoveredAnimal;
        _hoveredAnimal = nextHovered;

        LowerAnimal(previousHovered);
        RaiseAnimal(nextHovered);
    }

    private void OnDisable()
    {
        _hoveredAnimal = null;
        RestoreImmediately();
    }

    private LDY_Animal FindAnimalUnderMouse()
    {
        if (Mouse.current == null || _board == null)
            return null;

        if (_targetCamera == null)
            _targetCamera = Camera.main;
        if (_targetCamera == null)
            return null;

        Ray ray = _targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
            return null;

        // 기물 자체에 콜라이더가 있으면 바로 찾고, 없으면 맞은 보드 칸의 점유 기물을 찾는다.
        LDY_Animal hitAnimal = hit.collider.GetComponentInParent<LDY_Animal>();
        if (hitAnimal != null)
            return hitAnimal.team == LDY_Team.Player ? hitAnimal : null;

        Vector3Int gridPos = _board.WorldToGrid(hit.point);
        if (!_board.IsInside(gridPos))
            return null;

        LDY_Animal boardAnimal = _board.Get(gridPos);
        return boardAnimal != null && boardAnimal.team == LDY_Team.Player ? boardAnimal : null;
    }

    private void RaiseAnimal(LDY_Animal animal)
    {
        if (animal == null || animal.modelTransform == null)
            return;

        Transform target = animal.modelTransform;
        if (!_originalPositions.TryGetValue(target, out Vector3 originalPosition))
        {
            originalPosition = target.position;
            _originalPositions.Add(target, originalPosition);
        }

        StartPositionAnimation(
            target,
            originalPosition + Vector3.up * hoverHeight,
            riseDuration,
            riseCurve,
            false);
    }

    private void LowerAnimal(LDY_Animal animal)
    {
        if (animal == null || animal.modelTransform == null)
            return;

        Transform target = animal.modelTransform;
        if (!_originalPositions.TryGetValue(target, out Vector3 originalPosition))
            return;

        StartPositionAnimation(target, originalPosition, fallDuration, fallCurve, true);
    }

    private void StartPositionAnimation(
        Transform target,
        Vector3 targetPosition,
        float duration,
        AnimationCurve curve,
        bool forgetOriginalPosition)
    {
        if (_activeAnimations.TryGetValue(target, out Coroutine activeAnimation))
            StopCoroutine(activeAnimation);

        if (duration <= 0f)
        {
            target.position = targetPosition;
            _activeAnimations.Remove(target);
            if (forgetOriginalPosition)
                _originalPositions.Remove(target);
            return;
        }

        Coroutine animation = StartCoroutine(AnimatePosition(
            target,
            target.position,
            targetPosition,
            duration,
            curve,
            forgetOriginalPosition));
        _activeAnimations[target] = animation;
    }

    private IEnumerator AnimatePosition(
        Transform target,
        Vector3 startPosition,
        Vector3 targetPosition,
        float duration,
        AnimationCurve curve,
        bool forgetOriginalPosition)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null)
                yield break;

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = curve != null ? curve.Evaluate(progress) : progress;
            target.position = Vector3.LerpUnclamped(startPosition, targetPosition, eased);
            yield return null;
        }

        if (target != null)
            target.position = targetPosition;

        _activeAnimations.Remove(target);
        if (forgetOriginalPosition)
            _originalPositions.Remove(target);
    }

    private void RestoreImmediately()
    {
        StopAllCoroutines();

        foreach (KeyValuePair<Transform, Vector3> entry in _originalPositions)
        {
            if (entry.Key != null)
                entry.Key.position = entry.Value;
        }

        _activeAnimations.Clear();
        _originalPositions.Clear();
    }
}
