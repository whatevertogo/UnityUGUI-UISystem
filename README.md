# Unity UI System (UISystem)

## 简介

这是一个用于 Unity 的轻量级 UI 框架样例，包含 UI 管理器、视图基类、示例视图与 UI 逻辑分离的实现。适合作为小型游戏或项目的 UI 架构参考与快速集成模板。

## 主要特性

- 简单的 UI 管理器与视图生命周期管理
- 视图（UIView）与逻辑（UILogic）分离，便于测试与维护
- 提供示例视图与逻辑（PlayingState）
- 包含编辑器辅助脚本（用于自动化/生成组件）

## 项目结构（关键文件）

- `Editor/UIComponentGenerator.cs` — 编辑器脚本，辅助生成/管理 UI 组件
- `Scripts/UI/UIManager.cs` — 全局 UI 管理器（入口/调度）
- `Scripts/UI/UIViewBase.cs` — 视图基类，所有具体 UI 视图继承自此
- `Scripts/UI/IUILogic.cs` — UI 逻辑接口定义
- `Scripts/UI/IUIManager.cs` — UI 管理器接口定义
- `Scripts/UI/UIArgs.cs` — 视图/逻辑间传递的参数类型
- `Scripts/UI/Enum/UILayerEnum.cs` — UI 分层枚举定义
- `Scripts/UI/Loading/UIAssetProvider.cs` — UI 资源加载提供者（抽象/示例）
- `Scripts/UI/Logic/PlayingStateUILogic.cs` — 示例 UI 逻辑实现
- `Scripts/UI/Views/PlayingStateUIView.cs` — 示例视图实现

（更多辅助 meta 文件存在于工程中，用于 Unity 导入）

## 快速开始（集成到你的 Unity 项目）

1. 将 `Editor/` 与 `Scripts/` 文件夹（或对应文件）复制到你的 Unity 项目的 `Assets/` 下（例如 `Assets/UISystem`）。
2. 打开 Unity，等待编译完成。
3. 查看并参考示例：
   - `Scripts/UI/Views/PlayingStateUIView.cs`
   - `Scripts/UI/Logic/PlayingStateUILogic.cs`
   它们演示了视图与逻辑如何配合。
4. 在需要显示 UI 的地方使用 `UIManager` 提供的接口（项目中有 `IUIManager` / `UIManager`，请参考其实现里暴露的方法进行调用）。

注意：不同项目对 UI 显示/隐藏、资源加载的实现会不同，建议根据 `UIAssetProvider` 的抽象替换为你项目的资源加载实现（Addressables、Resources、AssetBundle 等）。

## 示例与扩展建议

- 新建视图：继承 `UIViewBase`，实现初始化与销毁逻辑。
- 新建逻辑：实现 `IUILogic` 接口，将视图交互事件转发到逻辑层。
- 替换资源加载：实现或扩展 `UIAssetProvider`，接入你项目的资源管理方案。
- 编辑器工具：`UIComponentGenerator` 可用来自动化创建绑定脚本或组件（按需调整）。

## 贡献

欢迎提 issue 与 pull request。建议提交前：

- 保持代码风格一致
- 补充或更新示例
- 添加说明或测试用例，便于后续维护

## 许可

建议使用 MIT 许可证（如果你希望开源且可自由使用）。如果需要我可以生成 `LICENSE` 文件内容。

---

如果你希望，我可以：

- 把上面的 README 生成为 `README.md` 文件并放进项目根目录（已完成）；
- 根据 `UIManager.cs` / `UIViewBase.cs` 的具体实现补充“使用示例代码片段”（我可以打开并读取这些文件以生成精确示例）。请告诉我你要哪一个。
