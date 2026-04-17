using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UnityMcp.Editor
{
    /// <summary>
    /// 最小 JSON 解析/序列化工具。仅支持 JSON-RPC 协议层所需的子集：
    /// object → Dictionary&lt;string, object&gt;, array → List&lt;object&gt;,
    /// string, number (long/double), bool, null。
    /// </summary>
    internal static class MiniJson
    {
        /// <summary>将 JSON 字符串反序列化为 object 树。</summary>
        public static object Deserialize(string json)
        {
            if (json == null) return null;
            var parser = new Parser(json);
            return parser.ParseValue();
        }

        /// <summary>转义并包裹 JSON 字符串（含双引号）。</summary>
        public static string SerializeString(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b");  break;
                    case '\f': sb.Append("\\f");  break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': sb.Append("\\r");  break;
                    case '\t': sb.Append("\\t");  break;
                    default:
                        if (c < 0x20)
                            sb.AppendFormat("\\u{0:x4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private class Parser
        {
            private readonly string _json;
            private int _pos;

            public Parser(string json)
            {
                _json = json;
                _pos = 0;
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (_pos >= _json.Length)
                    throw new FormatException("Unexpected end of JSON");

                switch (_json[_pos])
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': case 'f': return ParseBool();
                    case 'n': return ParseNull();
                    default:  return ParseNumber();
                }
            }

            private Dictionary<string, object> ParseObject()
            {
                Expect('{');
                var dict = new Dictionary<string, object>();
                SkipWhitespace();
                if (_pos < _json.Length && _json[_pos] == '}')
                {
                    _pos++;
                    return dict;
                }

                while (true)
                {
                    SkipWhitespace();
                    var key = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    var value = ParseValue();
                    dict[key] = value;
                    SkipWhitespace();
                    if (_pos >= _json.Length) break;
                    if (_json[_pos] == ',') { _pos++; continue; }
                    if (_json[_pos] == '}') { _pos++; break; }
                    throw new FormatException($"Expected ',' or '}}' at position {_pos}");
                }
                return dict;
            }

            private List<object> ParseArray()
            {
                Expect('[');
                var list = new List<object>();
                SkipWhitespace();
                if (_pos < _json.Length && _json[_pos] == ']')
                {
                    _pos++;
                    return list;
                }

                while (true)
                {
                    list.Add(ParseValue());
                    SkipWhitespace();
                    if (_pos >= _json.Length) break;
                    if (_json[_pos] == ',') { _pos++; continue; }
                    if (_json[_pos] == ']') { _pos++; break; }
                    throw new FormatException($"Expected ',' or ']' at position {_pos}");
                }
                return list;
            }

            private string ParseString()
            {
                Expect('"');
                var sb = new StringBuilder();
                while (_pos < _json.Length)
                {
                    var c = _json[_pos++];
                    if (c == '"') return sb.ToString();
                    if (c == '\\')
                    {
                        if (_pos >= _json.Length)
                            throw new FormatException("Unexpected end of string escape");
                        var esc = _json[_pos++];
                        switch (esc)
                        {
                            case '"':  sb.Append('"');  break;
                            case '\\': sb.Append('\\'); break;
                            case '/':  sb.Append('/');  break;
                            case 'b':  sb.Append('\b'); break;
                            case 'f':  sb.Append('\f'); break;
                            case 'n':  sb.Append('\n'); break;
                            case 'r':  sb.Append('\r'); break;
                            case 't':  sb.Append('\t'); break;
                            case 'u':
                                if (_pos + 4 > _json.Length)
                                    throw new FormatException("Invalid unicode escape");
                                var hex = _json.Substring(_pos, 4);
                                sb.Append((char)int.Parse(hex, NumberStyles.HexNumber));
                                _pos += 4;
                                break;
                            default:
                                throw new FormatException($"Invalid escape character: \\{esc}");
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                throw new FormatException("Unterminated string");
            }

            private object ParseNumber()
            {
                int start = _pos;
                if (_pos < _json.Length && _json[_pos] == '-') _pos++;
                while (_pos < _json.Length && char.IsDigit(_json[_pos])) _pos++;

                bool isFloat = false;
                if (_pos < _json.Length && _json[_pos] == '.')
                {
                    isFloat = true;
                    _pos++;
                    while (_pos < _json.Length && char.IsDigit(_json[_pos])) _pos++;
                }
                if (_pos < _json.Length && (_json[_pos] == 'e' || _json[_pos] == 'E'))
                {
                    isFloat = true;
                    _pos++;
                    if (_pos < _json.Length && (_json[_pos] == '+' || _json[_pos] == '-')) _pos++;
                    while (_pos < _json.Length && char.IsDigit(_json[_pos])) _pos++;
                }

                var numStr = _json.Substring(start, _pos - start);
                if (numStr.Length == 0)
                    throw new FormatException($"Invalid number at position {start}");

                if (!isFloat && long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                    return l;
                if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    return d;
                throw new FormatException($"Invalid number: {numStr}");
            }

            private bool ParseBool()
            {
                if (Match("true"))  return true;
                if (Match("false")) return false;
                throw new FormatException($"Invalid value at position {_pos}");
            }

            private object ParseNull()
            {
                if (Match("null")) return null;
                throw new FormatException($"Invalid value at position {_pos}");
            }

            private bool Match(string expected)
            {
                if (_pos + expected.Length > _json.Length) return false;
                for (int i = 0; i < expected.Length; i++)
                {
                    if (_json[_pos + i] != expected[i]) return false;
                }
                _pos += expected.Length;
                return true;
            }

            private void Expect(char c)
            {
                SkipWhitespace();
                if (_pos >= _json.Length || _json[_pos] != c)
                    throw new FormatException($"Expected '{c}' at position {_pos}");
                _pos++;
            }

            private void SkipWhitespace()
            {
                while (_pos < _json.Length && char.IsWhiteSpace(_json[_pos]))
                    _pos++;
            }
        }
    }
}
