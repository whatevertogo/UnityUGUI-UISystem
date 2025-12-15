using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using TMPro;

/// <summary>
/// UI 组件代码生成器
/// - 自动扫描子物体的 UI 组件
/// - 生成 View 层（组件绑定）和 Logic 层（业务逻辑）
/// - 支持 Button、Text、Image、InputField、Toggle、Slider 等
/// </summary>
public class UIComponentGenerator : EditorWindow
{
    private GameObject selectedObject;
    private string customNamespace = "Game.UI";
    private string viewScriptPath = "Assets/Scripts/UI/Views";
    private string logicScriptPath = "Assets/Scripts/UI/Logic";

    [MenuItem("Tools/UI Component Generator")]
    public static void ShowWindow()
    {
        GetWindow<UIComponentGenerator>("UI Component Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("UI 组件代码生成器（View + Logic）", EditorStyles.boldLabel);

        // 显示当前选中的对象
        GameObject currentSelection = Selection.activeGameObject;
        if (currentSelection != null && currentSelection.GetComponent<Canvas>() != null)
        {
            EditorGUILayout.HelpBox($"当前选中: {currentSelection.name}", MessageType.Info);
            if (GUILayout.Button("使用选中的对象", GUILayout.Height(30)))
            {
                selectedObject = currentSelection;
            }
        }

        selectedObject = (GameObject)EditorGUILayout.ObjectField("目标 UI 对象", selectedObject, typeof(GameObject), true);

        customNamespace = EditorGUILayout.TextField("命名空间默认为 UI", customNamespace);

        GUILayout.Space(10);
        GUILayout.Label("文件生成路径", EditorStyles.boldLabel);
        viewScriptPath = EditorGUILayout.TextField("View 层路径", viewScriptPath);
        logicScriptPath = EditorGUILayout.TextField("Logic 层路径", logicScriptPath);

        GUILayout.Space(10);

        // 快捷生成按钮 - 直接使用当前选中的对象
        EditorGUI.BeginDisabledGroup(currentSelection == null || currentSelection.GetComponent<Canvas>() == null);
        if (GUILayout.Button($"为选中对象生成脚本 ({(currentSelection != null ? currentSelection.name : "无选中")})", GUILayout.Height(40)))
        {
            if (currentSelection != null)
            {
                selectedObject = currentSelection;
                GenerateUIScripts(currentSelection);
            }
        }
        EditorGUI.EndDisabledGroup();
    }

    private void GenerateUIScripts(GameObject uiObject)
    {
        // 扫描 UI 组件
        var components = ScanUIComponents(uiObject);

        if (components.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "未找到任何 UI 组件", "OK");
            return;
        }

        string viewCode = GenerateViewCode(uiObject, components);
        string logicCode = GenerateLogicCode(uiObject, components);

        // 创建目录
        Directory.CreateDirectory(viewScriptPath);
        Directory.CreateDirectory(logicScriptPath);

        // 保存到指定路径
        string viewPath = Path.Combine(viewScriptPath, $"{uiObject.name}View.cs");
        string logicPath = Path.Combine(logicScriptPath, $"{uiObject.name}Logic.cs");

        File.WriteAllText(viewPath, viewCode, Encoding.UTF8);
        File.WriteAllText(logicPath, logicCode, Encoding.UTF8);

        AssetDatabase.Refresh();

        // 自动添加脚本到 GameObject 并绑定组件（支持等待编译完成）
        AutoAddScriptsAndBindComponents(uiObject, components, viewPath);

        EditorUtility.DisplayDialog("成功", $"已生成脚本并自动添加到对象：\n\nView: {viewPath}\n\nLogic: {logicPath}", "OK");
    }

    /// <summary>
    /// 自动添加脚本到 GameObject 并绑定组件
    /// </summary>
    private void AutoAddScriptsAndBindComponents(GameObject uiObject, Dictionary<string, (string path, string type)> components, string viewPath)
    {
        // 获取或添加 View 脚本
        string scriptName = uiObject.name + "View";
        var viewScriptType = GetTypeFromAssembly(scriptName);

        // 检查是否为 prefab asset（或实例关联到 prefab）
        string prefabAssetPath = null;
        try { prefabAssetPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(uiObject); } catch { prefabAssetPath = null; }

        // 如果是 prefab asset，使用 PrefabUtility.LoadPrefabContents 修改 prefab
        if (!string.IsNullOrEmpty(prefabAssetPath))
        {
            // 确保编译刷新后再尝试绑定类型
            if (viewScriptType == null)
            {
                // 可能脚本尚未编译，延迟重试
                AssetDatabase.Refresh();
                EditorApplication.delayCall += () => AutoAddScriptsAndBindComponents(uiObject, components, viewPath);
                return;
            }

            var prefabRoot = UnityEditor.PrefabUtility.LoadPrefabContents(prefabAssetPath);
            var existing = prefabRoot.GetComponent(viewScriptType) as MonoBehaviour;
            if (existing == null)
            {
                existing = prefabRoot.AddComponent(viewScriptType) as MonoBehaviour;
            }

            // 尝试添加 Logic 组件到 prefab（如果生成器已生成并已编译）
            var logicType = GetTypeFromAssembly(uiObject.name + "Logic");
            if (logicType != null)
            {
                var existingLogic = prefabRoot.GetComponent(logicType) as MonoBehaviour;
                if (existingLogic == null)
                {
                    prefabRoot.AddComponent(logicType);
                }
            }

            if (existing != null)
            {
                // 在 prefab 内容上进行字段绑定
                BindComponentsToGameObject(prefabRoot, components);
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabAssetPath);
            }

            UnityEditor.PrefabUtility.UnloadPrefabContents(prefabRoot);
            AssetDatabase.Refresh();
            return;
        }

        // 非 prefab（场景实例）流程
        MonoBehaviour scriptInstance = null;
        if (viewScriptType != null)
        {
            scriptInstance = uiObject.GetComponent(viewScriptType) as MonoBehaviour;
            if (scriptInstance == null)
            {
                scriptInstance = uiObject.AddComponent(viewScriptType) as MonoBehaviour;
            }
        }

        if (scriptInstance != null)
        {
            BindComponentsToGameObject(uiObject, components);
            // 对场景实例也尝试添加 Logic 组件（如果类型已存在）
            var logicTypeScene = GetTypeFromAssembly(uiObject.name + "Logic");
            if (logicTypeScene != null && uiObject.GetComponent(logicTypeScene) == null)
            {
                uiObject.AddComponent(logicTypeScene);
            }
            EditorUtility.SetDirty(scriptInstance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorApplication.delayCall += () =>
            {
                if (uiObject != null)
                {
                    BindComponentsToGameObject(uiObject, components);
                }
            };
        }
        else
        {
            Debug.LogWarning($"[UIComponentGenerator] 脚本类型 {scriptName} 未找到，稍后可能需要手动添加。请检查脚本是否已编译。");
        }
    }

    /// <summary>
    /// 从程序集获取类型
    /// </summary>
    private System.Type GetTypeFromAssembly(string typeName)
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(customNamespace + "." + typeName);
            if (type != null) return type;

            type = assembly.GetType(typeName);
            if (type != null) return type;
        }
        return null;
    }

    /// <summary>
    /// 绑定组件到 GameObject 的脚本字段
    /// </summary>
    private void BindComponentsToGameObject(GameObject uiObject, Dictionary<string, (string path, string type)> components)
    {
        string scriptName = $"{uiObject.name}View";
        var component = uiObject.GetComponent(scriptName) as Component;
        if (component == null)
        {
            // 如果没有通过名字获取到 component，尝试通过类型查找
            var t = GetTypeFromAssembly(scriptName);
            if (t != null) component = uiObject.GetComponent(t) as Component;
        }
        if (component == null) return;
        var serializedObject = new SerializedObject(component);

        foreach (var kvp in components)
        {
            string fieldName = kvp.Key;
            string path = kvp.Value.path;
            string type = kvp.Value.type;
            Transform targetTransform = uiObject.transform.Find(path);
            if (targetTransform != null)
            {
                Component targetComponent = GetComponentOfType(targetTransform, type);
                if (targetComponent != null)
                {
                    var property = serializedObject.FindProperty(fieldName);
                    if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        property.objectReferenceValue = targetComponent;
                    }
                }
            }
        }
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(component);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 扫描 UI 对象的所有子物体，找出 UI 组件
    /// </summary>
    private Dictionary<string, (string path, string type)> ScanUIComponents(GameObject uiObject)
    {
        var components = new Dictionary<string, (string, string)>();

        foreach (Transform child in uiObject.GetComponentsInChildren<Transform>(true))
        {
            if (child == uiObject.transform) continue; // 跳过自身

            // 获取所有 UI 组件类型
            var componentTypes = GetAllUIComponentTypes(child);
            string path = GetRelativePath(uiObject.transform, child);

            foreach (var componentType in componentTypes)
            {
                // 用 GameObject 的实际名字 + 组件类型作为变量名
                string baseName = MakeSafeVariableName(child.gameObject.name);
                string varName = componentTypes.Count > 1
                    ? $"{baseName}{componentType}" // 多个组件时加后缀，如 titleText, titleImage
                    : baseName; // 单个组件直接用名字

                // 避免重名
                int suffix = 1;
                string finalName = varName;
                while (components.ContainsKey(finalName))
                {
                    finalName = $"{varName}{suffix++}";
                }

                components[finalName] = (path, componentType);
            }
        }

        return components;
    }

    private string GetRelativePath(Transform root, Transform target)
    {
        var path = target.name;
        var current = target.parent;
        while (current != null && current != root)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }
        return path;
    }

    /// <summary>
    /// 获取物体上所有 UI 组件类型
    /// </summary>
    private List<string> GetAllUIComponentTypes(Transform child)
    {
        var types = new List<string>();

        if (child.GetComponent<Button>() != null) types.Add("Button");
        if (child.GetComponent<Text>() != null) types.Add("Text");
        if (child.GetComponent<Image>() != null) types.Add("Image");
        if (child.GetComponent<InputField>() != null) types.Add("InputField");
        if (child.GetComponent<Toggle>() != null) types.Add("Toggle");
        if (child.GetComponent<Slider>() != null) types.Add("Slider");
        if (child.GetComponent<ScrollRect>() != null) types.Add("ScrollRect");
        if (child.GetComponent<Dropdown>() != null) types.Add("Dropdown");
        if (child.GetComponent<TMP_Text>() != null) types.Add("TMP_Text");
        if (child.GetComponent<TMP_InputField>() != null) types.Add("TMP_InputField");

        return types;
    }

    /// <summary>
    /// 获取指定类型的组件实例
    /// </summary>
    private Component GetComponentOfType(Transform transform, string componentTypeName)
    {
        switch (componentTypeName)
        {
            case "Button": return transform.GetComponent<Button>();
            case "Text": return transform.GetComponent<Text>();
            case "Image": return transform.GetComponent<Image>();
            case "InputField": return transform.GetComponent<InputField>();
            case "Toggle": return transform.GetComponent<Toggle>();
            case "Slider": return transform.GetComponent<Slider>();
            case "ScrollRect": return transform.GetComponent<ScrollRect>();
            case "Dropdown": return transform.GetComponent<Dropdown>();
            case "TMP_Text": return transform.GetComponent<TMP_Text>();
            case "TMP_InputField": return transform.GetComponent<TMP_InputField>();
            default: return null;
        }
    }

    /// <summary>
    /// 生成安全的变量名
    /// </summary>
    private string MakeSafeVariableName(string name)
    {
        // 去除特殊字符，首字母小写
        var safeName = System.Text.RegularExpressions.Regex.Replace(name, "[^a-zA-Z0-9]", "");
        if (safeName.Length == 0) safeName = "element";
        return char.ToLower(safeName[0]) + safeName.Substring(1);
    }

    /// <summary>
    /// 生成 View 层代码
    /// </summary>
    private string GenerateViewCode(GameObject uiObject, Dictionary<string, (string path, string type)> components)
    {
        StringBuilder sb = new StringBuilder();

        // 头部
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using TMPro;");
        sb.AppendLine("using UI;");
        sb.AppendLine("using System;");
        sb.AppendLine("");
        if (!string.IsNullOrEmpty(customNamespace))
        {
            sb.AppendLine($"namespace {customNamespace}");
            sb.AppendLine("{");
        }

        // 类定义
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// {uiObject.name} View 层 - UI 组件绑定");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public partial class {uiObject.name}View : UIViewBase");
        sb.AppendLine("    {");

        // 声明组件字段
        sb.AppendLine($"        // UI Components");
        foreach (var kvp in components)
        {
            sb.AppendLine($"        [SerializeField] private {kvp.Value.type} {kvp.Key};");
        }

        // 初始化方法
        sb.AppendLine("");
        sb.AppendLine($"        // 在运行时创建阶段进行自动绑定（UIManager 会调用 OnCreate）");
        sb.AppendLine("        protected override void OnCreate()");
        sb.AppendLine("        {");
        sb.AppendLine("            BindComponentsAtRuntime();");
        sb.AppendLine("        }");

        // 绑定组件方法
        sb.AppendLine("");
        sb.AppendLine("        private void BindComponents() { } // Editor-binding 占位（实际优先运行时绑定）");

        sb.AppendLine("        private void BindComponentsAtRuntime()");
        sb.AppendLine("        {");
        foreach (var kvp in components)
        {
            sb.AppendLine($"            if ({kvp.Key} == null)");
            sb.AppendLine("            {");
            sb.AppendLine($"                var t = transform.Find(\"{kvp.Value.path}\");");
            sb.AppendLine($"                if (t != null) {kvp.Key} = t.GetComponent<{kvp.Value.type}>();");
            sb.AppendLine($"                if ({kvp.Key} == null) Debug.LogWarning(\"[{uiObject.name}View] {kvp.Key} ({kvp.Value.type}) 未绑定\");");
            sb.AppendLine("            }");
        }
        sb.AppendLine("        }");

        // 文本更新方法（如果有 Text/TMP_Text 组件）
        var texts = components.Where(c => c.Value.type == "Text" || c.Value.type == "TMP_Text").ToList();
        if (texts.Count > 0)
        {
            sb.AppendLine("");
            sb.AppendLine($"        /// <summary>更新文本内容</summary>");
            foreach (var txt in texts)
            {
                sb.AppendLine($"        public void Set{CapitalizeFirst(txt.Key)}(string content)");
                sb.AppendLine("        {");
                sb.AppendLine($"            if ({txt.Key} != null) {txt.Key}.text = content;");
                sb.AppendLine($"        }}");
            }
        }

        //按钮更新方法
        var buttons = components.Where(c => c.Value.type == "Button").ToList();
        if (buttons.Count > 0)
        {
            sb.AppendLine("");
            sb.AppendLine("        /// <summary>绑定 Button 事件</summary>");
            foreach (var button in buttons)
            {
                sb.AppendLine($"        public void Bind{CapitalizeFirst(button.Key)}Button(Action onClickAction)");
                sb.AppendLine("        {");
                // 先移除旧监听，避免重复订阅
                sb.AppendLine("            if (" + button.Key + " != null) { " + button.Key + ".onClick.RemoveAllListeners(); " + button.Key + ".onClick.AddListener(() => onClickAction?.Invoke()); }");
                sb.AppendLine("        }");
            }
        }

        // 关闭方法
        sb.AppendLine("");
        sb.AppendLine($"        public void Close()");
        sb.AppendLine("        {");
        sb.AppendLine($"            gameObject.SetActive(false);");
        sb.AppendLine($"        }}");

        sb.AppendLine($"    }}");

        if (!string.IsNullOrEmpty(customNamespace))
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成 Logic 层代码
    /// </summary>
    private string GenerateLogicCode(GameObject uiObject, Dictionary<string, (string path, string type)> components)
    {
        StringBuilder sb = new StringBuilder();

        // 头部
        sb.AppendLine("using System;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UI;");
        sb.AppendLine("");
        if (!string.IsNullOrEmpty(customNamespace))
        {
            sb.AppendLine("namespace " + customNamespace);
            sb.AppendLine("{");
        }

        var buttons = components.Where(c => c.Value.type == "Button").ToList();
        var texts = components.Where(c => c.Value.type == "Text" || c.Value.type == "TMP_Text").ToList();

        // 生成纯逻辑核心（可单元测试）
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// " + uiObject.name + " 纯逻辑核心（可单元测试）");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public class " + uiObject.name + "LogicCore");
        sb.AppendLine("    {");
        sb.AppendLine("        protected " + uiObject.name + "View _view;");
        sb.AppendLine("        public virtual void Bind(UIViewBase view)");
        sb.AppendLine("        {");
        sb.AppendLine("            _view = view as " + uiObject.name + "View;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        public virtual void OnOpen(UIArgs args)");
        sb.AppendLine("        {");
        sb.AppendLine("            // 在此实现打开时的业务逻辑（纯 C#，易于单元测试）");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        public virtual void OnClose()");
        sb.AppendLine("        {");
        sb.AppendLine("            // 关闭时清理");
        sb.AppendLine("            _view = null;");
        sb.AppendLine("        }");

        if (buttons.Count > 0)
        {
            foreach (var button in buttons)
            {
                sb.AppendLine("");
                sb.AppendLine("        public void On" + CapitalizeFirst(button.Key) + "Clicked()");
                sb.AppendLine("        {");
                sb.AppendLine("            // TODO: 处理按钮点击后的业务逻辑（纯逻辑）");
                if (texts.Count > 0)
                {
                    sb.AppendLine("            // 可在此调用 _view.SetXXX 方法更新文本内容");
                }
                sb.AppendLine("        }");
            }
        }

        sb.AppendLine("    }");

        // 生成 MonoBehaviour wrapper，实现 IUILogic 并将调用转发给 LogicCore
        sb.AppendLine("");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// MonoBehaviour Wrapper：创建并持有 LogicCore，在运行时作为 IUILogic 注入到 View");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public class " + uiObject.name + "Logic : MonoBehaviour, IUILogic");
        sb.AppendLine("    {");
        sb.AppendLine("        private " + uiObject.name + "LogicCore _core = new " + uiObject.name + "LogicCore();");
        sb.AppendLine("");
        sb.AppendLine("        public void Bind(UIViewBase view)");
        sb.AppendLine("        {");
        sb.AppendLine("            _core.Bind(view);");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        public void OnOpen(UIArgs args)");
        sb.AppendLine("        {");
        sb.AppendLine("            _core.OnOpen(args);");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        public void OnClose()");
        sb.AppendLine("        {");
        sb.AppendLine("            _core.OnClose();");
        sb.AppendLine("        }");

        if (buttons.Count > 0)
        {
            foreach (var button in buttons)
            {
                sb.AppendLine("");
                sb.AppendLine("        private void On" + CapitalizeFirst(button.Key) + "Clicked()");
                sb.AppendLine("        {");
                sb.AppendLine("            _core.On" + CapitalizeFirst(button.Key) + "Clicked();");
                sb.AppendLine("        }");
            }
        }

        sb.AppendLine("    }");

        if (!string.IsNullOrEmpty(customNamespace))
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }


    /// <summary>
    /// 首字母大写
    /// </summary>
    private string CapitalizeFirst(string str)
    {
        if (str.Length == 0) return str;
        return char.ToUpper(str[0]) + str.Substring(1);
    }
}

