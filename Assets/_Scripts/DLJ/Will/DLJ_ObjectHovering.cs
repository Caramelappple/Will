using System.Collections;
using _Scripts.LDY;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LDY_SelectionController))]
public class DLJ_ObjectHovering : MonoBehaviour
{
    [Header("Hover Settings")]
    [SerializeField, Min(0f)] private float hoverHeight = 0.2f;
    [SerializeField, Min(0f)] private float riseDuration = 0.15f;
    [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private LDY_SelectionController _selectionController;
    private Transform _hoveredTransform;
    private Vector3 _originalPosition;
    private Coroutine _hoverRoutine;

    private void Awake()
    {
        _selectionController = GetComponent<LDY_SelectionController>();
    }

    private void OnEnable()
    {
        _selectionController.OnSelectionChanged += HandleSelectionChanged;

        // 이 컴포넌트보다 먼저 선택이 이루어진 상태에서 다시 활성화될 수도 있다.
        if (_selectionController.Selected != null)
            HandleSelectionChanged(_selectionController.Selected);
    }

    private void OnDisable()
    {
        _selectionController.OnSelectionChanged -= HandleSelectionChanged;
        RestoreImmediately();
    }

    private void HandleSelectionChanged(LDY_Animal selectedAnimal)
    {
        RestoreImmediately();

        if (selectedAnimal == null || selectedAnimal.modelTransform == null)
            return;

        _hoveredTransform = selectedAnimal.modelTransform;
        _originalPosition = _hoveredTransform.position;
        _hoverRoutine = StartCoroutine(RaiseSelectedObject());
    }

    private IEnumerator RaiseSelectedObject()
    {
        if (riseDuration <= 0f)
        {
            if (_hoveredTransform != null)
                _hoveredTransform.position = _originalPosition + Vector3.up * hoverHeight;
            _hoverRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            if (_hoveredTransform == null)
            {
                _hoverRoutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / riseDuration);
            float eased = riseCurve != null ? riseCurve.Evaluate(progress) : progress;
            _hoveredTransform.position = _originalPosition + Vector3.up * (hoverHeight * eased);
            yield return null;
        }

        if (_hoveredTransform != null)
            _hoveredTransform.position = _originalPosition + Vector3.up * hoverHeight;

        _hoverRoutine = null;
    }

    private void RestoreImmediately()
    {
        if (_hoverRoutine != null)
        {
            StopCoroutine(_hoverRoutine);
            _hoverRoutine = null;
        }

        if (_hoveredTransform != null)
            _hoveredTransform.position = _originalPosition;

        _hoveredTransform = null;
    }
}
