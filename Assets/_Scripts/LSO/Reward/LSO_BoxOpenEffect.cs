using System;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
   public class LSO_BoxOpenEffect : MonoBehaviour
   {
      [Header("돌릴 상자 뚜껑")]
      [SerializeField] private GameObject boxTop;

      [Header("회전 방향")]
      [Tooltip("뚜껑이 돌아갈 축. 뚜껑 자신의 로컬 축이다.\n" +
               "경첩이 놓인 방향을 넣는다. 씬 뷰를 Local 모드로 두고 확인할 것.")]
      [SerializeField] private Vector3 rotationVector = Vector3.forward;

      [Header("열기")]
      [Tooltip("닫힌 자세에서 몇 도나 더 돌릴지. 절대 각도가 아니라 열리는 양이다.")]
      [SerializeField] private float openAngle;
      [SerializeField] private float openDuration;
      [SerializeField] private Ease openEase;

      [Header("닫기")]
      [SerializeField] private float closeDuration;
      [SerializeField] private Ease closeEase;

      [Header("토글 사용")]
      [SerializeField] private bool isTest;

      //여기에 보상 시스템 연결
      public event Action OnOpened;
      public event Action OnClosed;

      // 트윈이 끝나기를 기다렸다 바꾸지 않는다.
      // Kill된 트윈은 OnComplete를 부르지 않아서, 그때 바꾸면 값이 실제와 어긋난 채 굳는다.
      private bool _isOpened;

      private Quaternion _originalRotation;

      private Tween _tween;

      public bool IsOpened => _isOpened;
      public bool IsMoving => _tween != null;

      private void Awake()
      {
         if (boxTop == null)
         {
            Debug.LogError($"{name}: Box Top이 비어 있어 열고 닫을 것이 없습니다.", this);
            return;
         }

         _originalRotation = boxTop.transform.localRotation;
      }

      #region ForTest

      //테스트 용
      public void Toggle()
      {
         if (!isTest)
         {
            Open();
            Debug.LogWarning("테스트 모드를 사용중이지 않습니다, 주석으로 지워주세요");
            return;
         }

         if (!_isOpened)
            Open();
         else
            Close();
      }

      #endregion


      [ContextMenu("Open")]
      public void Open()
      {
         if (IsMoving || _isOpened || boxTop == null) return;

         _isOpened = true;

         Rotate(OpenedRotation, openDuration, openEase, () => OnOpened?.Invoke());
      }

      [ContextMenu("Close")]
      public void Close()
      {
         if (IsMoving || !_isOpened || boxTop == null) return;

         _isOpened = false;

         Rotate(_originalRotation, closeDuration, closeEase, () => OnClosed?.Invoke());
      }

      /// <summary>
      /// 남은 거리에 비례해 시간을 줄인다.
      /// 조금 열린 것을 닫는데 활짝 열린 것과 같은 시간을 쓰면 기어가는 것처럼 보인다.
      /// </summary>
      private void Rotate(Quaternion target, float duration, Ease ease, TweenCallback onComplete)
      {
         KillTween();

         float remaining = Quaternion.Angle(boxTop.transform.localRotation, target);
         float full = Quaternion.Angle(_originalRotation, OpenedRotation);
         float scaled = full > 0f ? duration * (remaining / full) : duration;

         if (scaled <= 0f)
         {
            boxTop.transform.localRotation = target;
            onComplete?.Invoke();
            return;
         }

         _tween = boxTop.transform
            .DOLocalRotateQuaternion(target, scaled)
            .SetEase(ease)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
               _tween = null;
               onComplete?.Invoke();
            });
      }

      private Quaternion OpenedRotation => _originalRotation * Quaternion.AngleAxis(openAngle, Axis);

      private Vector3 Axis => rotationVector.sqrMagnitude > 0f ? rotationVector.normalized : Vector3.right;

      private void KillTween()
      {
         if (_tween == null) return;

         _tween.Kill();
         _tween = null;
      }

      private void OnDisable()
      {
         KillTween();
      }
   }
}
