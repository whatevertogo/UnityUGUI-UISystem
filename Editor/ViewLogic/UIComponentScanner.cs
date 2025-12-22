using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;

/// <summary>
/// UI 组件扫描器 - 负责扫描和分析 UI 对象的组件
/// </summary>
public class UIComponentScanner
{
    private UIComponentConfig config;

    public UIComponentScanner(UIComponentConfig config)
    {
        this.config = config;
    }

    /// <summary>
    /// 扫描 UI 对象的所有子物体，找出 UI 组件
    /// </summary>
    public Dictionary<string, UIComponentInfo> ScanUIComponents(GameObject uiObject)
    {
        var components = new Dictionary<string, UIComponentInfo>();
        var componentCache = new Dictionary<Transform, List<string>>();

        // 一次性获取所有子物体的组件，避免重复遍历
        var allChildren = uiObject.GetComponentsInChildren<Transform>(true);
        
        foreach (var child in allChildren)
        {
            if (child == uiObject.transform) continue; // 跳过自身

            // 获取该物体上的所有 UI 组件类型
            var componentTypes = GetUIComponentTypes(child, componentCache);
            if (componentTypes.Count == 0) continue;

            string path = GetRelativePath(uiObject.transform, child);

            foreach (var componentType in componentTypes)
            {
                var componentInfo = new UIComponentInfo
                {
                    name = componentType,
                    path = path,
                    transform = child,
                    gameObject = child.gameObject
                };

                // 生成安全的变量名
                string varName = GenerateVariableName(child.gameObject.name, componentType, components);
                components[varName] = componentInfo;
            }
        }

        return components;
    }

    /// <summary>
    /// 获取物体上所有 UI 组件类型（使用缓存优化）
    /// </summary>
    private List<string> GetUIComponentTypes(Transform transform, Dictionary<Transform, List<string>> cache)
    {
        if (cache.TryGetValue(transform, out var cachedTypes))
        {
            return cachedTypes;
        }

        var types = new List<string>();
        var components = transform.GetComponents<Component>();

        foreach (var component in components)
        {
            string typeName = GetComponentTypeName(component);
            if (!string.IsNullOrEmpty(typeName))
            {
                types.Add(typeName);
            }
        }

        cache[transform] = types;
        return types;
    }

    /// <summary>
    /// 根据组件实例获取类型名称
    /// </summary>
    private string GetComponentTypeName(Component component)
    {
        var componentType = component.GetType();
        var configType = config.supportedComponents.FirstOrDefault(c => c.fullName == componentType.FullName);
        
        return configType?.name;
    }

    /// <summary>
    /// 生成安全的变量名
    /// </summary>
    private string GenerateVariableName(string gameObjectName, string componentType, Dictionary<string, UIComponentInfo> existingComponents)
    {
        // 清理 GameObject 名称，移除特殊字符
        string baseName = MakeSafeVariableName(gameObjectName);

        // 如果名称以组件类型后缀结尾（例如 "saveButton" + Button），去掉后缀以得到更语义化的变量名
        var typeLower = componentType.ToLower();
        if (baseName.EndsWith(typeLower, StringComparison.OrdinalIgnoreCase))
        {
            baseName = baseName.Substring(0, baseName.Length - typeLower.Length);
            if (string.IsNullOrEmpty(baseName)) baseName = "element";
        }

        // 如果已有相同变量名，添加类型后缀以区分
        string varName = baseName;
        if (existingComponents.ContainsKey(varName))
        {
            varName = baseName + componentType;
        }

        // 确保变量名唯一
        int suffix = 1;
        string finalName = varName;
        while (existingComponents.ContainsKey(finalName))
        {
            finalName = $"{varName}{suffix++}";
        }

        return finalName;
    }

    /// <summary>
    /// 生成安全的变量名（移除特殊字符，首字母小写）
    /// </summary>
    private string MakeSafeVariableName(string name)
    {
        // 移除特殊字符，只保留字母和数字
        var safeName = Regex.Replace(name, "[^a-zA-Z0-9_]", "");
        
        // 如果为空，使用默认名称
        if (string.IsNullOrEmpty(safeName)) 
            safeName = "element";
        
        // 确保首字母小写
        return char.ToLower(safeName[0]) + safeName.Substring(1);
    }

    /// <summary>
    /// 获取相对路径
    /// </summary>
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
}

/// <summary>
/// UI 组件信息
/// </summary>
public class UIComponentInfo
{
    public string name;           // 组件类型名称
    public string path;           // 相对路径
    public Transform transform;   // Transform 引用
    public GameObject gameObject; // GameObject 引用
}