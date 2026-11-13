using System;
using System.Collections.Generic;
using System.Globalization;
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
        public string Description => "修改 Transform / RectTransform 属性";

        /// <inheritdoc />
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"instanceID\":{\"type\":\"integer\",\"description\":\"优先于 path\"},\"path\":{\"type\":\"string\",\"description\":\"如 /Root/Child\"},\"localPosition\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"description\":\"[x,y,z]\"},\"localRotation\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"description\":\"欧拉角 [x,y,z]\"},\"localScale\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"description\":\"[x,y,z]\"},\"rect\":{\"type\":\"object\",\"description\":\"仅 RectTransform 节点\",\"properties\":{\"anchoredPosition\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"description\":\"[x,y]\"},\"sizeDelta\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"description\":\"[w,h]\"},\"pivot\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"description\":\"[x,y]\"},\"anchorMin\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"description\":\"[x,y]\"},\"anchorMax\":{\"type\":\"array\",\"items\":{\"type\":\"number\"},\"description\":\"[x,y]\"}}}}}";

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

                // 3. Extract rect sub-object for RectTransform params
                Dictionary<string, object> rtParams = null;
                if (parameters != null && parameters.TryGetValue("rect", out var rawRect) && rawRect is Dictionary<string, object> rectDict)
                    rtParams = rectDict;

                // 4. Handle RectTransform-only params from rect sub-object
                if (rtParams != null && rtParams.Count > 0)
                {
                    var rt = transform as RectTransform;
                    if (rt == null)
                        return Task.FromResult(ToolResult.Error(
                            "目标 GO 不含 RectTransform 组件，无法设置 rect 属性"));

                    if (rtParams.TryGetValue("anchoredPosition", out var rawAP) && rawAP != null)
                    {
                        rt.anchoredPosition = VectorParseHelper.ParseVector2(rawAP);
                        hasAnyProp = true;
                    }
                    if (rtParams.TryGetValue("sizeDelta", out var rawSD) && rawSD != null)
                    {
                        rt.sizeDelta = VectorParseHelper.ParseVector2(rawSD);
                        hasAnyProp = true;
                    }
                    if (rtParams.TryGetValue("pivot", out var rawPivot) && rawPivot != null)
                    {
                        rt.pivot = VectorParseHelper.ParseVector2(rawPivot);
                        hasAnyProp = true;
                    }
                    if (rtParams.TryGetValue("anchorMin", out var rawAMin) && rawAMin != null)
                    {
                        rt.anchorMin = VectorParseHelper.ParseVector2(rawAMin);
                        hasAnyProp = true;
                    }
                    if (rtParams.TryGetValue("anchorMax", out var rawAMax) && rawAMax != null)
                    {
                        rt.anchorMax = VectorParseHelper.ParseVector2(rawAMax);
                        hasAnyProp = true;
                    }
                }

                // 5. Must have at least one property
                if (!hasAnyProp)
                    return Task.FromResult(ToolResult.Error("至少需要提供一个属性参数"));

                // 6. Build JSON response：只回显本次传入的属性（读回实际值）
                var sb = new StringBuilder();
                sb.Append("{\"path\":");
                sb.Append(MiniJson.SerializeString(GameObjectPathHelper.GetGameObjectPath(go)));
                if (HasParam(parameters, "localPosition")) AppendVector(sb, "localPosition", transform.localPosition);
                if (HasParam(parameters, "localRotation")) AppendVector(sb, "localRotation", transform.localEulerAngles);
                if (HasParam(parameters, "localScale")) AppendVector(sb, "localScale", transform.localScale);
                if (transform is RectTransform rtResult)
                {
                    if (HasParam(rtParams, "anchoredPosition")) AppendVector(sb, "anchoredPosition", rtResult.anchoredPosition);
                    if (HasParam(rtParams, "sizeDelta")) AppendVector(sb, "sizeDelta", rtResult.sizeDelta);
                    if (HasParam(rtParams, "pivot")) AppendVector(sb, "pivot", rtResult.pivot);
                    if (HasParam(rtParams, "anchorMin")) AppendVector(sb, "anchorMin", rtResult.anchorMin);
                    if (HasParam(rtParams, "anchorMax")) AppendVector(sb, "anchorMax", rtResult.anchorMax);
                }
                sb.Append('}');

                return Task.FromResult(ToolResult.Success(sb.ToString()));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Error(ex.Message));
            }
        }

        private static bool HasParam(Dictionary<string, object> dict, string key)
        {
            return dict != null && dict.TryGetValue(key, out var raw) && raw != null;
        }

        private static string Num(float f) => f.ToString("G", CultureInfo.InvariantCulture);

        private static void AppendVector(StringBuilder sb, string key, Vector3 v)
        {
            sb.Append(",\"").Append(key).Append("\":[")
              .Append(Num(v.x)).Append(',').Append(Num(v.y)).Append(',').Append(Num(v.z)).Append(']');
        }

        private static void AppendVector(StringBuilder sb, string key, Vector2 v)
        {
            sb.Append(",\"").Append(key).Append("\":[")
              .Append(Num(v.x)).Append(',').Append(Num(v.y)).Append(']');
        }
    }
}
