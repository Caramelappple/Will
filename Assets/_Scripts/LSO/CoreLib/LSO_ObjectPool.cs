using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LSO.CoreLib
{
    /// <summary>
    /// 프리팹 하나를 재사용하는 풀. 만들고 빌려주고 돌려받는 것 외의 책임은 갖지 않는다.
    ///
    /// MonoBehaviour가 아니라 평범한 클래스다. 쓰는 쪽이 필드로 하나 들고 있으면 된다.
    /// 제네릭 MonoBehaviour로 만들면 인스펙터에 뜨지 않고, 제네릭 타입 안에서는
    /// RuntimeInitializeOnLoadMethod가 돌지 않으며, static 필드가 닫힌 타입마다 따로 생겨
    /// "왜 이 풀만 안 비워지지"를 뒤늦게 찾게 된다.
    ///
    /// 쓰는 법:
    /// <code>
    /// private LSO_ObjectPool&lt;LSO_DamagePopup&gt; _pool;
    ///
    /// private void Awake()
    /// {
    ///     _pool = new LSO_ObjectPool&lt;LSO_DamagePopup&gt;(prefab, transform, prewarm: 8);
    /// }
    ///
    /// var item = _pool.Get();
    /// _pool.Release(item);
    /// </code>
    /// </summary>
    public class LSO_ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly int _maxRetained;

        private readonly Stack<T> _idle = new Stack<T>();

        // 이미 반납된 것을 또 반납했는지 보려고 따로 둔다.
        // Stack만으로는 들어 있는지 확인하는 데 전체를 훑어야 하는데,
        // 중복 반납은 같은 오브젝트가 두 곳에서 동시에 쓰이는 형태로 드러나서
        // 원인을 찾기가 대단히 어렵다. 그 정도면 세트 하나를 더 들 값어치가 있다.
        private readonly HashSet<T> _idleSet = new HashSet<T>();

        /// <summary>지금 빌려줄 수 있는 개수.</summary>
        public int IdleCount => _idle.Count;

        /// <summary>이 풀이 지금까지 실제로 Instantiate한 횟수. 풀 크기를 정할 때 본다.</summary>
        public int CreatedCount { get; private set; }

        /// <param name="prefab">복제할 원본.</param>
        /// <param name="parent">쉬는 동안 매달아 둘 곳. 없으면 씬 루트에 흩어진다.</param>
        /// <param name="prewarm">미리 만들어 둘 개수. 첫 사용 때 끊기는 것을 막는다.</param>
        /// <param name="maxRetained">보관할 최대 개수. -1이면 무제한.</param>
        public LSO_ObjectPool(T prefab, Transform parent = null, int prewarm = 0, int maxRetained = -1)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab), "풀에 넣을 프리팹이 없습니다.");

            _prefab = prefab;
            _parent = parent;
            _maxRetained = maxRetained;

            for (int i = 0; i < prewarm; i++)
            {
                T item = Create();

                item.gameObject.SetActive(false);

                _idle.Push(item);
                _idleSet.Add(item);
            }
        }

        public T Get()
        {
            T item = TakeIdle() ?? Create();

            item.gameObject.SetActive(true);

            if (item is LSO_IPoolable poolable)
                poolable.OnSpawned();

            return item;
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            T item = TakeIdle() ?? Create();

            // 켜기 전에 자리를 잡는다. 켜고 나서 옮기면 한 프레임 동안
            // 지난번 자리에 그려진다.
            item.transform.SetPositionAndRotation(position, rotation);

            item.gameObject.SetActive(true);

            if (item is LSO_IPoolable poolable)
                poolable.OnSpawned();

            return item;
        }

        /// <summary>다 쓴 것을 돌려준다. 이미 돌려준 것을 또 넣으면 무시하고 경고한다.</summary>
        public void Release(T item)
        {
            if (!IsAlive(item)) return;

            // 같은 것을 두 번 반납하면 다음 Get 두 번이 같은 오브젝트를 내준다.
            // 두 곳이 하나를 동시에 쓰게 되므로 여기서 끊는다.
            if (!_idleSet.Add(item))
            {
                Debug.LogWarning($"{item.name}: 이미 풀에 돌아가 있는 것을 또 반납했습니다.", item);
                return;
            }

            if (item is LSO_IPoolable poolable)
                poolable.OnDespawned();

            item.gameObject.SetActive(false);

            if (_parent != null && item.transform.parent != _parent)
                item.transform.SetParent(_parent, false);

            // 한때 몰려 썼다가 안 쓰게 된 만큼을 계속 들고 있을 이유는 없다.
            if (_maxRetained >= 0 && _idle.Count >= _maxRetained)
            {
                _idleSet.Remove(item);
                UnityEngine.Object.Destroy(item.gameObject);
                return;
            }

            _idle.Push(item);
        }

        /// <summary>보관 중인 것을 모두 없앤다. 빌려간 것은 건드리지 않는다.</summary>
        public void Clear()
        {
            while (_idle.Count > 0)
            {
                T item = _idle.Pop();

                if (IsAlive(item))
                    UnityEngine.Object.Destroy(item.gameObject);
            }

            _idleSet.Clear();
        }

        private T TakeIdle()
        {
            // 씬이 바뀌거나 누군가 직접 Destroy하면 보관 중이던 것이 죽은 채로 남는다.
            // 그것을 그대로 내주면 쓰는 쪽에서 NullReference가 나므로 여기서 걸러 버린다.
            while (_idle.Count > 0)
            {
                T item = _idle.Pop();
                _idleSet.Remove(item);

                if (IsAlive(item)) return item;
            }

            return null;
        }

        private T Create()
        {
            T item = UnityEngine.Object.Instantiate(_prefab, _parent);

            CreatedCount++;

            return item;
        }

        /// <summary>
        /// 살아 있는지. 제네릭 안에서 <c>item == null</c>을 쓰면 안 되기 때문에 따로 둔다.
        ///
        /// T가 타입 매개변수이면 C#은 UnityEngine.Object가 정의한 == 를 쓰지 않고
        /// 그냥 참조 비교를 한다. 그래서 Destroy된 오브젝트가 "null이 아니다"로 통과해버린다.
        /// Component로 한 번 받아내면 그때부터 Unity의 == 가 걸려 제대로 걸러진다.
        /// </summary>
        private static bool IsAlive(T item)
        {
            return item is Component component && component != null;
        }
    }
}
