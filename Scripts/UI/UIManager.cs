using System;
using System.Collections.Generic;
using UnityEngine;
using CDTU.Utils;
using UI.Loading;
using System.Linq;
using System.Threading.Tasks;

namespace UI
{
    /// <summary>
    /// UI 管理器
    /// - 核心优化：使用 List 代替 Stack 维护 View 栈，支持任意位置移除（Close）和置顶（Re-Open）
    /// - 保持 Type -> View 的 O(1) 查找
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        private class ViewEntry
        {
            public UIViewBase View;
            public UILayer Layer;
        }

        /// <summary>
        /// 当前已打开的 View（用于快速查询 Type -> Instance）
        /// </summary>
        private readonly Dictionary<Type, ViewEntry> _openViews = new();

        /// <summary>
        /// 每个 Layer 对应一个 View 列表（模拟栈，Index 大的在顶层）
        /// List 结构允许我们在非栈顶位置移除 UI
        /// </summary>
        private readonly Dictionary<UILayer, List<UIViewBase>> _layerStacks = new();

        /// <summary>
        /// 每个 Layer 对应一个根节点
        /// </summary>
        private readonly Dictionary<UILayer, Transform> _layerRoots = new();

        public static UIAssetProvider UIAssetProvider { get; } = new UIAssetProvider();

        protected override void Awake()
        {
            base.Awake();
            CreateLayerRoots();
            // CDTU.Utils.CDLogger.Log("[UIManager] Initialized (List-Based)");
        }

        #region Layer 管理

        private void CreateLayerRoots()
        {
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var go = new GameObject(layer + "Layer");
                go.transform.SetParent(transform, false);

                _layerRoots[layer] = go.transform;
                _layerStacks[layer] = new List<UIViewBase>();
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
        /// - 自动处理置顶（Bring To Front）
        /// - 自动管理层级栈
        /// </summary>
        public async Task<T> Open<T>(
            UIArgs args = null,
            UILayer layer = UILayer.Normal,
            params IUILogic[] logics
        ) where T : UIViewBase
        {
            Type type = typeof(T);
            var stack = _layerStacks[layer];

            // 1. 已经打开：执行“置顶”逻辑
            if (_openViews.TryGetValue(type, out var existEntry))
            {
                var existView = existEntry.View as T;

                // 安全检查：如果层级不匹配，通常保持原层级或报警告
                if (existEntry.Layer != layer)
                {
                       CDTU.Utils.CDLogger.LogWarning($"[UIManager] Try to open {type.Name} on {layer}, but it's already on {existEntry.Layer}. Keeping original layer. If you intend to move it, call UIManager.Instance.MoveToLayer<{type.Name}>({layer}) or UIManager.Instance.MoveToLayer(view, newLayer).");
                    layer = existEntry.Layer;
                    stack = _layerStacks[layer];
                }

                // 检查是否已经在栈顶（List 末尾）
                bool isTop = stack.Any() && stack[stack.Count - 1] == existView;

                if (!isTop)
                {
                    // 旧的栈顶被覆盖
                    if (stack.Any()) stack[stack.Count - 1].OnCovered();

                    // 移动到 List 末尾（逻辑置顶）
                    stack.Remove(existView);
                    stack.Add(existView);

                    // 移动到 Hierarchy 末尾（视觉置顶）
                    existView.transform.SetAsLastSibling();

                    // 恢复
                    existView.OnResume();
                }

                // 刷新参数
                existView.OnOpen(args);
                return existView;
            }

            // 2. 加载新 UI
            GameObject prefab = await UIAssetProvider.LoadAsync<T>() as GameObject;
            if (prefab == null)
            {
                CDTU.Utils.CDLogger.LogError($"[UIManager] UI prefab not found: {type.Name}");
                return null;
            }

            // 实例化
            Transform root = GetLayerRoot(layer);
            GameObject instance = Instantiate(prefab, root, false);

            T view = instance.GetComponent<T>();
            if (view == null)
            {
                CDTU.Utils.CDLogger.LogError($"[UIManager] {type.Name} missing UIViewBase");
                Destroy(instance);
                return null;
            }

            // 3. 同层互斥：关闭该层所有旧界面
            if (view.Exclusive)
            {
                CloseAllInLayer(layer);
            }
            // 4. 普通覆盖：通知当前栈顶
            else if (stack.Any())
            {
                stack[stack.Count - 1].OnCovered();
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

            // 入栈（List 末尾）
            stack.Add(view);

            // 注册
            _openViews[type] = new ViewEntry
            {
                View = view,
                Layer = layer
            };

            return view;
        }

        public T GetView<T>() where T : UIViewBase
        {
            Type type = typeof(T);
            if (_openViews.TryGetValue(type, out var entry))
            {
                return entry.View as T;
            }
            return null;
        }

        /// <summary>
        /// 关闭栈顶 UI（ESC / 返回）
        /// </summary>
        public void CloseTop(UILayer layer = UILayer.Normal)
        {
            var stack = _layerStacks[layer];
            if (stack.Count == 0)
                return;

            // 取 List 末尾
            var top = stack[stack.Count - 1];
            CloseViewInstance(top, stack);
        }

        /// <summary>
        /// 关闭指定 UI
        /// </summary>
        public void Close<T>() where T : UIViewBase
        {
            Type type = typeof(T);

            if (!_openViews.TryGetValue(type, out var entry))
                return;

            var layer = entry.Layer;
            var stack = _layerStacks[layer];

            CloseViewInstance(entry.View, stack);
        }

        /// <summary>
        /// 统一的关闭实现
        /// </summary>
        private void CloseViewInstance(UIViewBase view, List<UIViewBase> stack)
        {
            if (view == null) return;

            // 检查它是否是栈顶
            bool isTop = stack.Any() && stack[stack.Count - 1] == view;

            // 1. 移除引用
            stack.Remove(view); // O(N) 但 N 很小
            _openViews.Remove(view.GetType());

            // 2. 销毁对象
            view.OnClose();
            Destroy(view.gameObject);

            // 3. 焦点恢复：如果刚才关闭的是栈顶，且还有其他界面，则恢复新的栈顶
            if (isTop && stack.Any())
            {
                var newTop = stack[stack.Count - 1];
                newTop.OnResume();
            }
        }

        /// <summary>
        /// 关闭某一层的所有 UI，包括栈顶和非栈顶
        /// </summary>
        private void CloseAllInLayer(UILayer layer)
        {
            var stack = _layerStacks[layer];

            // 倒序遍历删除，安全且模拟出栈顺序
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                var view = stack[i];
                if (view == null) continue;

                view.OnClose();
                Destroy(view.gameObject);

                // 别忘了清理 _openViews
                _openViews.Remove(view.GetType());
            }

            stack.Clear();
        }

        /// <summary>
        /// 将一个已打开的 View 移动到另一个 Layer（会更新栈、Hierarchy 和 _openViews）
        /// </summary>
        public bool MoveToLayer<T>(UILayer newLayer) where T : UIViewBase
        {
            if (!_openViews.TryGetValue(typeof(T), out var entry))
                return false;
            return MoveToLayer(entry.View, newLayer);
        }

        public bool MoveToLayer(UIViewBase view, UILayer newLayer)
        {
            if (view == null) return false;
            Type type = view.GetType();
            if (!_openViews.TryGetValue(type, out var entry))
                return false;

            var oldLayer = entry.Layer;
            if (oldLayer == newLayer) return true;

            var oldStack = _layerStacks[oldLayer];
            var newStack = _layerStacks[newLayer];

            bool wasTopInOld = oldStack.Any() && oldStack[oldStack.Count - 1] == view;
            oldStack.Remove(view);
            if (wasTopInOld && oldStack.Any())
            {
                oldStack[oldStack.Count - 1].OnResume();
            }

            // 如果目标层是独占性 UI，先关闭该层的所有 UI（不包含自己，因为尚未加入）
            if (view.Exclusive)
            {
                CloseAllInLayer(newLayer);
            }
            else if (newStack.Any())
            {
                newStack[newStack.Count - 1].OnCovered();
            }

            // 重新设置父对象和层级
            view.transform.SetParent(GetLayerRoot(newLayer), false);
            view.transform.SetAsLastSibling();

            newStack.Add(view);
            _openViews[type].Layer = newLayer;

            // 视图恢复/刷新
            view.OnResume();

            return true;
        }

        #endregion

        #region Back / 查询

        /// <summary>
        /// 处理返回（ESC）
        /// </summary>
        public bool HandleBack(UILayer layer = UILayer.Normal)
        {
            var stack = _layerStacks[layer];
            // 如果只有 1 个界面（通常是常驻主界面），可能不允许关闭
            if (stack.Count <= 1)
                return false;

            var top = stack[stack.Count - 1];
            if (!top.CanBack)
                return false;

            CloseTop(layer);
            return true;
        }

        public bool IsOpen<T>() where T : UIViewBase
        {
            return _openViews.ContainsKey(typeof(T));
        }

        public bool HasViewInLayer(UILayer layer)
        {
            return _layerStacks[layer].Count > 0;
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
