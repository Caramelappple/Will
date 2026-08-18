using System.Collections;
using UnityEngine;

namespace _Scripts.LDY
{
    /// <summary>
    /// 화면을 잠깐 흔든다. 충돌처럼 "묵직하게 부딪혔다"를 알릴 때 쓴다.
    ///
    /// 씬에 미리 붙여둘 필요가 없다. 처음 흔들 때 메인 카메라를 찾아 스스로 붙는다.
    /// 연출 컴포넌트를 씬마다 손으로 달아두게 하면, 안 달린 씬에서 조용히 아무 일도 일어나지 않고
    /// 원인이 배선이라는 걸 알아채기까지 한참 걸린다.
    ///
    /// 카메라가 없으면 그냥 넘어간다. 연출이 없다고 게임이 멈추면 안 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LDY_CameraShake : MonoBehaviour
    {
        private static LDY_CameraShake _instance;

        // 흔들기 전의 자리. 부모가 카메라를 따로 움직이는 경우까지 감안해 로컬 좌표로 기억한다.
        private Vector3 _baseLocalPos;
        private Coroutine _running;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // 에디터에서는 재생을 멈춰도 static이 남는다. 다음 재생에서 파괴된 컴포넌트를 붙들지 않도록 비운다.
            _instance = null;
        }

        /// <param name="duration">흔드는 시간(초).</param>
        /// <param name="strength">최대 흔들림 폭(월드 단위). 시간이 지날수록 잦아든다.</param>
        public static void Shake(float duration, float strength)
        {
            if (duration <= 0f || strength <= 0f) return;

            LDY_CameraShake shaker = Resolve();
            if (shaker == null) return;

            shaker.Play(duration, strength);
        }

        private static LDY_CameraShake Resolve()
        {
            if (_instance != null) return _instance;

            Camera camera = Camera.main;
            if (camera == null) return null;

            _instance = camera.GetComponent<LDY_CameraShake>();
            if (_instance == null)
                _instance = camera.gameObject.AddComponent<LDY_CameraShake>();

            return _instance;
        }

        private void Play(float duration, float strength)
        {
            // 흔드는 도중에 또 들어오면(연쇄 충돌) 먼저 제자리로 돌려놓는다.
            // 그러지 않으면 흔들린 위치를 새 기준점으로 잡아 카메라가 조금씩 밀려난다.
            if (_running != null)
            {
                StopCoroutine(_running);
                transform.localPosition = _baseLocalPos;
            }

            _baseLocalPos = transform.localPosition;
            _running = StartCoroutine(ShakeRoutine(duration, strength));
        }

        private IEnumerator ShakeRoutine(float duration, float strength)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // 끝으로 갈수록 잦아들게 한다. 끝까지 같은 폭으로 흔들면 뚝 끊기는 느낌이 난다.
                float falloff = 1f - Mathf.Clamp01(elapsed / duration);
                Vector2 offset = Random.insideUnitCircle * (strength * falloff);

                transform.localPosition = _baseLocalPos + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }

            transform.localPosition = _baseLocalPos;
            _running = null;
        }
    }
}
