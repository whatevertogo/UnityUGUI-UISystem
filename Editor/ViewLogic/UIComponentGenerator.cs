using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI 组件代码生成器 - 优化版本
/// - 模块化设计，职责分离
/// - 性能优化的组件扫描
/// - 可配置的代码模板
/// - 增强的错误处理
/// </summary>
public class UIComponentGenerator : EditorWindow
{
    // 核心组件
    private UIComponentConfig config;
    private UIComponentScanner scanner;
    private UICodeTemplate codeTemplate;
    private UIComponentBinder binder;

    // 配置参数
    private GameObject selectedObject;
    private string customNamespace = "Game.UI";
    private string viewScriptPath = "Assets/Scripts/UI/Views";
    private string logicScriptPath = "Assets/Scripts/UI/Logic";

    // 自动添加/持久化选项
    private bool autoAddComponents = true;
    private bool autoPersistBindings = false;
    private const string EditorPrefAutoAdd = "UIComponentGenerator_AutoAdd";
    private const string EditorPrefPersist = "UIComponentGenerator_PersistBindings";



    // 状态跟踪
    private bool isProcessing = false;
    private string lastError = string.Empty;
    private bool _initialized = false;
    [MenuItem("Tools/UI Component Generator")]
    public static void ShowWindow()
    {
        GetWindow<UIComponentGenerator>("UI Component Generator");
    }

    private void OnEnable()
    {
    }

    /// <summary>
    /// 延迟初始化，保证安全
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        InitializeComponents();

        // 加载用户偏好
        autoAddComponents = EditorPrefs.GetBool(EditorPrefAutoAdd, true);
        autoPersistBindings = EditorPrefs.GetBool(EditorPrefPersist, false);
    }




    /// <summary>
    /// 初始化核心组件
    /// </summary>
    private void InitializeComponents()
    {
        config = new UIComponentConfig();
        scanner = new UIComponentScanner(config);
        codeTemplate = new UICodeTemplate(config);
        binder = new UIComponentBinder(customNamespace);
    }

    private void OnGUI()
    {
        EnsureInitialized();


        DrawHeader();
        DrawObjectSelection();
        DrawConfiguration();
        DrawActionButtons();
        DrawStatus();
    }

    /// <summary>
    /// 绘制标题区域
    /// </summary>
    private void DrawHeader()
    {
        GUILayout.Label("UI 组件代码生成器（优化版）", EditorStyles.boldLabel);
        GUILayout.Label("模块化设计 | 性能优化 | 可配置模板", EditorStyles.miniLabel);
        GUILayout.Space(10);
    }

    /// <summary>
    /// 绘制对象选择区域
    /// </summary>
    private void DrawObjectSelection()
    {
        GUILayout.Label("目标对象", EditorStyles.boldLabel);

        GameObject currentSelection = Selection.activeGameObject;
        if (IsValidUIObject(currentSelection))
        {
            EditorGUILayout.HelpBox($"当前选中: {currentSelection.name}", MessageType.Info);
            if (GUILayout.Button("使用选中的对象", GUILayout.Height(25)))
            {
                selectedObject = currentSelection;
                GUI.FocusControl(null); // 清除焦点
            }
        }

        selectedObject = (GameObject)EditorGUILayout.ObjectField("目标 UI 对象", selectedObject, typeof(GameObject), true);
        GUILayout.Space(10);
    }

    /// <summary>
    /// 绘制配置区域
    /// </summary>
    private void DrawConfiguration()
    {
        GUILayout.Label("配置选项", EditorStyles.boldLabel);

        string newNamespace = EditorGUILayout.TextField("命名空间", customNamespace);
        if (newNamespace != customNamespace)
        {
            customNamespace = newNamespace;
            binder = new UIComponentBinder(customNamespace); // 重新创建绑定器
        }

        viewScriptPath = EditorGUILayout.TextField("View 层路径", viewScriptPath);
        logicScriptPath = EditorGUILayout.TextField("Logic 层路径", logicScriptPath);
        GUILayout.Space(6);
        GUILayout.Label("自动添加选项", EditorStyles.boldLabel);
        bool newAutoAdd = EditorGUILayout.Toggle("自动 Add 生成的组件", autoAddComponents);
        bool newPersist = EditorGUILayout.Toggle(new GUIContent("持久化绑定引用", "在 Editor 中把找到的引用写回为序列化字段（会修改 Prefab/场景）"), autoPersistBindings);

        if (newAutoAdd != autoAddComponents || newPersist != autoPersistBindings)
        {
            autoAddComponents = newAutoAdd;
            autoPersistBindings = newPersist;
            EditorPrefs.SetBool(EditorPrefAutoAdd, autoAddComponents);
            EditorPrefs.SetBool(EditorPrefPersist, autoPersistBindings);
        }

        GUILayout.Space(10);
    }

    /// <summary>
    /// 绘制操作按钮
    /// </summary>
    private void DrawActionButtons()
    {
        GUILayout.Label("操作", EditorStyles.boldLabel);

        GameObject currentSelection = Selection.activeGameObject;
        bool canGenerate = IsValidUIObject(currentSelection) && !isProcessing;

        EditorGUI.BeginDisabledGroup(!canGenerate);

        // 主生成按钮
        if (GUILayout.Button($"生成脚本 ({(currentSelection != null ? currentSelection.name : "无选中")})", GUILayout.Height(35)))
        {
            GenerateUIScripts(currentSelection);
        }

        // 仅生成 View 按钮
        if (GUILayout.Button($"仅重新生成 View ({(currentSelection != null ? currentSelection.name : "无选中")})", GUILayout.Height(25)))
        {
            GenerateViewScriptOnly(currentSelection);
        }

        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);
    }

    /// <summary>
    /// 绘制状态信息
    /// </summary>
    private void DrawStatus()
    {
        if (isProcessing)
        {
            EditorGUILayout.HelpBox("正在处理中...", MessageType.Info);
        }

        if (!string.IsNullOrEmpty(lastError))
        {
            EditorGUILayout.HelpBox($"错误: {lastError}", MessageType.Error);
            if (GUILayout.Button("清除错误信息"))
            {
                lastError = string.Empty;
            }
        }
    }

    /// <summary>
    /// 检查是否为有效的 UI 对象
    /// </summary>
    private bool IsValidUIObject(GameObject obj)
    {
        return obj != null && (obj.GetComponent<Canvas>() != null || obj.transform.parent?.GetComponentInParent<Canvas>() != null);
    }

    /// <summary>
    /// 生成 UI 脚本（View + Logic）
    /// </summary>
    private void GenerateUIScripts(GameObject uiObject)
    {
        if (isProcessing) return;

        try
        {
            isProcessing = true;
            lastError = string.Empty;

            // 扫描 UI 组件
            var components = scanner.ScanUIComponents(uiObject);

            if (components.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "未找到任何 UI 组件", "OK");
                return;
            }

            // 智能处理 View 名称：避免 "ViewView" 后缀，并确保类名与文件名一致
            string viewClassName = uiObject.name.EndsWith("View") ? uiObject.name : uiObject.name + "View";

            // 生成代码
            // 注意：这里传递 viewClassName 给模板，确保生成的 class XXX : UIViewBase 与文件名一致
            string viewCode = codeTemplate.GenerateViewCode(viewClassName, components, customNamespace);
            string logicCode = codeTemplate.GenerateLogicCode(uiObject.name, viewClassName, components, customNamespace);

            // 创建目录并保存文件
            CreateDirectoriesAndSaveFiles(uiObject.name, viewClassName, viewCode, logicCode);

            EditorUtility.DisplayDialog("成功",
                $"已生成脚本并自动添加到对象：\n\nView: {Path.Combine(viewScriptPath, viewClassName + ".cs")}\n\nLogic: {Path.Combine(logicScriptPath, uiObject.name + "Logic.cs")}",
                "OK");
        }
        catch (System.Exception ex)
        {
            lastError = ex.Message;
            Debug.LogError($"[UIComponentGenerator] 生成脚本失败: {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog("错误", $"生成脚本失败: {ex.Message}", "OK");
        }
        finally
        {
            isProcessing = false;
        }
    }

    /// <summary>
    /// 仅生成 View 脚本
    /// </summary>
    private void GenerateViewScriptOnly(GameObject uiObject)
    {
        if (isProcessing) return;

        try
        {
            isProcessing = true;
            lastError = string.Empty;

            var components = scanner.ScanUIComponents(uiObject);

            if (components.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "未找到任何 UI 组件", "OK");
                return;
            }

            string viewClassName = uiObject.name.EndsWith("View") ? uiObject.name : uiObject.name + "View";
            string viewCode = codeTemplate.GenerateViewCode(viewClassName, components, customNamespace);

            // 创建目录并保存文件
            CreateDirectoryAndSaveViewFile(uiObject.name, viewClassName, viewCode);

            EditorUtility.DisplayDialog("成功",
                $"已重新生成 View 并自动绑定：\n\nView: {Path.Combine(viewScriptPath, viewClassName + ".cs")}",
                "OK");
        }
        catch (System.Exception ex)
        {
            lastError = ex.Message;
            Debug.LogError($"[UIComponentGenerator] 生成 View 脚本失败: {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog("错误", $"生成 View 脚本失败: {ex.Message}", "OK");
        }
        finally
        {
            isProcessing = false;
        }
    }

    /// <summary>
    /// 创建目录并保存文件
    /// </summary>
    private void CreateDirectoriesAndSaveFiles(string uiObjectName, string viewClassName, string viewCode, string logicCode)
    {
        // 创建目录
        Directory.CreateDirectory(viewScriptPath);
        Directory.CreateDirectory(logicScriptPath);

        // 保存文件 (文件名必须与类名一致)
        string viewPath = Path.Combine(viewScriptPath, $"{viewClassName}.cs");
        string logicPath = Path.Combine(logicScriptPath, $"{uiObjectName}Logic.cs");

        File.WriteAllText(viewPath, viewCode, Encoding.UTF8);
        File.WriteAllText(logicPath, logicCode, Encoding.UTF8);

        // 存储挂载任务到 SessionState
        if (autoAddComponents && selectedObject != null)
        {
            SessionState.SetString("PendingUIBinding_Name", uiObjectName);
            SessionState.SetString("PendingUIBinding_ViewClass", viewClassName); // 存储 View 类名供 Binder 使用
            SessionState.SetInt("PendingUIBinding_ID", selectedObject.GetInstanceID());
            SessionState.SetBool("PendingUIBinding_Persist", autoPersistBindings);
            SessionState.SetInt("PendingUIBinding_Mode", 0); // 0 = View + Logic (默认)
            SessionState.SetString("PendingUIBinding_Namespace", customNamespace);
        }

        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 编译完成后回调：执行脚本挂载
    /// </summary>
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        string uiObjectName = SessionState.GetString("PendingUIBinding_Name", string.Empty);
        string viewClassName = SessionState.GetString("PendingUIBinding_ViewClass", string.Empty);
        int objectID = SessionState.GetInt("PendingUIBinding_ID", 0);

        if (string.IsNullOrEmpty(uiObjectName) || objectID == 0) return;

        // 清除状态
        SessionState.EraseString("PendingUIBinding_Name");
        SessionState.EraseString("PendingUIBinding_ViewClass");
        SessionState.EraseInt("PendingUIBinding_ID");
        bool persist = SessionState.GetBool("PendingUIBinding_Persist", false);
        int mode = SessionState.GetInt("PendingUIBinding_Mode", 0);
        string ns = SessionState.GetString("PendingUIBinding_Namespace", "Game.UI");

        // 查找对象
        GameObject targetObject = EditorUtility.InstanceIDToObject(objectID) as GameObject;
        if (targetObject == null)
        {
            Debug.LogWarning("[UIComponentGenerator] 找不到原始目标对象，无法自动挂载脚本。");
            return;
        }

        Debug.Log($"[UIComponentGenerator] 编译完成，开始为 '{targetObject.name}' 挂载脚本...");

        // 执行绑定
        var config = new UIComponentConfig();
        var scanner = new UIComponentScanner(config);
        var binder = new UIComponentBinder(ns);

        var components = scanner.ScanUIComponents(targetObject);
        // 传递 viewClassName
        binder.AutoAddScriptsAndBindComponents(targetObject, components, "", true, persist, mode, viewClassName);

        Debug.Log($"[UIComponentGenerator] 脚本挂载完成！");
    }

    /// <summary>
    /// 创建目录并保存 View 文件
    /// </summary>
    private void CreateDirectoryAndSaveViewFile(string uiObjectName, string viewClassName, string viewCode)
    {
        // 创建目录
        Directory.CreateDirectory(viewScriptPath);

        // 保存文件
        string viewPath = Path.Combine(viewScriptPath, $"{viewClassName}.cs");
        File.WriteAllText(viewPath, viewCode, Encoding.UTF8);

        // 存储挂载任务到 SessionState (仅 View)
        if (autoAddComponents && selectedObject != null)
        {
            SessionState.SetString("PendingUIBinding_Name", uiObjectName);
            SessionState.SetString("PendingUIBinding_ViewClass", viewClassName);
            SessionState.SetInt("PendingUIBinding_ID", selectedObject.GetInstanceID());
            SessionState.SetBool("PendingUIBinding_Persist", autoPersistBindings);
            SessionState.SetInt("PendingUIBinding_Mode", 1); // 强制 View Only
            SessionState.SetString("PendingUIBinding_Namespace", customNamespace);
        }

        AssetDatabase.Refresh();
    }
}

