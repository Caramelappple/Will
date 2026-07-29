using UnityEngine;

namespace _Scripts.LSO.CoreLib
{
    public class MonoSingleton<T> : MonoBehaviour where T: MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<T>();

                if (_instance is null)
                {
                    string objectName = typeof(T).ToString();
                    GameObject instanceGo = new GameObject(objectName);
                    _instance = instanceGo.AddComponent<T>();
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            T[] managers = FindObjectsByType<T>(FindObjectsSortMode.None);
            if(managers.Length >1)
                Destroy(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}