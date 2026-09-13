using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：创建受限类型的 .asset 资产。
    /// </summary>
    public class AssetCreateTool : IMcpTool
    {
        private const string ScriptableObjectKind = "scriptableObject";
        private const string MaterialKind = "material";
        private const string PhysicMaterialKind = "physicMaterial";
        private const string PhysicsMaterial2DKind = "physicsMaterial2D";

        /// <inheritdoc />
        public string Name => "asset_create";

        /// <inheritdoc />
        public string Category => "editor";

        /// <inheritdoc />
        public string Description => "创建受限类型的 .asset 资产";

        /// <inheritdoc />
        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{\"kind\":{\"type\":\"string\",\"enum\":[\"scriptableObject\",\"material\",\"physicMaterial\",\"physicsMaterial2D\"],\"description\":\"资产类型\"},\"path\":{\"type\":\"string\",\"pattern\":\"^Assets/\",\"description\":\"目标 .asset 路径（必须位于 Assets 目录）\"},\"typeName\":{\"type\":\"string\",\"description\":\"非抽象 ScriptableObject 派生类的完整类型名，仅 scriptableObject 必填\"},\"shaderName\":{\"type\":\"string\",\"description\":\"Shader.Find 使用的 Shader 名称，仅 material 必填\"},\"conflictStrategy\":{\"type\":\"string\",\"enum\":[\"error\",\"unique\"],\"default\":\"error\",\"description\":\"目标已存在时的处理方式\"}},\"required\":[\"kind\",\"path\"]}";

        /// <inheritdoc />
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            string kind = GetString(parameters, "kind");
            if (string.IsNullOrEmpty(kind))
                return Task.FromResult(ToolResult.Error("kind parameter is required"));

            if (!IsSupportedKind(kind))
                return Task.FromResult(ToolResult.Error($"unsupported asset kind: {kind}"));

            string path = GetString(parameters, "path");
            if (string.IsNullOrEmpty(path))
                return Task.FromResult(ToolResult.Error("path parameter is required"));

            string assetPath = NormalizeAssetPath(path);
            if (assetPath == null)
                return Task.FromResult(ToolResult.Error($"path must be an .asset file under the Assets folder: {path}"));

            string parentPath = GetParentAssetPath(assetPath);
            if (!AssetDatabase.IsValidFolder(parentPath))
                return Task.FromResult(ToolResult.Error($"parent directory not found: {parentPath}"));

            string conflictStrategy = GetString(parameters, "conflictStrategy") ?? "error";
            if (conflictStrategy != "error" && conflictStrategy != "unique")
                return Task.FromResult(ToolResult.Error($"unsupported conflictStrategy: {conflictStrategy}"));

            if (AssetPathExists(assetPath))
            {
                if (conflictStrategy == "error")
                    return Task.FromResult(ToolResult.Error($"asset already exists: {assetPath}"));

                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                if (AssetPathExists(assetPath))
                    return Task.FromResult(ToolResult.Error($"failed to generate a unique asset path: {assetPath}"));
            }

            UnityEngine.Object asset = null;
            bool assetCreated = false;
            try
            {
                string createError;
                asset = CreateAssetObject(kind, GetString(parameters, "typeName"), GetString(parameters, "shaderName"), out createError);
                if (asset == null)
                    return Task.FromResult(ToolResult.Error(createError));

                asset.name = Path.GetFileNameWithoutExtension(assetPath);
                AssetDatabase.CreateAsset(asset, assetPath);
                assetCreated = true;
                AssetDatabase.SaveAssets();

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                    throw new InvalidOperationException("AssetDatabase did not return a GUID");

                return Task.FromResult(ToolResult.Success(BuildResponse(assetPath, guid, kind, asset.GetType().FullName)));
            }
            catch (Exception ex)
            {
                CleanupFailedAsset(asset, assetPath, assetCreated);
                return Task.FromResult(ToolResult.Error($"failed to create asset: {ex.Message}"));
            }
        }

        internal static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            string normalizedInput = path.Replace('\\', '/');
            if (!normalizedInput.StartsWith("Assets/", StringComparison.Ordinal))
                return null;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, normalizedInput));
            string assetsPath = Path.GetFullPath(Application.dataPath);
            if (!fullPath.StartsWith(assetsPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                return null;

            string assetPath = "Assets" + fullPath.Substring(assetsPath.Length).Replace('\\', '/');
            if (!assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(assetPath)))
                return null;

            return assetPath;
        }

        private static string GetString(Dictionary<string, object> parameters, string name)
        {
            if (parameters != null && parameters.TryGetValue(name, out var raw) && raw is string value)
                return value;

            return null;
        }

        private static bool IsSupportedKind(string kind)
        {
            return kind == ScriptableObjectKind ||
                   kind == MaterialKind ||
                   kind == PhysicMaterialKind ||
                   kind == PhysicsMaterial2DKind;
        }

        private static string GetParentAssetPath(string assetPath)
        {
            int separatorIndex = assetPath.LastIndexOf('/');
            return separatorIndex > 0 ? assetPath.Substring(0, separatorIndex) : "Assets";
        }

        private static bool AssetPathExists(string assetPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
                return true;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            return File.Exists(fullPath) || File.Exists(fullPath + ".meta") || Directory.Exists(fullPath);
        }

        private static UnityEngine.Object CreateAssetObject(string kind, string typeName, string shaderName, out string error)
        {
            error = null;
            switch (kind)
            {
                case ScriptableObjectKind:
                    if (string.IsNullOrEmpty(typeName))
                    {
                        error = "typeName parameter is required for scriptableObject";
                        return null;
                    }

                    Type type = ResolveType(typeName);
                    if (type == null)
                    {
                        error = $"ScriptableObject type not found: {typeName}";
                        return null;
                    }

                    if (!typeof(ScriptableObject).IsAssignableFrom(type))
                    {
                        error = $"type must derive from ScriptableObject: {typeName}";
                        return null;
                    }

                    if (type.IsAbstract)
                    {
                        error = $"ScriptableObject type must not be abstract: {typeName}";
                        return null;
                    }

                    return ScriptableObject.CreateInstance(type);

                case MaterialKind:
                    if (string.IsNullOrEmpty(shaderName))
                    {
                        error = "shaderName parameter is required for material";
                        return null;
                    }

                    Shader shader = Shader.Find(shaderName);
                    if (shader == null)
                    {
                        error = $"shader not found: {shaderName}";
                        return null;
                    }

                    return new Material(shader);

                case PhysicMaterialKind:
                    return new PhysicMaterial();

                case PhysicsMaterial2DKind:
                    return new PhysicsMaterial2D();

                default:
                    error = $"unsupported asset kind: {kind}";
                    return null;
            }
        }

        private static Type ResolveType(string typeName)
        {
            Type type = Type.GetType(typeName, false);
            if (type != null)
                return type;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName, false);
                if (type != null)
                    return type;
            }

            return null;
        }

        private static string BuildResponse(string assetPath, string guid, string kind, string typeName)
        {
            var sb = new StringBuilder();
            sb.Append("{\"assetPath\":");
            sb.Append(MiniJson.SerializeString(assetPath));
            sb.Append(",\"guid\":");
            sb.Append(MiniJson.SerializeString(guid));
            sb.Append(",\"kind\":");
            sb.Append(MiniJson.SerializeString(kind));
            sb.Append(",\"typeName\":");
            sb.Append(MiniJson.SerializeString(typeName));
            sb.Append('}');
            return sb.ToString();
        }

        private static void CleanupFailedAsset(UnityEngine.Object asset, string assetPath, bool assetCreated)
        {
            try
            {
                if (assetCreated)
                    AssetDatabase.DeleteAsset(assetPath);
                else if (asset != null)
                    UnityEngine.Object.DestroyImmediate(asset);
            }
            catch
            {
                // 保留原始创建异常。
            }
        }
    }
}

