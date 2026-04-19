using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：修改 GameObject 上指定组件的序列化字段值。
    /// </summary>
    public class SetFieldTool : IMcpTool
    {
        /// <inheritdoc />
        public string Name => "editor_setField";

        /// <inheritdoc />
        public string Category => "editor";

        /// <inheritdoc />
        public string Description => "修改 GameObject 上指定组件的序列化字段值";

        /// <inheritdoc />
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"instanceID\":{\"type\":\"integer\",\"description\":\"目标 GameObject 的 instanceID\"},\"path\":{\"type\":\"string\",\"description\":\"目标 GameObject 的路径（如 \\\"/Root/Child\\\"）\"},\"componentType\":{\"type\":\"string\",\"description\":\"组件类型名（如 \\\"BoxCollider\\\"）\"},\"fieldName\":{\"type\":\"string\",\"description\":\"序列化字段名\"},\"value\":{\"description\":\"新值（类型需与字段匹配）\"}},\"required\":[\"componentType\",\"fieldName\",\"value\"]}";

        /// <inheritdoc />
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            try
            {
                // 1. Resolve target GameObject
                var (go, err) = GameObjectResolveHelper.Resolve(parameters);
                if (go == null)
                    return Task.FromResult(ToolResult.Error(err));

                // 2. Extract componentType
                string typeName = null;
                if (parameters != null && parameters.TryGetValue("componentType", out var rawType))
                    typeName = rawType as string;
                if (string.IsNullOrEmpty(typeName))
                    return Task.FromResult(ToolResult.Error("componentType 为必填参数"));

                // 3. Find component on GO
                var comp = ComponentTypeHelper.FindComponent(go, typeName);
                if (comp == null)
                    return Task.FromResult(ToolResult.Error($"在 {go.name} 上未找到 {typeName} 组件"));

                // 4. Extract fieldName
                string fieldName = null;
                if (parameters != null && parameters.TryGetValue("fieldName", out var rawField))
                    fieldName = rawField as string;
                if (string.IsNullOrEmpty(fieldName))
                    return Task.FromResult(ToolResult.Error("fieldName 为必填参数"));

                // 5. Extract value
                if (parameters == null || !parameters.TryGetValue("value", out var value) || value == null)
                    return Task.FromResult(ToolResult.Error("value 为必填参数"));

                // 6. Create SerializedObject and find property
                var so = new SerializedObject(comp);
                var prop = so.FindProperty(fieldName);
                if (prop == null)
                    return Task.FromResult(ToolResult.Error(
                        $"在 {comp.GetType().Name} 上未找到序列化字段: {fieldName}"));

                // 7. Set property value
                var setErr = SetPropertyValue(prop, value, fieldName);
                if (setErr != null)
                    return Task.FromResult(ToolResult.Error(setErr));

                // 8. Apply changes (auto Undo support)
                so.ApplyModifiedProperties();

                // 9. Read back and return result
                var sb = new StringBuilder();
                sb.Append("{\"fieldName\":");
                sb.Append(MiniJson.SerializeString(fieldName));
                sb.Append(",\"fieldType\":");
                sb.Append(MiniJson.SerializeString(prop.propertyType.ToString()));
                sb.Append(",\"newValue\":");
                sb.Append(MiniJson.SerializeString(GetPropertyValue(prop)));
                sb.Append('}');

                return Task.FromResult(ToolResult.Success(sb.ToString()));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Error(ex.Message));
            }
        }

        /// <summary>
        /// 按 SerializedPropertyType 分发设置属性值。
        /// </summary>
        /// <returns>错误信息，null 表示成功。</returns>
        private static string SetPropertyValue(SerializedProperty prop, object value, string fieldName)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (value is long l)
                        prop.intValue = (int)l;
                    else if (value is double d)
                        prop.intValue = (int)d;
                    else
                        return $"字段 {fieldName} 类型为 Integer，无法接受提供的值";
                    break;

                case SerializedPropertyType.Boolean:
                    if (value is bool b)
                        prop.boolValue = b;
                    else
                        return $"字段 {fieldName} 类型为 Boolean，无法接受提供的值";
                    break;

                case SerializedPropertyType.Float:
                    try
                    {
                        prop.floatValue = VectorParseHelper.ToFloat(value);
                    }
                    catch (ArgumentException)
                    {
                        return $"字段 {fieldName} 类型为 Float，无法接受提供的值";
                    }
                    break;

                case SerializedPropertyType.String:
                    if (value is string s)
                        prop.stringValue = s;
                    else
                        return $"字段 {fieldName} 类型为 String，无法接受提供的值";
                    break;

                case SerializedPropertyType.Enum:
                    if (value is string enumName)
                    {
                        int idx = Array.IndexOf(prop.enumDisplayNames, enumName);
                        if (idx < 0)
                            return $"枚举值未找到: {enumName}";
                        prop.enumValueIndex = idx;
                    }
                    else if (value is long el)
                    {
                        prop.enumValueIndex = (int)el;
                    }
                    else if (value is double ed)
                    {
                        prop.enumValueIndex = (int)ed;
                    }
                    else
                    {
                        return $"字段 {fieldName} 类型为 Enum，无法接受提供的值";
                    }
                    break;

                case SerializedPropertyType.Vector2:
                    try { prop.vector2Value = VectorParseHelper.ParseVector2(value); }
                    catch (ArgumentException) { return $"字段 {fieldName} 类型为 Vector2，无法接受提供的值"; }
                    break;

                case SerializedPropertyType.Vector3:
                    try { prop.vector3Value = VectorParseHelper.ParseVector3(value); }
                    catch (ArgumentException) { return $"字段 {fieldName} 类型为 Vector3，无法接受提供的值"; }
                    break;

                case SerializedPropertyType.Vector4:
                    try { prop.vector4Value = VectorParseHelper.ParseVector4(value); }
                    catch (ArgumentException) { return $"字段 {fieldName} 类型为 Vector4，无法接受提供的值"; }
                    break;

                case SerializedPropertyType.Color:
                    try { prop.colorValue = VectorParseHelper.ParseColor(value); }
                    catch (ArgumentException) { return $"字段 {fieldName} 类型为 Color，无法接受提供的值"; }
                    break;

                case SerializedPropertyType.Rect:
                    try { prop.rectValue = VectorParseHelper.ParseRect(value); }
                    catch (ArgumentException) { return $"字段 {fieldName} 类型为 Rect，无法接受提供的值"; }
                    break;

                case SerializedPropertyType.ObjectReference:
                    if (value is long refId)
                    {
                        prop.objectReferenceValue = EditorUtility.InstanceIDToObject((int)refId);
                    }
                    else if (value is double refD)
                    {
                        prop.objectReferenceValue = EditorUtility.InstanceIDToObject((int)refD);
                    }
                    else
                    {
                        return $"字段 {fieldName} 类型为 ObjectReference，无法接受提供的值";
                    }
                    break;

                default:
                    return $"不支持的字段类型: {prop.propertyType}";
            }

            return null;
        }

        /// <summary>
        /// 读取 SerializedProperty 的当前值为字符串表示。
        /// 参考 InspectorTool.GetPropertyValue 实现。
        /// </summary>
        private static string GetPropertyValue(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return prop.intValue.ToString();
                case SerializedPropertyType.Boolean:
                    return prop.boolValue.ToString();
                case SerializedPropertyType.Float:
                    return prop.floatValue.ToString("G");
                case SerializedPropertyType.String:
                    return prop.stringValue ?? "";
                case SerializedPropertyType.Enum:
                    return prop.enumDisplayNames != null
                        && prop.enumValueIndex >= 0
                        && prop.enumValueIndex < prop.enumDisplayNames.Length
                        ? prop.enumDisplayNames[prop.enumValueIndex]
                        : prop.enumValueIndex.ToString();
                case SerializedPropertyType.Vector2:
                    return prop.vector2Value.ToString();
                case SerializedPropertyType.Vector3:
                    return prop.vector3Value.ToString();
                case SerializedPropertyType.Vector4:
                    return prop.vector4Value.ToString();
                case SerializedPropertyType.Color:
                    return prop.colorValue.ToString();
                case SerializedPropertyType.Rect:
                    return prop.rectValue.ToString();
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue != null
                        ? prop.objectReferenceValue.name
                        : "null";
                default:
                    return prop.propertyType.ToString();
            }
        }
    }
}
