using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// AssetDeleteFolderTool 属性测试（标记 Slow，可通过 --where "cat != Slow" 跳过）。
    /// </summary>
    public class AssetDeleteFolderToolPropertyTests
    {
        // Feature: mcp-console-asset-tools, Property 1: Path safety validation
        // Validates: Requirements 1.3
        [Test]
        [Category("Slow")]
        public void Property1_PathSafetyValidation()
        {
            var rng = new System.Random(789);
            string assetsDir = Application.dataPath; // e.g. /project/Assets

            // --- Deterministic known-bad paths ---
            string[] knownBadPaths = new string[]
            {
                "Assets/../../etc/passwd",
                "Assets/../../../tmp",
                "Assets/../../outside",
                "AssetsExtra/foo",
                "AssetsFake",
                "/tmp/evil",
                "/etc/passwd",
                ".",
                "..",
                "../",
                "../../",
                "",
                " ",
                "Assets/../..",
                "Assets/Sub/../../..",
                "Assets/Sub/../../../etc",
            };

            foreach (string badPath in knownBadPaths)
            {
                string result = AssetDeleteFolderTool.ValidatePath(badPath);

                // Empty/whitespace paths: ValidatePath may resolve them to project root,
                // which is not under Assets — should return null.
                // All these paths resolve outside Assets, so result must be null.
                if (string.IsNullOrWhiteSpace(badPath))
                {
                    // Empty string combined with project root resolves to project root itself
                    // which is not under Assets — expect null
                    Assert.IsNull(result,
                        $"Known-bad path '{badPath}' should be rejected but got: {result}");
                    continue;
                }

                // Verify the path truly resolves outside Assets before asserting
                string projectRoot = Path.GetDirectoryName(assetsDir);
                string fullPath = Path.GetFullPath(Path.Combine(projectRoot, badPath));
                bool isUnderAssets = fullPath == assetsDir ||
                    fullPath.StartsWith(assetsDir + Path.DirectorySeparatorChar);

                if (!isUnderAssets)
                {
                    Assert.IsNull(result,
                        $"Known-bad path '{badPath}' resolves to '{fullPath}' which is outside Assets, " +
                        $"but ValidatePath returned: {result}");
                }
            }

            // --- Random iterations ---
            for (int iter = 0; iter < 100; iter++)
            {
                string path = GenerateUnsafePath(rng, iter);

                // Compute expected: does this path resolve under Assets?
                string projectRoot = Path.GetDirectoryName(assetsDir);
                string fullPath;
                try
                {
                    fullPath = Path.GetFullPath(Path.Combine(projectRoot, path));
                }
                catch
                {
                    // If Path.GetFullPath throws (invalid chars etc.), ValidatePath
                    // should also reject — but it may throw too. Skip this iteration.
                    continue;
                }

                bool isUnderAssets = fullPath == assetsDir ||
                    fullPath.StartsWith(assetsDir + Path.DirectorySeparatorChar);

                if (!isUnderAssets)
                {
                    string result = AssetDeleteFolderTool.ValidatePath(path);
                    Assert.IsNull(result,
                        $"Iteration {iter}: path '{path}' resolves to '{fullPath}' " +
                        $"which is outside Assets, but ValidatePath returned: {result}");
                }
            }
        }

        /// <summary>
        /// Generate a random path that should resolve outside the Assets directory.
        /// Mixes several strategies: traversal with "..", absolute paths, look-alike prefixes,
        /// and special characters.
        /// </summary>
        private static string GenerateUnsafePath(System.Random rng, int iter)
        {
            int strategy = iter % 5;

            switch (strategy)
            {
                case 0: // Traversal: Assets followed by enough ".." to escape
                {
                    int depth = rng.Next(2, 6); // 2-5 levels of ".."
                    string traversal = "Assets";
                    for (int i = 0; i < depth; i++)
                        traversal += "/..";
                    traversal += "/" + RandomSegment(rng);
                    return traversal;
                }

                case 1: // Absolute path
                {
                    string[] roots = { "/tmp", "/etc", "/var", "/usr" };
                    return roots[rng.Next(roots.Length)] + "/" + RandomSegment(rng);
                }

                case 2: // Look-alike prefix (not exactly "Assets")
                {
                    string[] fakes = { "AssetsExtra", "Assets2", "Assetsss", "Asset", "ASSETS" };
                    return fakes[rng.Next(fakes.Length)] + "/" + RandomSegment(rng);
                }

                case 3: // Dot paths that resolve to project root or above
                {
                    string[] dots = { ".", "..", "../..", "../../.." };
                    return dots[rng.Next(dots.Length)];
                }

                case 4: // Random segments with ".." inserted
                {
                    int segments = rng.Next(2, 5);
                    string path = RandomSegment(rng);
                    for (int i = 0; i < segments; i++)
                    {
                        path += rng.Next(2) == 0 ? "/.." : ("/" + RandomSegment(rng));
                    }
                    return path;
                }

                default:
                    return "../" + RandomSegment(rng);
            }
        }

        private static string RandomSegment(System.Random rng)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789_";
            int len = rng.Next(1, 8);
            var sb = new System.Text.StringBuilder(len);
            for (int i = 0; i < len; i++)
                sb.Append(chars[rng.Next(chars.Length)]);
            return sb.ToString();
        }
    }
}
