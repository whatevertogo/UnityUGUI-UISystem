using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whatevertogo.UISystem
{
    public sealed class PrefabUIViewFactory : IUIViewFactory
    {
        private readonly Dictionary<Type, UIView> _prefabs = new Dictionary<Type, UIView>();

        public PrefabUIViewFactory(IEnumerable<UIView> prefabs)
        {
            if (prefabs == null)
                throw new ArgumentNullException(nameof(prefabs));

            foreach (var prefab in prefabs)
            {
                if (prefab == null)
                    continue;

                var type = prefab.GetType();
                if (_prefabs.ContainsKey(type))
                    throw new InvalidOperationException($"Duplicate UI prefab type: {type.FullName}");

                _prefabs.Add(type, prefab);
            }
        }

        public UIView Create(Type viewType, Transform parent)
        {
            if (viewType == null)
                throw new ArgumentNullException(nameof(viewType));
            if (!_prefabs.TryGetValue(viewType, out var prefab))
                throw new KeyNotFoundException($"No UI prefab is registered for {viewType.FullName}.");

            return UnityEngine.Object.Instantiate(prefab, parent, false);
        }

        public void Release(UIView view)
        {
            if (view == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(view.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(view.gameObject);
        }
    }
}
