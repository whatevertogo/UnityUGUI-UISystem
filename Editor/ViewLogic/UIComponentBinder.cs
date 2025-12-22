using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// UI 组件绑定器 - 简化版本，仅负责将生成的脚本添加到 GameObject
/// 用户需要在编辑器中手动拖拽绑定组件引用
/// </summary>
public class UIComponentBinder
{
    private string customNamespace;

    // 控制选项（Generator 会设置）
    public bool AutoAddComponents { get; set; } = true;
    public bool PersistBindings { get; set; } = false;
    public int AddMode { get; set; } = 0; // 0 both, 1 view only, 2 logic only

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="customNamespace">自定义命名空间</param>
    public UIComponentBinder(string customNamespace)
    {
        this.customNamespace = customNamespace;
    }

    /// <summary>
    /// 兼容方法：根据当前设置自动添加 View/Logic 脚本到目标对象（不做字段绑定）
    /// </summary>
    public void AutoAddScriptsAndBindComponents(GameObject uiObject, Dictionary<string, UIComponentInfo> components, string viewPath, string viewClassName = null)
    {
        if (uiObject == null) return;

        if (!AutoAddComponents)
        {
            Debug.Log("[UIComponentBinder] AutoAddComponents disabled, 跳过添加组件");
            return;
        }

        // 视图脚本
        if (AddMode == 0 || AddMode == 1)
        {
            // 优先使用传入的类名，否则回退到默认（+View）
            // 默认情况：如果是智能生成的，viewClassName 应该已经是对的了
            string viewScriptName = !string.IsNullOrEmpty(viewClassName) ? viewClassName : (uiObject.name + "View");
            
            var viewType = GetTypeFromAssembly(viewScriptName);
            if (viewType != null)
            {
                string prefabAssetPath = GetPrefabAssetPath(uiObject);
                if (!string.IsNullOrEmpty(prefabAssetPath))
                    AddScriptToPrefab(uiObject, prefabAssetPath, viewType);
                else
                    AddScriptToSceneObject(uiObject, viewType);
            }
            else
            {
                Debug.LogWarning($"[UIComponentBinder] 未找到 View 类型 {viewScriptName}, 可能尚未编译");
            }
        }

        // Logic 脚本
        if (AddMode == 0 || AddMode == 2)
        {
            string logicScriptName = uiObject.name + "Logic";
            var logicType = GetTypeFromAssembly(logicScriptName);
            if (logicType != null)
            {
                string prefabAssetPath = GetPrefabAssetPath(uiObject);
                if (!string.IsNullOrEmpty(prefabAssetPath))
                    AddScriptToPrefab(uiObject, prefabAssetPath, logicType);
                else
                    AddScriptToSceneObject(uiObject, logicType);
            }
            else
            {
                Debug.LogWarning($"[UIComponentBinder] 未找到 Logic 类型 {logicScriptName}, 可能尚未编译");
            }
        }

        if (PersistBindings)
        {
            // 尝试写回绑定（若类型已编译），失败时给出警告
            string viewScriptName = !string.IsNullOrEmpty(viewClassName) ? viewClassName : (uiObject.name + "View");
            var viewType = GetTypeFromAssembly(viewScriptName);
            if (viewType == null)
            {
                Debug.LogWarning($"[UIComponentBinder] PersistBindings: 未能找到已编译的 View 类型 {viewScriptName}，请等待脚本编译后重试。");
            }
            else
            {
                string prefabAssetPath = GetPrefabAssetPath(uiObject);
                if (!string.IsNullOrEmpty(prefabAssetPath))
                {
                    PersistBindingsToPrefab(prefabAssetPath, viewType, components);
                }
                else
                {
                    PersistBindingsToSceneObject(uiObject, viewType, components);
                }
            }
        }
    }

    /// <summary>
    /// 允许通过参数直接调用的重载（Generator 可能会使用）
    /// </summary>
    public void AutoAddScriptsAndBindComponents(GameObject uiObject, Dictionary<string, UIComponentInfo> components, string viewPath,
        bool autoAddComponents, bool persistBindings, int addMode, string viewClassName = null)
    {
        this.AutoAddComponents = autoAddComponents;
        this.PersistBindings = persistBindings;
        this.AddMode = addMode;
        AutoAddScriptsAndBindComponents(uiObject, components, viewPath, viewClassName);
    }

    /// <summary>
    /// 简化的脚本添加方法 - 仅添加脚本组件，不进行自动绑定
    /// </summary>
    /// <param name="uiObject">目标 UI 对象</param>
    /// <param name="viewPath">视图脚本路径（未使用，保留兼容性）</param>
    public void AddViewScript(GameObject uiObject, string viewPath)
    {
        if (uiObject == null)
        {
            Debug.LogError("[UIComponentBinder] UI 对象为空，无法添加脚本");
            return;
        }

        string scriptName = uiObject.name + "View";
        var viewScriptType = GetTypeFromAssembly(scriptName);

        if (viewScriptType == null)
        {
            Debug.LogWarning($"[UIComponentBinder] 无法找到脚本类型 {scriptName}，请确保脚本已编译");
            return;
        }

        // 检查是否为 prefab asset
        string prefabAssetPath = GetPrefabAssetPath(uiObject);

        if (!string.IsNullOrEmpty(prefabAssetPath))
        {
            AddScriptToPrefab(uiObject, prefabAssetPath, viewScriptType);
        }
        else
        {
            AddScriptToSceneObject(uiObject, viewScriptType);
        }
    }

    /// <summary>
    /// 向 Prefab 添加脚本
    /// </summary>
    private void AddScriptToPrefab(GameObject uiObject, string prefabAssetPath, System.Type scriptType)
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabAssetPath);
        try
        {
            var existingComponent = prefabRoot.GetComponent(scriptType);
            if (existingComponent == null)
            {
                prefabRoot.AddComponent(scriptType);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabAssetPath);
                Debug.Log($"[UIComponentBinder] 已向 Prefab {prefabAssetPath} 添加脚本 {scriptType.Name}");
            }
            else
            {
                Debug.Log($"[UIComponentBinder] Prefab {prefabAssetPath} 已包含脚本 {scriptType.Name}");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 向场景对象添加脚本
    /// </summary>
    private void AddScriptToSceneObject(GameObject uiObject, System.Type scriptType)
    {
        var existingComponent = uiObject.GetComponent(scriptType);
        if (existingComponent == null)
        {
            uiObject.AddComponent(scriptType);
            EditorUtility.SetDirty(uiObject);
            Debug.Log($"[UIComponentBinder] 已向场景对象 {uiObject.name} 添加脚本 {scriptType.Name}");
        }
        else
        {
            Debug.Log($"[UIComponentBinder] 场景对象 {uiObject.name} 已包含脚本 {scriptType.Name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 从程序集获取类型
    /// </summary>
    private System.Type GetTypeFromAssembly(string typeName)
    {
        // 首先尝试带命名空间的完整类型名
        string fullTypeName = string.IsNullOrEmpty(customNamespace) ? typeName : $"{customNamespace}.{typeName}";
        
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(fullTypeName);
            if (type != null) return type;

            // 兜底：尝试不带命名空间的类型名
            type = assembly.GetType(typeName);
            if (type != null) return type;
        }
        return null;
    }

    /// <summary>
    /// 在 Prefab Asset 上写回序列化字段绑定
    /// </summary>
    private void PersistBindingsToPrefab(string prefabAssetPath, System.Type viewType, Dictionary<string, UIComponentInfo> components)
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabAssetPath);
        bool changed = false;
        try
        {
            var viewComp = prefabRoot.GetComponent(viewType);
            if (viewComp == null)
            {
                viewComp = prefabRoot.AddComponent(viewType);
            }

            var so = new SerializedObject(viewComp);

            foreach (var kvp in components)
            {
                var varName = kvp.Key;
                var info = kvp.Value;
                if (info == null || string.IsNullOrEmpty(info.path)) continue;

                var targetTransform = prefabRoot.transform.Find(info.path);
                if (targetTransform == null) continue;
                var targetGo = targetTransform.gameObject;

                var compType = GetComponentTypeByName(info.name);
                if (compType == null)
                {
                    // 兜底：尝试按短名从目标上匹配
                    var found = targetGo.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == info.name);
                    if (found == null) continue;
                    compType = found.GetType();
                }

                var compInstance = targetGo.GetComponent(compType);
                if (compInstance == null) continue;

                if (TrySetSerializedObjectReference(so, varName, compInstance))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabAssetPath);
                Debug.Log($"[UIComponentBinder] PersistBindings: 已写回绑定到 Prefab {prefabAssetPath}");
            }
            else
            {
                Debug.Log($"[UIComponentBinder] PersistBindings: 未找到需要写回的字段或绑定未发生变化 for {prefabAssetPath}");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// 在场景对象上写回序列化字段绑定（支持 Undo）
    /// </summary>
    private void PersistBindingsToSceneObject(GameObject uiObject, System.Type viewType, Dictionary<string, UIComponentInfo> components)
    {
        var viewComp = uiObject.GetComponent(viewType);
        if (viewComp == null)
        {
            viewComp = uiObject.AddComponent(viewType);
        }

        var so = new SerializedObject(viewComp);
        bool changed = false;

        foreach (var kvp in components)
        {
            var varName = kvp.Key;
            var info = kvp.Value;
            if (info == null || string.IsNullOrEmpty(info.path)) continue;

            var targetTransform = uiObject.transform.Find(info.path);
            if (targetTransform == null) continue;
            var targetGo = targetTransform.gameObject;

            var compType = GetComponentTypeByName(info.name);
            if (compType == null)
            {
                var found = targetGo.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == info.name);
                if (found == null) continue;
                compType = found.GetType();
            }

            var compInstance = targetGo.GetComponent(compType);
            if (compInstance == null) continue;

            if (TrySetSerializedObjectReference(so, varName, compInstance))
            {
                changed = true;
            }
        }

        if (changed)
        {
            so.ApplyModifiedProperties();
            Undo.RecordObject(viewComp, "Bind UI Components");
            EditorUtility.SetDirty(viewComp);
            EditorSceneManager.MarkSceneDirty(uiObject.scene);
            Debug.Log($"[UIComponentBinder] PersistBindings: 已写回绑定到场景对象 {uiObject.name}");
        }
        else
        {
            Debug.Log($"[UIComponentBinder] PersistBindings: 未发生写回变更 for scene object {uiObject.name}");
        }
    }

    /// <summary>
    /// 根据组件短名查找 Type（在所有已加载程序集搜索）
    /// </summary>
    private System.Type GetComponentTypeByName(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t = asm.GetTypes().FirstOrDefault(x => x.Name == name || x.FullName?.EndsWith("." + name) == true);
                if (t != null) return t;
            }
            catch { }
        }
        return null;
    }

    /// <summary>
    /// 尝试通过 SerializedObject 将引用写回到指定字段名
    /// </summary>
    private bool TrySetSerializedObjectReference(SerializedObject so, string fieldName, UnityEngine.Object reference)
    {
        if (so == null || string.IsNullOrEmpty(fieldName) || reference == null) return false;

        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            // 迭代查找可能的命中（不区分大小写）
            var iter = so.GetIterator();
            bool found = false;
            while (iter.NextVisible(true))
            {
                if (string.Equals(iter.name, fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    prop = so.FindProperty(iter.name);
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }

        if (prop.propertyType == SerializedPropertyType.ObjectReference)
        {
            if (prop.objectReferenceValue != reference)
            {
                prop.objectReferenceValue = reference;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取 Prefab 资源路径
    /// </summary>
    private string GetPrefabAssetPath(GameObject gameObject)
    {
        try
        {
            return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
        }
        catch
        {
            return null;
        }
    }
}