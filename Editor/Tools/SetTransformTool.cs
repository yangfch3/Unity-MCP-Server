using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：修改 GameObject 的 Transform / RectTransform 属性。
    /// </summary>
    public class SetTransformTool : IMcpTool
    {
        /// <inheritdoc />
        public string Name => "editor_setTransform";

        /// <inheritdoc />
        public string Category => "editor";

        /// <inheritdoc />
        public string Description => "修改 GameObject 的 Transform / RectTransform 属性";

        /// <inheritdoc />
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"instanceID\":{\"type\":\"integer\",\"description\":\"目标 GameObject 的 instanceID\"},\"path\":{\"type\":\"string\",\"description\":\"目标 GameObject 的路径（如 \\\"/Root/Child\\\"）\"},\"localPosition\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"minItems\":3,\"maxItems\":3,\"description\":\"本地位置 [x, y, z]\"},\"localRotation\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"minItems\":3,\"maxItems\":3,\"description\":\"本地旋转欧拉角 [x, y, z]\"},\"localScale\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"minItems\":3,\"maxItems\":3,\"description\":\"本地缩放 [x, y, z]\"},\"anchoredPosition\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"minItems\":2,\"maxItems\":2,\"description\":\"锚点位置 [x, y]（仅 RectTransform）\"},\"sizeDelta\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"minItems\":2,\"maxItems\":2,\"description\":\"尺寸偏移 [w, h]（仅 RectTransform）\"},\"pivot\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"minItems\":2,\"maxItems\":2,\"description\":\"轴心 [x, y]（仅 RectTransform）\"},\"anchorMin\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"minItems\":2,\"maxItems\":2,\"description\":\"最小锚点 [x, y]（仅 RectTransform）\"},\"anchorMax\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"minItems\":2,\"maxItems\":2,\"description\":\"最大锚点 [x, y]（仅 RectTransform）\"}}}";

        private static readonly string[] RtParamNames =
            { "anchoredPosition", "sizeDelta", "pivot", "anchorMin", "anchorMax" };

        /// <inheritdoc />
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            try
            {
                // 1. Resolve target GO
                var (go, err) = GameObjectResolveHelper.Resolve(parameters);
                if (go == null)
                    return Task.FromResult(ToolResult.Error(err));

                var transform = go.transform;
                Undo.RecordObject(transform, "Set Transform");

                bool hasAnyProp = false;

                // 2. Transform properties (Vector3)
                if (parameters != null && parameters.TryGetValue("localPosition", out var rawPos) && rawPos != null)
                {
                    transform.localPosition = VectorParseHelper.ParseVector3(rawPos);
                    hasAnyProp = true;
                }
                if (parameters != null && parameters.TryGetValue("localRotation", out var rawRot) && rawRot != null)
                {
                    transform.localEulerAngles = VectorParseHelper.ParseVector3(rawRot);
                    hasAnyProp = true;
                }
                if (parameters != null && parameters.TryGetValue("localScale", out var rawScale) && rawScale != null)
                {
                    transform.localScale = VectorParseHelper.ParseVector3(rawScale);
                    hasAnyProp = true;
                }

                // 3. Check if any RectTransform-only params are provided
                bool hasRtParam = false;
                if (parameters != null)
                {
                    foreach (var name in RtParamNames)
                    {
                        if (parameters.TryGetValue(name, out var v) && v != null)
                        {
                            hasRtParam = true;
                            break;
                        }
                    }
                }

                // 4. Handle RectTransform-only params
                if (hasRtParam)
                {
                    var rt = transform as RectTransform;
                    if (rt == null)
                        return Task.FromResult(ToolResult.Error(
                            "目标 GO 不含 RectTransform 组件，无法设置 anchoredPosition/sizeDelta 等属性"));

                    if (parameters.TryGetValue("anchoredPosition", out var rawAP) && rawAP != null)
                    {
                        rt.anchoredPosition = VectorParseHelper.ParseVector2(rawAP);
                        hasAnyProp = true;
                    }
                    if (parameters.TryGetValue("sizeDelta", out var rawSD) && rawSD != null)
                    {
                        rt.sizeDelta = VectorParseHelper.ParseVector2(rawSD);
                        hasAnyProp = true;
                    }
                    if (parameters.TryGetValue("pivot", out var rawPivot) && rawPivot != null)
                    {
                        rt.pivot = VectorParseHelper.ParseVector2(rawPivot);
                        hasAnyProp = true;
                    }
                    if (parameters.TryGetValue("anchorMin", out var rawAMin) && rawAMin != null)
                    {
                        rt.anchorMin = VectorParseHelper.ParseVector2(rawAMin);
                        hasAnyProp = true;
                    }
                    if (parameters.TryGetValue("anchorMax", out var rawAMax) && rawAMax != null)
                    {
                        rt.anchorMax = VectorParseHelper.ParseVector2(rawAMax);
                        hasAnyProp = true;
                    }
                }

                // 5. Must have at least one property
                if (!hasAnyProp)
                    return Task.FromResult(ToolResult.Error("至少需要提供一个属性参数"));

                // 6. Build JSON response
                var goPath = GameObjectPathHelper.GetGameObjectPath(go);
                var sb = new StringBuilder();
                sb.Append("{\"name\":");
                sb.Append(MiniJson.SerializeString(go.name));
                sb.Append(",\"path\":");
                sb.Append(MiniJson.SerializeString(goPath));
                sb.Append(",\"localPosition\":\"");
                AppendVector3(sb, transform.localPosition);
                sb.Append("\",\"localEulerAngles\":\"");
                AppendVector3(sb, transform.localEulerAngles);
                sb.Append("\",\"localScale\":\"");
                AppendVector3(sb, transform.localScale);
                sb.Append('"');

                if (transform is RectTransform rtResult)
                {
                    sb.Append(",\"anchoredPosition\":\"");
                    AppendVector2(sb, rtResult.anchoredPosition);
                    sb.Append("\",\"sizeDelta\":\"");
                    AppendVector2(sb, rtResult.sizeDelta);
                    sb.Append("\",\"pivot\":\"");
                    AppendVector2(sb, rtResult.pivot);
                    sb.Append("\",\"anchorMin\":\"");
                    AppendVector2(sb, rtResult.anchorMin);
                    sb.Append("\",\"anchorMax\":\"");
                    AppendVector2(sb, rtResult.anchorMax);
                    sb.Append('"');
                }

                sb.Append('}');

                return Task.FromResult(ToolResult.Success(sb.ToString()));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Error(ex.Message));
            }
        }

        private static void AppendVector3(StringBuilder sb, Vector3 v)
        {
            sb.Append('[');
            sb.Append(v.x.ToString("G"));
            sb.Append(", ");
            sb.Append(v.y.ToString("G"));
            sb.Append(", ");
            sb.Append(v.z.ToString("G"));
            sb.Append(']');
        }

        private static void AppendVector2(StringBuilder sb, Vector2 v)
        {
            sb.Append('[');
            sb.Append(v.x.ToString("G"));
            sb.Append(", ");
            sb.Append(v.y.ToString("G"));
            sb.Append(']');
        }
    }
}
