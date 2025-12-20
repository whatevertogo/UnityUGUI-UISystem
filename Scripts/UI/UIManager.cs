using System;
using System.Collections.Generic;
using UnityEngine;
using CDTU.Utils;
using UI.Loading;

namespace UI
{
    /// <summary>
    /// UI 管理器（方案 A）
    /// - Layer 由 UIManager 统一管理
    /// - View 不感知 Layer
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        private class ViewEntry
        {
            public UIViewBase View;
            public UILayer Layer;
        }

        // 当前打开的 UI
        private readonly Dictionary<Type, ViewEntry> _openViews = new();

        // 每个 Layer 对应一个根节点
        private readonly Dictionary<UILayer, Transform> _layerRoots = new();


        #region Unity 生命周期

        protected override void Awake()
        {
            base.Awake();
            CreateLayerRoots();
            Debug.Log("[UIManager] Initialized");
        }

        #endregion

        #region Layer 管理

        private void CreateLayerRoots()
        {
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var go = new GameObject(layer + "Layer");
                go.transform.SetParent(transform, false);
                _layerRoots[layer] = go.transform;
            }
        }

        private Transform GetLayerRoot(UILayer layer)
        {
            return _layerRoots.TryGetValue(layer, out var root)
                ? root
                : transform;
        }

        #endregion

        #region Open / Close

        /// <summary>
        /// 打开 UI
        /// </summary>
        public T Open<T>(UIArgs args = null, UILayer layer = UILayer.Normal, params IUILogic[] logics) where T : UIViewBase
        {
            Type type = typeof(T);

            // 已经打开
            if (_openViews.TryGetValue(type, out var exist))
            {
                Debug.LogWarning($"[UIManager] {type.Name} already opened");
                return exist.View as T;
            }

            // 加载 prefab
            GameObject prefab = UIAssetProvider.Load<T>();
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] UI prefab not found: {type.Name}");
                return null;
            }

            // 实例化
            Transform root = GetLayerRoot(layer);
            GameObject instance = Instantiate(prefab, root, false);

            T view = instance.GetComponent<T>();
            if (view == null)
            {
                Debug.LogError($"[UIManager] {type.Name} missing UIViewBase");
                Destroy(instance);
                return null;
            }

            // 同层互斥
            if (view.Exclusive)
            {
                CloseAllInLayer(layer);
            }

            // 生命周期
            view.OnCreate();

            // 自动收集 Logic
            BindLogicFromHierarchy(view, instance);

            // 手动注入 Logic
            if (logics != null)
            {
                foreach (var logic in logics)
                {
                    view.AddLogic(logic);
                }
            }

            view.OnOpen(args);

            // 注册
            _openViews[type] = new ViewEntry
            {
                View = view,
                Layer = layer
            };

            return view;
        }

        /// <summary>
        /// 关闭指定 UI
        /// </summary>
        public void Close<T>() where T : UIViewBase
        {
            Type type = typeof(T);

            if (_openViews.TryGetValue(type, out var entry))
            {
                entry.View.OnClose();
                Destroy(entry.View.gameObject);
                _openViews.Remove(type);
            }
        }

        /// <summary>
        /// 关闭某一层的所有 UI
        /// </summary>
        private void CloseAllInLayer(UILayer layer)
        {
            var toRemove = new List<Type>();

            foreach (var kvp in _openViews)
            {
                var entry = kvp.Value;
                if (entry.View == null)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                if (entry.Layer == layer)
                {
                    entry.View.OnClose();
                    Destroy(entry.View.gameObject);
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var type in toRemove)
            {
                _openViews.Remove(type);
            }
        }

        #endregion

        #region 查询

        public bool IsOpen<T>() where T : UIViewBase
        {
            return _openViews.ContainsKey(typeof(T));
        }

        #endregion

        #region Logic 绑定

        private void BindLogicFromHierarchy(UIViewBase view, GameObject root)
        {
            var monos = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in monos)
            {
                if (mb is IUILogic logic)
                {
                    view.AddLogic(logic);
                }
            }
        }

        #endregion
    }
}
