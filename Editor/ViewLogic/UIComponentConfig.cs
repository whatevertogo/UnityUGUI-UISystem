using System.Collections.Generic;

/// <summary>
/// UI 组件生成器配置
/// </summary>
[System.Serializable]
public class UIComponentConfig
{
    /// <summary>
    /// 支持的 UI 组件类型定义
    /// </summary>
    [System.Serializable]
    public class ComponentType
    {
        public string name;
        public string fullName;
        public string usingNamespace;
        public bool generateSetterMethod;
        public bool generateEventMethod;
        public string eventMethodName;
    }

    /// <summary>
    /// 代码模板配置
    /// </summary>
    [System.Serializable]
    public class CodeTemplate
    {
        public string viewTemplate;
        public string logicTemplate;
        public string logicCoreTemplate;
    }

    public List<ComponentType> supportedComponents = new List<ComponentType>
    {
        new ComponentType 
        { 
            name = "Button", 
            fullName = "UnityEngine.UI.Button", 
            usingNamespace = "UnityEngine.UI",
            generateSetterMethod = false,
            generateEventMethod = true,
            eventMethodName = "onClick"
        },
        new ComponentType 
        { 
            name = "Text", 
            fullName = "UnityEngine.UI.Text", 
            usingNamespace = "UnityEngine.UI",
            generateSetterMethod = true,
            generateEventMethod = false
        },
        new ComponentType 
        { 
            name = "Image", 
            fullName = "UnityEngine.UI.Image", 
            usingNamespace = "UnityEngine.UI",
            generateSetterMethod = false,
            generateEventMethod = false
        },
        new ComponentType 
        { 
            name = "InputField", 
            fullName = "UnityEngine.UI.InputField", 
            usingNamespace = "UnityEngine.UI",
            generateSetterMethod = true,
            generateEventMethod = true,
            eventMethodName = "onValueChanged"
        },
        new ComponentType 
        { 
            name = "Toggle", 
            fullName = "UnityEngine.UI.Toggle", 
            usingNamespace = "UnityEngine.UI",
            generateSetterMethod = true,
            generateEventMethod = true,
            eventMethodName = "onValueChanged"
        },
        new ComponentType 
        { 
            name = "Slider", 
            fullName = "UnityEngine.UI.Slider", 
            usingNamespace = "UnityEngine.UI",
            generateSetterMethod = true,
            generateEventMethod = true,
            eventMethodName = "onValueChanged"
        },
        new ComponentType 
        { 
            name = "ScrollRect", 
            fullName = "UnityEngine.UI.ScrollRect", 
            usingNamespace = "UnityEngine.UI",
            generateSetterMethod = false,
            generateEventMethod = false
        },
        new ComponentType 
        { 
            name = "Dropdown", 
            fullName = "UnityEngine.UI.Dropdown", 
            usingNamespace = "UnityEngine.UI",
            generateSetterMethod = true,
            generateEventMethod = true,
            eventMethodName = "onValueChanged"
        },
        new ComponentType 
        { 
            name = "TMP_Text", 
            fullName = "TMPro.TextMeshProUGUI", 
            usingNamespace = "TMPro",
            generateSetterMethod = true,
            generateEventMethod = false
        },
        new ComponentType 
        { 
            name = "TMP_InputField", 
            fullName = "TMPro.TMP_InputField", 
            usingNamespace = "TMPro",
            generateSetterMethod = true,
            generateEventMethod = true,
            eventMethodName = "onValueChanged"
        }
    };

    /// <summary>
    /// 获取组件类型信息
    /// </summary>
    public ComponentType GetComponentType(string typeName)
    {
        return supportedComponents.Find(c => c.name == typeName);
    }

    /// <summary>
    /// 获取所有需要的 using 语句
    /// </summary>
    public HashSet<string> GetRequiredUsings()
    {
        var usings = new HashSet<string>();
        foreach (var component in supportedComponents)
        {
            if (!string.IsNullOrEmpty(component.usingNamespace))
            {
                usings.Add(component.usingNamespace);
            }
        }
        return usings;
    }
}