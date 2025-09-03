using UnityEngine;

namespace XGame.Core
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
#if UNITY_6000_0_OR_NEWER
                    _instance = FindFirstObjectByType<T>();
#else
                    _instance = FindObjectOfType<T>();
#endif
                    if (_instance == null)
                    {
                        var singletonObject = new GameObject(typeof(T).Name);
                        _instance = singletonObject.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            // 确保实例唯一
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this as T;
        }
    }

    /// <summary>
    /// 持久化单例，在场景切换时，实例不会被销毁
    /// </summary>
    public class MonoSingletonPersistent<T> : MonoSingleton<T> where T : MonoBehaviour
    {
        protected override void Awake()
        {
            base.Awake();
            if (gameObject != null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}
