using UnityEngine;

namespace XGame.Core
{
    /// <summary>
    /// UI 界面基类
    /// </summary>
    public abstract class UIPanelBase : MonoBehaviour
    {
        private int _id;
        private string _parentLayer;
        private bool _isInitialized;
        private bool _isVisible;
        private bool _isPaused;

        public int ID => _id;
        public string ParentLayer => _parentLayer;
        public bool IsInitialized => _isInitialized;
        public bool IsVisible => _isVisible;
        public bool IsPaused => _isPaused;

        public void Init(int id, string parentLayer)
        {
            _id = id;
            _parentLayer = parentLayer;

            SetVisibilityInternal(false);
            _isPaused = false;

            OnInit();

            _isInitialized = true;
        }

        public void Clear()
        {
            OnClear();

            _isInitialized = false;
        }

        public void Show()
        {
            SetVisibilityInternal(true);
            OnShow();

            _isVisible = true;
        }

        public void Hide()
        {
            SetVisibilityInternal(false);
            OnHide();

            _isVisible = false;
        }

        public void Pause()
        {
            OnPause();

            _isPaused = true;
        }

        public void Resume()
        {
            OnResume();

            _isPaused = false;
        }

        protected virtual void OnInit()
        {
        }

        protected virtual void OnShow()
        {
        }

        protected virtual void OnHide()
        {
        }

        protected virtual void OnPause()
        {
        }

        protected virtual void OnResume()
        {
        }

        protected virtual void OnClear()
        {
        }

        private void SetVisibilityInternal(bool isVisible)
        {
            gameObject.SetActive(isVisible);

            _isVisible = isVisible;
        }
    }
}
