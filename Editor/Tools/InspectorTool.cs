using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：获取选中对象的 Inspector 属性（序列化字段值）。
    /// 多选时仅返回第一个选中对象。
    /// </summary>
    public class InspectorTool : IMcpTool
    {
        public string Name => "editor_getInspector";
        public string Category => "editor";
        public string Description => "获取选中对象的 Inspector 序列化字段值";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{}}";

        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            var go = Selection.activeGameObject;
            if (go == null)
                return Task.FromResult(ToolResult.Success("未选中任何 GameObject"));

            var sb = new StringBuilder();
            sb.Append("{\"gameObject\":");
            sb.Append(MiniJson.SerializeString(go.name));
            sb.Append(",\"components\":[");

            var components = go.GetComponents<Component>();
            bool firstComp = true;
            foreach (var comp in components)
            {
                if (comp == null) continue;

                if (!firstComp) sb.Append(',');
                firstComp = false;

                sb.Append("{\"type\":");
                sb.Append(MiniJson.SerializeString(comp.GetType().Name));
                sb.Append(",\"fields\":[");

                try
                {
                    var so = new SerializedObject(comp);
                    var prop = so.GetIterator();
                    bool firstField = true;

                    if (prop.NextVisible(true))
                    {
                        do
                        {
                            if (!firstField) sb.Append(',');
                            firstField = false;

                            sb.Append("{\"name\":");
                            sb.Append(MiniJson.SerializeString(prop.name));
                            sb.Append(",\"type\":");
                            sb.Append(MiniJson.SerializeString(prop.propertyType.ToString()));
                            sb.Append(",\"value\":");
                            sb.Append(MiniJson.SerializeString(GetPropertyValue(prop)));
                            sb.Append('}');
                        }
                        while (prop.NextVisible(false));
                    }
                }
                catch
                {
                    // 跳过异常组件
                }

                sb.Append("]}");
            }

            sb.Append("]}");
            return Task.FromResult(ToolResult.Success(sb.ToString()));
        }

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
                    return prop.enumDisplayNames != null && prop.enumValueIndex >= 0 && prop.enumValueIndex < prop.enumDisplayNames.Length
                        ? prop.enumDisplayNames[prop.enumValueIndex]
                        : prop.enumValueIndex.ToString();
                case SerializedPropertyType.Vector2:
                    return prop.vector2Value.ToString();
                case SerializedPropertyType.Vector3:
                    return prop.vector3Value.ToString();
                case SerializedPropertyType.Vector4:
                    return prop.vector4Value.ToString();
                case SerializedPropertyType.Quaternion:
                    return prop.quaternionValue.ToString();
                case SerializedPropertyType.Color:
                    return prop.colorValue.ToString();
                case SerializedPropertyType.Rect:
                    return prop.rectValue.ToString();
                case SerializedPropertyType.Bounds:
                    return prop.boundsValue.ToString();
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue != null ? prop.objectReferenceValue.name : "null";
                default:
                    return prop.propertyType.ToString();
            }
        }
    }
}
