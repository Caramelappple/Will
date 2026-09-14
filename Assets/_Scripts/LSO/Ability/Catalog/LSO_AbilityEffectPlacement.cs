using UnityEngine;

namespace _Scripts.LSO.Ability.Catalog
{
    /// <summary>
    /// 특성 이펙트를 어디에 어떤 자세로 놓을지 정하는 **유일한 곳.**
    ///
    /// 실제 재생(LSO_AbilityEffectPlayer)과 에디터 미리보기가 둘 다 여기를 쓴다.
    /// 각자 계산하면 미리보기에서 맞춰놓은 자리가 플레이할 때 달라진다 —
    /// 미리보기가 거짓말을 하면 안 보는 것만 못하다.
    /// </summary>
    public static class LSO_AbilityEffectPlacement
    {
        /// <summary>
        /// 사전에 적힌 대로 놓는다.
        ///
        /// 회전과 크기는 프리팹 값에 더하거나 곱하지 않고 **그대로 덮어쓴다.**
        /// 더하는 방식을 쓰던 때에는 프리팹이 -90 으로 눕혀져 있으면 사전에 90 을
        /// 적었을 때 상쇄돼 0 이 됐다. 적은 값이 그대로 들어가야 숫자를 보며 맞출 수 있다.
        /// </summary>
        /// <param name="instance">놓을 대상.</param>
        /// <param name="info">사전에 적힌 값.</param>
        /// <param name="at">기준이 되는 자리. 보통 기물이 선 곳이다.</param>
        public static void Apply(Transform instance, LSO_AbilityInfo info, Vector3 at)
        {
            if (instance == null) return;

            instance.SetPositionAndRotation(
                at + info.effectOffset,
                Quaternion.Euler(info.effectRotation));

            instance.localScale = info.effectScale;

            ScaleWithHierarchy(instance);
        }

        /// <summary>
        /// 자식 파티클이 부모 크기를 따라오게 한다.
        ///
        /// ── 왜 필요한가 ───────────────────────────────────────────
        /// ParticleSystem 의 Scaling Mode 는 기본이 **Local** 이다. 자기 트랜스폼의
        /// 크기만 보고 부모는 무시한다. 그래서 뿌리를 줄여도 자식에서 나오는 입자는
        /// 그대로 커다랗게 남는다.
        ///
        /// Hierarchy 로 바꾸면 위에서부터 곱해진 크기를 쓴다.
        /// ─────────────────────────────────────────────────────────
        ///
        /// 띄운 사본에만 손대므로 프리팹 원본은 그대로다.
        /// 프리팹을 다른 곳에서 쓰고 있어도 영향이 없다.
        /// </summary>
        private static void ScaleWithHierarchy(Transform instance)
        {
            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem.MainModule main = systems[i].main;

                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
        }
    }
}
