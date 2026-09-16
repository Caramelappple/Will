using System;
using _Scripts.LDY;
using _Scripts.LSO.CoreLib;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 특성이 발동했다는 것을 알리는 유일한 창구.
    ///
    /// ── 왜 필요했나 ───────────────────────────────────────────
    /// 특성은 MonoBehaviour가 아니라 순수 C# 클래스다. 인스펙터도 없고
    /// 씬에 붙지도 않으니, 발동했을 때 화면에 무언가를 띄울 방법이 없었다.
    /// 지금까지는 LSO_AbilityLog로 콘솔에 찍는 것이 전부였다 —
    /// 개발자는 보지만 플레이어는 체력이 왜 줄었는지 알 수 없다.
    ///
    /// 유언은 DLJ_WillDataSO.effectPrefab 이 그 일을 한다. 특성에는 그게 없었다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// **발행하는 곳은 특성, 듣는 곳은 여럿이다.** 이펙트·소리·화면 흔들림이
    /// 각자 구독하면 되고, 특성 코드는 누가 듣는지 몰라도 된다.
    ///
    /// 콘솔 로그도 여기를 지난다. 특성이 "발동했다"를 두 번 말하지 않게 하려는 것이다.
    /// </summary>
    public static class LSO_AbilitySignal
    {
        /// <summary>
        /// 특성이 발동했다. 구독하는 쪽이 화면에 무언가를 띄운다.
        ///
        /// 정적 이벤트라 구독을 반드시 끊어야 한다. 안 끊으면 씬을 넘긴 뒤에도
        /// 죽은 오브젝트가 불려 나온다.
        /// </summary>
        public static event Action<LSO_AbilityFired> Fired;

        // 정적 이벤트는 Reload Domain을 끄면 플레이 사이에 살아남는다.
        // 지난 판의 구독자가 남아 있으면 파괴된 오브젝트를 건드린다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Fired = null;
        }

        /// <summary>
        /// 발동을 알린다. 특성이 부른다.
        ///
        /// 이펙트는 특성을 가진 기물 자리에 뜬다. 다른 곳에 띄워야 하면 아래 겹쳐 쓴 것을 쓴다.
        /// </summary>
        /// <param name="type">어떤 특성인지. 사전에서 이펙트를 찾는 열쇠다.</param>
        /// <param name="owner">특성을 가진 기물.</param>
        /// <param name="message">콘솔에 남길 말. 비우면 로그를 남기지 않는다.</param>
        public static void Raise(LSO_AbilityType type, LDY_Animal owner, string message = null)
        {
            Raise(type, owner, owner, message);
        }

        /// <summary>
        /// 발동을 알리되, **이펙트가 뜰 자리를 따로 준다.**
        ///
        /// ── 왜 나눠야 했나 ────────────────────────────────────────
        /// 가시는 고슴도치가 가진 특성이지만 실제로 일이 일어나는 곳은 **때린 쪽**이다.
        /// 고슴도치 자리에 가시가 돋으면 "내가 찔렸다"로 보여서, 피해를 받은 것이
        /// 누구인지 화면과 숫자가 어긋난다.
        ///
        /// 그래서 "누구의 특성인가(owner)"와 "어디에 띄우는가(at)"를 나눴다.
        /// 둘이 같은 보통의 경우는 위의 짧은 쪽이 알아서 넘긴다.
        /// ─────────────────────────────────────────────────────────
        ///
        /// 로그는 owner 를 문맥으로 남긴다. 콘솔에서 눌렀을 때 특성을 가진 기물이
        /// 선택돼야 왜 발동했는지 따라갈 수 있다.
        /// </summary>
        /// <param name="type">어떤 특성인지.</param>
        /// <param name="owner">특성을 가진 기물.</param>
        /// <param name="at">이펙트가 뜰 자리가 될 기물. null 이면 아무것도 띄우지 않는다.</param>
        /// <param name="message">콘솔에 남길 말. 비우면 로그를 남기지 않는다.</param>
        public static void Raise(LSO_AbilityType type, LDY_Animal owner, LDY_Animal at, string message = null)
        {
            if (!string.IsNullOrEmpty(message))
                LSO_AbilityLog.Log(message, owner);

            if (type == LSO_AbilityType.None) return;

            Fired?.Invoke(new LSO_AbilityFired(type, owner, at));
        }

        /// <summary>
        /// 체력 쪽에서 부를 때. 회피·면역처럼 피해를 가로채는 특성은
        /// 기물이 아니라 DamageableResources 를 받기 때문이다.
        ///
        /// 기물을 찾는 규칙을 여기 하나에 둔다. 호출부마다 GetComponent 를 적으면
        /// 한 곳이 부모를 안 훑는 식으로 어긋난다.
        /// </summary>
        public static void Raise(LSO_AbilityType type, Component at, string message = null)
        {
            LDY_Animal animal = at != null ? at.GetComponentInParent<LDY_Animal>() : null;

            Raise(type, animal, message);
        }
    }

    /// <summary>
    /// 방금 발동한 특성 하나.
    ///
    /// 기물은 이미 죽어 파괴되는 중일 수 있다. 자리(Position)를 따로 담아두는 것은
    /// 듣는 쪽이 기물을 다시 물어보지 않아도 되게 하려는 것이다 —
    /// 물어보는 시점에는 이미 없을 수 있다.
    /// </summary>
    public readonly struct LSO_AbilityFired
    {
        public LSO_AbilityFired(LSO_AbilityType type, LDY_Animal animal)
            : this(type, animal, animal) { }

        public LSO_AbilityFired(LSO_AbilityType type, LDY_Animal animal, LDY_Animal at)
        {
            Type = type;
            Animal = animal;
            At = at;

            Position = PositionOf(at);
            HasPosition = at != null;
        }

        /// <summary>어떤 특성인지.</summary>
        public LSO_AbilityType Type { get; }

        /// <summary>특성을 가진 기물. 이미 파괴됐으면 null일 수 있다.</summary>
        public LDY_Animal Animal { get; }

        /// <summary>
        /// 이펙트가 뜬 기물. 보통은 Animal 과 같지만, 가시처럼 상대에게 일이 일어나는
        /// 특성에서는 다르다. 마찬가지로 이미 파괴됐을 수 있다.
        /// </summary>
        public LDY_Animal At { get; }

        /// <summary>이펙트가 뜰 자리. 기물이 사라져도 남는다.</summary>
        public Vector3 Position { get; }

        /// <summary>자리를 믿어도 되는지. 기물을 몰랐으면 false다.</summary>
        public bool HasPosition { get; }

        /// <summary>
        /// 기물의 어느 자리에 띄울지.
        ///
        /// modelTransform 을 먼저 보는 것은 기물 루트가 타일 바닥에 있고 모델만
        /// 위로 올라와 있는 구조이기 때문이다. 루트를 쓰면 이펙트가 발밑에 깔린다.
        /// </summary>
        private static Vector3 PositionOf(LDY_Animal animal)
        {
            if (animal == null) return Vector3.zero;

            return animal.modelTransform != null
                ? animal.modelTransform.position
                : animal.transform.position;
        }
    }
}
