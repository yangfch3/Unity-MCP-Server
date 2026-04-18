using System;
using System.Collections.Generic;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// ConsoleTool 测试共享辅助方法：JSON 解析。
    /// </summary>
    internal static class ConsoleToolTestHelper
    {
        /// <summary>
        /// 从 Execute 返回的 JSON 数组字符串中解析出每条日志的 level 和 message。
        /// 简易解析，不依赖 internal MiniJson。
        /// </summary>
        internal static List<(string level, string message)> ParseEntries(string json)
        {
            var entries = new List<(string level, string message)>();
            int pos = 0;
            while (pos < json.Length)
            {
                int objStart = json.IndexOf('{', pos);
                if (objStart < 0) break;
                int objEnd = json.IndexOf('}', objStart);
                if (objEnd < 0) break;

                string obj = json.Substring(objStart, objEnd - objStart + 1);
                string level = ExtractStringField(obj, "level");
                string message = ExtractStringField(obj, "message");
                entries.Add((level, message));
                pos = objEnd + 1;
            }
            return entries;
        }

        /// <summary>从 JSON 对象字符串中提取指定字符串字段的值。</summary>
        internal static string ExtractStringField(string obj, string fieldName)
        {
            string key = $"\"{fieldName}\":\"";
            int start = obj.IndexOf(key, StringComparison.Ordinal);
            if (start < 0) return null;
            start += key.Length;
            var sb = new System.Text.StringBuilder();
            for (int i = start; i < obj.Length; i++)
            {
                if (obj[i] == '\\' && i + 1 < obj.Length)
                {
                    sb.Append(obj[i + 1]);
                    i++;
                }
                else if (obj[i] == '"')
                {
                    break;
                }
                else
                {
                    sb.Append(obj[i]);
                }
            }
            return sb.ToString();
        }

        /// <summary>从 JSON 对象字符串中提取指定整数字段的值。</summary>
        internal static long ExtractLongField(string obj, string fieldName)
        {
            string key = $"\"{fieldName}\":";
            int start = obj.IndexOf(key, StringComparison.Ordinal);
            if (start < 0) return -1;
            start += key.Length;
            var sb = new System.Text.StringBuilder();
            for (int i = start; i < obj.Length; i++)
            {
                if (char.IsDigit(obj[i]) || obj[i] == '-')
                    sb.Append(obj[i]);
                else
                    break;
            }
            return long.TryParse(sb.ToString(), out long val) ? val : -1;
        }
    }
}
