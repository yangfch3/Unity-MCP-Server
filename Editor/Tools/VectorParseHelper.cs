using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// 共享辅助类：提供向量、颜色、矩形的解析方法。
    /// 将 MiniJson 反序列化得到的 <see cref="List{T}"/>（元素为 long/double/int/float）
    /// 转换为 Unity 的 Vector2/3/4、Color、Rect 类型。
    /// </summary>
    internal static class VectorParseHelper
    {
        /// <summary>
        /// 将 MiniJson 解析结果转换为 <see cref="Vector2"/>。
        /// </summary>
        /// <param name="raw">应为 <see cref="List{Object}"/>，取前 2 个元素。</param>
        /// <returns>解析后的 Vector2。</returns>
        /// <exception cref="ArgumentException">数组长度不足或元素类型无法转换。</exception>
        internal static Vector2 ParseVector2(object raw)
        {
            var list = CastList(raw, 2);
            return new Vector2(ToFloat(list[0]), ToFloat(list[1]));
        }

        /// <summary>
        /// 将 MiniJson 解析结果转换为 <see cref="Vector3"/>。
        /// </summary>
        /// <param name="raw">应为 <see cref="List{Object}"/>，取前 3 个元素。</param>
        /// <returns>解析后的 Vector3。</returns>
        /// <exception cref="ArgumentException">数组长度不足或元素类型无法转换。</exception>
        internal static Vector3 ParseVector3(object raw)
        {
            var list = CastList(raw, 3);
            return new Vector3(ToFloat(list[0]), ToFloat(list[1]), ToFloat(list[2]));
        }

        /// <summary>
        /// 将 MiniJson 解析结果转换为 <see cref="Vector4"/>。
        /// </summary>
        /// <param name="raw">应为 <see cref="List{Object}"/>，取前 4 个元素。</param>
        /// <returns>解析后的 Vector4。</returns>
        /// <exception cref="ArgumentException">数组长度不足或元素类型无法转换。</exception>
        internal static Vector4 ParseVector4(object raw)
        {
            var list = CastList(raw, 4);
            return new Vector4(ToFloat(list[0]), ToFloat(list[1]), ToFloat(list[2]), ToFloat(list[3]));
        }

        /// <summary>
        /// 将 MiniJson 解析结果转换为 <see cref="Color"/>。
        /// </summary>
        /// <param name="raw">应为 <see cref="List{Object}"/>，格式 [r, g, b, a]，各分量 0~1。</param>
        /// <returns>解析后的 Color。</returns>
        /// <exception cref="ArgumentException">数组长度不足或元素类型无法转换。</exception>
        internal static Color ParseColor(object raw)
        {
            var list = CastList(raw, 4);
            return new Color(ToFloat(list[0]), ToFloat(list[1]), ToFloat(list[2]), ToFloat(list[3]));
        }

        /// <summary>
        /// 将 MiniJson 解析结果转换为 <see cref="Rect"/>。
        /// </summary>
        /// <param name="raw">应为 <see cref="List{Object}"/>，格式 [x, y, width, height]。</param>
        /// <returns>解析后的 Rect。</returns>
        /// <exception cref="ArgumentException">数组长度不足或元素类型无法转换。</exception>
        internal static Rect ParseRect(object raw)
        {
            var list = CastList(raw, 4);
            return new Rect(ToFloat(list[0]), ToFloat(list[1]), ToFloat(list[2]), ToFloat(list[3]));
        }

        /// <summary>
        /// 将 object（long/double/int/float）转换为 float。
        /// </summary>
        /// <param name="raw">数值对象。</param>
        /// <returns>转换后的 float 值。</returns>
        /// <exception cref="ArgumentException">类型无法转换为 float。</exception>
        internal static float ToFloat(object raw)
        {
            if (raw is double d) return (float)d;
            if (raw is long l) return (float)l;
            if (raw is int i) return (float)i;
            if (raw is float f) return f;
            throw new ArgumentException($"无法将 {raw?.GetType().Name ?? "null"} 转换为 float");
        }

        /// <summary>
        /// 将 raw 转换为 <see cref="List{Object}"/> 并校验最小长度。
        /// </summary>
        private static List<object> CastList(object raw, int minLength)
        {
            if (!(raw is List<object> list))
                throw new ArgumentException($"期望 List<object>，实际为 {raw?.GetType().Name ?? "null"}");
            if (list.Count < minLength)
                throw new ArgumentException($"数组长度不足：期望至少 {minLength} 个元素，实际 {list.Count} 个");
            return list;
        }
    }
}
