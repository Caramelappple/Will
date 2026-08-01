using UnityEngine;

namespace _Scripts.LSO.CoreLib
{
    public class MonoSingleton<T> : MonoBehaviour where T: MonoBehaviour
    {
        private static T _instance;
        private static bool _isQuitting;
        
        public static bool HasInstance => _instance != null;

        public static T Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance != null) return _instance;

                _instance = FindFirstObjectByType<T>();
                if (_instance != null) return _instance;

                GameObject instanceGo = new GameObject(typeof(T).Name);
                _instance = instanceGo.AddComponent<T>();

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }
    }
}
