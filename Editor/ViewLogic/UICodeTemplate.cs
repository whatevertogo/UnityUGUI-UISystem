using System.Collections.Generic;
using System.Text;
using System.Linq;

/// <summary>
/// UI 代码生成模板 - 负责生成 View 和 Logic 层代码
/// </summary>
public class UICodeTemplate
{
    private UIComponentConfig config;

    public UICodeTemplate(UIComponentConfig config)
    {
        this.config = config;
    }

    /// <summary>
    /// 生成 View 层代码
    /// </summary>
    public string GenerateViewCode(string uiObjectName, Dictionary<string, UIComponentInfo> components, string namespaceName)
    {
        var sb = new StringBuilder();

        // 生成头部
        GenerateViewHeader(sb, namespaceName);

        // 生成类定义
        GenerateViewClassStart(sb, uiObjectName);

        // 生成组件字段声明
        GenerateComponentFields(sb, components);

        //生成必要属性
        GenerateViewExculuseProperty(sb);

        // 生成初始化方法
        GenerateInitializationMethods(sb, uiObjectName, components);

        // 生成组件访问方法
        GenerateComponentAccessMethods(sb, components);

        // 生成关闭方法
        GenerateCloseMethod(sb);

        // 生成类结束
        GenerateViewClassEnd(sb, namespaceName);

        return sb.ToString();
    }

    /// <summary>
    /// 生成 Logic 层代码
    /// </summary>
    public string GenerateLogicCode(string uiObjectName, Dictionary<string, UIComponentInfo> components, string namespaceName)
    {
        var sb = new StringBuilder();

        // 生成头部
        GenerateLogicHeader(sb, namespaceName);

        // 生成 LogicCore 类
        GenerateLogicCore(sb, uiObjectName, components);

        // 生成 MonoBehaviour Wrapper 类
        GenerateLogicWrapper(sb, uiObjectName, components);

        // 生成命名空间结束
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    #region View 代码生成

    private void GenerateViewHeader(StringBuilder sb, string namespaceName)
    {
        var usings = config.GetRequiredUsings();
        usings.Add("UnityEngine");
        usings.Add("UI");
        usings.Add("System");

        foreach (var usingNamespace in usings.OrderBy(u => u))
        {
            sb.AppendLine($"using {usingNamespace};");
        }
        sb.AppendLine();

        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
        }
    }

    private void GenerateViewExculuseProperty(StringBuilder sb)
    {
        sb.AppendLine("        public override bool Exclusive => false;");
        sb.AppendLine("        public override bool CanBack => true;");
    }

    private void GenerateViewClassStart(StringBuilder sb, string uiObjectName)
    {
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// {uiObjectName} View 层 - UI 组件绑定");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public partial class {uiObjectName} : UIViewBase");
        sb.AppendLine("    {");
    }

    private void GenerateComponentFields(StringBuilder sb, Dictionary<string, UIComponentInfo> components)
    {
        sb.AppendLine("        // UI Components");
        foreach (var kvp in components)
        {
            sb.AppendLine($"        [SerializeField] private {kvp.Value.name} {kvp.Key};");
        }
    }

    private void GenerateInitializationMethods(StringBuilder sb, string uiObjectName, Dictionary<string, UIComponentInfo> components)
    {
        sb.AppendLine();
        sb.AppendLine("        public override void OnCreate()");
        sb.AppendLine("        {");
        sb.AppendLine("            // 组件已在编辑器中手动绑定，无需运行时自动绑定");
        sb.AppendLine("        }");
    }

    private void GenerateComponentAccessMethods(StringBuilder sb, Dictionary<string, UIComponentInfo> components)
    {
        // 生成文本设置方法
        var textComponents = components.Where(c => 
            c.Value.name == "Text" || c.Value.name == "TMP_Text").ToList();
        
        if (textComponents.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("        /// <summary>更新文本内容</summary>");
            foreach (var text in textComponents)
            {
                sb.AppendLine($"        public void Set{CapitalizeFirst(text.Key)}(string content)");
                sb.AppendLine("        {");
                sb.AppendLine($"            if ({text.Key} != null) {text.Key}.text = content;");
                sb.AppendLine("        }");
            }
        }

        // 生成按钮事件绑定方法
        var buttonComponents = components.Where(c => c.Value.name == "Button").ToList();
        
        if (buttonComponents.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("        /// <summary>绑定 Button 事件</summary>");
            foreach (var button in buttonComponents)
            {
                sb.AppendLine($"        public void Bind{CapitalizeFirst(button.Key)}Button(System.Action onClickAction)");
                sb.AppendLine("        {");
                sb.AppendLine($"            if ({button.Key} != null) {{ {button.Key}.onClick.RemoveAllListeners(); if (onClickAction != null) {{ {button.Key}.onClick.AddListener(() => onClickAction()); }} }}");
                sb.AppendLine("        }");
            }
        }

        // 生成其他组件的设置方法
        var setterComponents = components.Where(c => 
        {
            var componentConfig = config.GetComponentType(c.Value.name);
            return componentConfig?.generateSetterMethod == true;
        }).ToList();

        foreach (var component in setterComponents)
        {
            GenerateComponentSetterMethod(sb, component);
        }
    }

    private void GenerateComponentSetterMethod(StringBuilder sb, KeyValuePair<string, UIComponentInfo> component)
    {
        string componentName = component.Key;
        string componentType = component.Value.name;

        sb.AppendLine();
        switch (componentType)
        {
            case "InputField":
            case "TMP_InputField":
                sb.AppendLine($"        /// <summary>设置 {componentName} 的值</summary>");
                sb.AppendLine($"        public void Set{CapitalizeFirst(componentName)}Text(string text)");
                sb.AppendLine("        {");
                sb.AppendLine($"            if ({componentName} != null) {componentName}.text = text;");
                sb.AppendLine("        }");
                break;
                
            case "Toggle":
                sb.AppendLine($"        /// <summary>设置 {componentName} 的值</summary>");
                sb.AppendLine($"        public void Set{CapitalizeFirst(componentName)}IsOn(bool isOn)");
                sb.AppendLine("        {");
                sb.AppendLine($"            if ({componentName} != null) {componentName}.isOn = isOn;");
                sb.AppendLine("        }");
                break;
                
            case "Slider":
                sb.AppendLine($"        /// <summary>设置 {componentName} 的值</summary>");
                sb.AppendLine($"        public void Set{CapitalizeFirst(componentName)}Value(float value)");
                sb.AppendLine("        {");
                sb.AppendLine($"            if ({componentName} != null) {componentName}.value = value;");
                sb.AppendLine("        }");
                break;
                
            case "Dropdown":
                sb.AppendLine($"        /// <summary>设置 {componentName} 的值</summary>");
                sb.AppendLine($"        public void Set{CapitalizeFirst(componentName)}Value(int value)");
                sb.AppendLine("        {");
                sb.AppendLine($"            if ({componentName} != null) {componentName}.value = value;");
                sb.AppendLine("        }");
                break;
        }
    }

    private void GenerateCloseMethod(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("        public void Close()");
        sb.AppendLine("        {");
        sb.AppendLine("            gameObject.SetActive(false);");
        sb.AppendLine("        }");
    }

    private void GenerateViewClassEnd(StringBuilder sb, string namespaceName)
    {
        sb.AppendLine("    }");
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine("}");
        }
    }

    #endregion

    #region Logic 代码生成

    private void GenerateLogicHeader(StringBuilder sb, string namespaceName)
    {
        sb.AppendLine("using System;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UI;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
        }
    }

    private void GenerateLogicCore(StringBuilder sb, string uiObjectName, Dictionary<string, UIComponentInfo> components)
    {
        var buttonComponents = components.Where(c => c.Value.name == "Button").ToList();
        var textComponents = components.Where(c => 
            c.Value.name == "Text" || c.Value.name == "TMP_Text").ToList();

        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// {uiObjectName} 纯逻辑核心（可单元测试）");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public class {uiObjectName}LogicCore");
        sb.AppendLine("    {");
        sb.AppendLine($"        protected {uiObjectName} _view;");
        sb.AppendLine("        public virtual void Bind(UIViewBase view)");
        sb.AppendLine("        {");
        sb.AppendLine($"            _view = view as {uiObjectName};");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public virtual void OnOpen(UIArgs args)");
        sb.AppendLine("        {");
        sb.AppendLine("            // 在此实现打开时的业务逻辑（纯 C#，易于单元测试）");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public virtual void OnClose()");
        sb.AppendLine("        {");
        sb.AppendLine("            // 关闭时清理");
        sb.AppendLine("            _view = null;");
        sb.AppendLine("        }");

        // 覆盖 / 恢复 回调
        sb.AppendLine();
        sb.AppendLine("        public virtual void OnCovered()");
        sb.AppendLine("        {");
        sb.AppendLine("            // 被同层新 UI 覆盖时的默认处理（子类可重写）");
        sb.AppendLine("        }");

        sb.AppendLine();
        sb.AppendLine("        public virtual void OnResume()");
        sb.AppendLine("        {");
        sb.AppendLine("            // 从覆盖状态恢复时的默认处理（子类可重写）");
        sb.AppendLine("        }");

        // 生成按钮点击处理方法
        foreach (var button in buttonComponents)
        {
            sb.AppendLine();
            sb.AppendLine($"        public void On{CapitalizeFirst(button.Key)}Clicked()");
            sb.AppendLine("        {");
            sb.AppendLine("            // TODO: 处理按钮点击后的业务逻辑（纯逻辑）");
            if (textComponents.Count > 0)
            {
                sb.AppendLine("            // 可在此调用 _view.SetXXX 方法更新文本内容");
            }
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
    }

    private void GenerateLogicWrapper(StringBuilder sb, string uiObjectName, Dictionary<string, UIComponentInfo> components)
    {
        var buttonComponents = components.Where(c => c.Value.name == "Button").ToList();

        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// MonoBehaviour Wrapper：创建并持有 LogicCore，在运行时作为 IUILogic 注入到 View");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public class {uiObjectName}Logic : MonoBehaviour, IUILogic");
        sb.AppendLine("    {");
        sb.AppendLine($"        private {uiObjectName}LogicCore _core = new {uiObjectName}LogicCore();");
        sb.AppendLine($"        private {uiObjectName} _view;");
        sb.AppendLine();
        sb.AppendLine("        public void Bind(UIViewBase view)");
        sb.AppendLine("        {");
        sb.AppendLine($"            _view = view as {uiObjectName};");
        sb.AppendLine("            if (_view == null)");
        sb.AppendLine("            {");
        sb.AppendLine($"                Debug.LogError($\"[UI] {uiObjectName}Logic: Bind failed! View is not {{typeof({uiObjectName})}}\");");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            _core.Bind(view);");
        
        // 绑定按钮事件到 wrapper 回调
        foreach (var button in buttonComponents)
        {
            string btnName = CapitalizeFirst(button.Key);
            sb.AppendLine($"            // Auto-bind event for {button.Key}");
            sb.AppendLine($"            _view.Bind{btnName}Button(On{btnName}Clicked);");
        }
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public void OnOpen(UIArgs args)");
        sb.AppendLine("        {");
        sb.AppendLine("            _core.OnOpen(args);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public void OnClose()");
        sb.AppendLine("        {");
        sb.AppendLine("            _core.OnClose();");
        // 解绑按钮事件
        foreach (var button in buttonComponents)
        {
            string btnName = CapitalizeFirst(button.Key);
            sb.AppendLine($"            if (_view != null) _view.Bind{btnName}Button(null);");
        }
        sb.AppendLine("            _view = null;");
        sb.AppendLine("        }");
        
        // 实现接口方法
        sb.AppendLine();
        sb.AppendLine("        public void OnCovered()");
        sb.AppendLine("        {");
        sb.AppendLine("            _core.OnCovered();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public void OnResume()");
        sb.AppendLine("        {");
        sb.AppendLine("            _core.OnResume();");
        sb.AppendLine("        }");

        // 生成按钮点击事件处理方法
        foreach (var button in buttonComponents)
        {
            string btnName = CapitalizeFirst(button.Key);
            sb.AppendLine();
            sb.AppendLine($"        private void On{btnName}Clicked()");
            sb.AppendLine("        {");
            sb.AppendLine($"            _core.On{btnName}Clicked();");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
    }

    #endregion

    /// <summary>
    /// 首字母大写
    /// </summary>
    private string CapitalizeFirst(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToUpper(str[0]) + str.Substring(1);
    }
}