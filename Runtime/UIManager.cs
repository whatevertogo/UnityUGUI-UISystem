using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whatevertogo.UISystem
{
    public sealed class UIManager : MonoBehaviour
    {
        private sealed class ViewEntry
        {
            public UIView View;
            public UILayer Layer;
        }

        [SerializeField] private UIViewCatalog catalog;
        [SerializeField] private RectTransform layerContainer;

        private readonly Dictionary<Type, ViewEntry> _openViews = new Dictionary<Type, ViewEntry>();
        private readonly Dictionary<UILayer, List<UIView>> _stacks = new Dictionary<UILayer, List<UIView>>();
        private readonly Dictionary<UILayer, RectTransform> _roots = new Dictionary<UILayer, RectTransform>();
        private IUIViewFactory _factory;
        private bool _initialized;
        private bool _shuttingDown;

        public int OpenViewCount => _openViews.Count;

        public void SetFactory(IUIViewFactory factory)
        {
            if (_initialized || _openViews.Count > 0)
                throw new InvalidOperationException("The UI factory must be set before initialization.");

            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private void Awake()
        {
            Initialize();
        }

        public T Open<T>(UIArguments arguments = null) where T : UIView
        {
            Initialize();
            var type = typeof(T);
            if (_openViews.TryGetValue(type, out var existing))
            {
                BringToFront(existing, arguments);
                return (T)existing.View;
            }

            var view = _factory.Create(type, transform);
            if (!(view is T typedView))
            {
                _factory.Release(view);
                throw new InvalidOperationException($"Factory returned the wrong view type for {type.FullName}.");
            }

            var layer = view.Layer;
            var stack = _stacks[layer];
            view.transform.SetParent(_roots[layer], false);

            if (view.Exclusive)
                CloseAllInLayer(layer);
            else if (stack.Count > 0)
                stack[stack.Count - 1].Cover();

            try
            {
                view.Create();
                stack.Add(view);
                _openViews.Add(type, new ViewEntry { View = view, Layer = layer });
                view.Open(arguments);
                return typedView;
            }
            catch
            {
                stack.Remove(view);
                _openViews.Remove(type);
                _factory.Release(view);
                if (stack.Count > 0)
                    stack[stack.Count - 1].Resume();
                throw;
            }
        }

        public T Get<T>() where T : UIView
        {
            return _openViews.TryGetValue(typeof(T), out var entry) ? (T)entry.View : null;
        }

        public bool IsOpen<T>() where T : UIView
        {
            return _openViews.ContainsKey(typeof(T));
        }

        public bool Close<T>() where T : UIView
        {
            if (!_openViews.TryGetValue(typeof(T), out var entry))
                return false;

            CloseEntry(entry, resumeNext: true);
            return true;
        }

        public bool CloseTop(UILayer layer = UILayer.Normal)
        {
            Initialize();
            var stack = _stacks[layer];
            if (stack.Count == 0)
                return false;

            var view = stack[stack.Count - 1];
            CloseEntry(_openViews[view.GetType()], resumeNext: true);
            return true;
        }

        public bool HandleBack(UILayer layer = UILayer.Normal)
        {
            Initialize();
            var stack = _stacks[layer];
            if (stack.Count == 0 || !stack[stack.Count - 1].CanGoBack)
                return false;

            return CloseTop(layer);
        }

        public void CloseAll()
        {
            if (!_initialized)
                return;

            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
                CloseAllInLayer(layer);
        }

        private void Initialize()
        {
            if (_initialized)
                return;

            if (_factory == null)
            {
                if (catalog == null)
                    throw new InvalidOperationException("UIManager requires a catalog or an injected factory.");
                _factory = catalog.CreateFactory();
            }

            var parent = layerContainer == null ? transform : layerContainer;
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var rootObject = new GameObject($"{layer} Layer", typeof(RectTransform));
                var root = (RectTransform)rootObject.transform;
                root.SetParent(parent, false);
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
                _roots.Add(layer, root);
                _stacks.Add(layer, new List<UIView>());
            }

            _initialized = true;
        }

        private void BringToFront(ViewEntry entry, UIArguments arguments)
        {
            var stack = _stacks[entry.Layer];
            var currentTop = stack[stack.Count - 1];
            if (currentTop != entry.View)
            {
                currentTop.Cover();
                stack.Remove(entry.View);
                stack.Add(entry.View);
                entry.View.transform.SetAsLastSibling();
                entry.View.Resume();
            }

            entry.View.Open(arguments);
        }

        private void CloseEntry(ViewEntry entry, bool resumeNext)
        {
            var stack = _stacks[entry.Layer];
            var wasTop = stack.Count > 0 && stack[stack.Count - 1] == entry.View;
            stack.Remove(entry.View);
            _openViews.Remove(entry.View.GetType());
            try
            {
                entry.View.Close();
            }
            finally
            {
                _factory.Release(entry.View);
                if (resumeNext && wasTop && stack.Count > 0)
                    stack[stack.Count - 1].Resume();
            }
        }

        private void CloseAllInLayer(UILayer layer)
        {
            var stack = _stacks[layer];
            for (var index = stack.Count - 1; index >= 0; index--)
            {
                var view = stack[index];
                _openViews.Remove(view.GetType());
                try
                {
                    view.Close();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, view);
                }
                finally
                {
                    _factory.Release(view);
                }
            }

            stack.Clear();
        }

        private void OnDestroy()
        {
            if (!_initialized || _shuttingDown)
                return;

            _shuttingDown = true;
            CloseAll();
        }
    }
}
