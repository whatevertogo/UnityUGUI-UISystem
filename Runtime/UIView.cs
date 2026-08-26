using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Whatevertogo.UISystem
{
    public abstract class UIView : MonoBehaviour
    {
        private sealed class ButtonBinding
        {
            public Button Button;
            public UnityAction Action;
        }

        [SerializeField] private UILayer layer = UILayer.Normal;
        [SerializeField] private bool exclusive;
        [SerializeField] private bool canGoBack = true;

        private readonly List<IUIViewLogic> _logics = new List<IUIViewLogic>();
        private readonly List<ButtonBinding> _buttonBindings = new List<ButtonBinding>();
        private bool _created;
        private bool _open;

        public virtual UILayer Layer => layer;
        public virtual bool Exclusive => exclusive;
        public virtual bool CanGoBack => canGoBack;
        public bool IsOpen => _open;

        public void AddLogic(IUIViewLogic logic)
        {
            if (logic == null || _logics.Contains(logic))
                return;

            logic.Bind(this);
            _logics.Add(logic);
        }

        protected void BindButton(Button button, UnityAction action)
        {
            if (button == null)
                throw new ArgumentNullException(nameof(button));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            button.onClick.AddListener(action);
            _buttonBindings.Add(new ButtonBinding { Button = button, Action = action });
        }

        internal void Create()
        {
            if (_created)
                return;

            _created = true;
            foreach (var behaviour in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is IUIViewLogic logic)
                    AddLogic(logic);
            }

            OnCreated();
        }

        internal void Open(UIArguments arguments)
        {
            if (!_created)
                Create();

            if (_open)
            {
                foreach (var logic in _logics)
                    logic.OnRefresh(arguments);
                OnRefreshed(arguments);
                return;
            }

            _open = true;
            gameObject.SetActive(true);
            foreach (var logic in _logics)
                logic.OnOpen(arguments);
            OnOpened(arguments);
        }

        internal void Cover()
        {
            if (!_open)
                return;

            foreach (var logic in _logics)
                logic.OnCovered();
            OnCovered();
        }

        internal void Resume()
        {
            if (!_open)
                return;

            foreach (var logic in _logics)
                logic.OnResumed();
            OnResumed();
        }

        internal void Close()
        {
            if (!_open)
                return;

            _open = false;
            ClearButtonBindings();
            foreach (var logic in _logics)
                logic.OnClose();
            OnClosed();
        }

        protected virtual void OnCreated() { }
        protected virtual void OnOpened(UIArguments arguments) { }
        protected virtual void OnRefreshed(UIArguments arguments) { }
        protected virtual void OnCovered() { }
        protected virtual void OnResumed() { }
        protected virtual void OnClosed() { }

        private void ClearButtonBindings()
        {
            foreach (var binding in _buttonBindings)
            {
                if (binding.Button != null)
                    binding.Button.onClick.RemoveListener(binding.Action);
            }

            _buttonBindings.Clear();
        }

        protected virtual void OnDestroy()
        {
            try
            {
                Close();
            }
            finally
            {
                ClearButtonBindings();
                _logics.Clear();
            }
        }
    }
}
