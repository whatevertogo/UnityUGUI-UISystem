# Whatevertogo uGUI System

一个只负责通用 UI 生命周期和分层栈的 Unity uGUI（Unity 传统 UI）包。

## 安装

在 Unity Package Manager（Unity 包管理器）中添加：

```text
https://github.com/whatevertogo/UnityUGUI-UISystem.git
```

最低版本为 Unity 2021.3。

## 核心边界

- `UIView`：视图配置、按钮绑定和生命周期。
- `IUIViewLogic` / `UIViewLogic`：可选的视图逻辑组件。
- `UIManager`：按层维护打开顺序、覆盖、恢复、返回和独占视图。
- `IUIViewFactory`：资源创建与释放边界。
- `UIViewCatalog` / `PrefabUIViewFactory`：无需额外依赖的预制体目录实现。

核心不依赖 Addressables（可寻址资源系统）、UniTask、DOTween、事件总线、项目单例或任何具体游戏类型。需要其他加载方式时，实现自己的 `IUIViewFactory` 即可。

## 配置

1. 创建 `UIViewCatalog` 资产，将所有 UIView 预制体加入列表。
2. 在 Canvas 下添加 `UIManager`。
3. 把 Catalog 赋给 Manager。
4. 具体页面继承 `UIView`，按需覆盖生命周期。

```csharp
public sealed class InventoryView : UIView
{
    public override UILayer Layer => UILayer.Normal;
    public override bool CanGoBack => true;

    protected override void OnOpened(UIArguments arguments)
    {
        // render
    }
}
```

## 打开、关闭与返回

```csharp
InventoryView view = uiManager.Open<InventoryView>(arguments);

uiManager.Close<InventoryView>();
uiManager.CloseTop(UILayer.Popup);
uiManager.HandleBack();
```

行为约定：

- 每个具体 View 类型最多打开一个实例。
- 重复打开已有实例会将其置顶并触发 Refresh（刷新）。
- 新页面会 Cover（覆盖）同层栈顶；关闭栈顶会 Resume（恢复）下一层页面。
- `Exclusive` 页面打开时关闭同层全部旧页面。
- `CanGoBack == false` 的栈顶不会被 `HandleBack` 关闭。
- Manager 不是全局单例，由场景或依赖注入明确持有。

## Logic（逻辑组件）

View 创建时会自动收集子层级中的 `IUIViewLogic`：

```csharp
public sealed class InventoryLogic : UIViewLogic
{
    public override void OnOpen(UIArguments arguments) { }
    public override void OnCovered() { }
    public override void OnResumed() { }
    public override void OnClose() { }
}
```

按钮可以通过 `BindButton` 绑定，关闭时会自动解绑。
