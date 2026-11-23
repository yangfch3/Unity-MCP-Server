using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor
{
    [Serializable]
    internal class StartupTimingSettings
    {
        public bool enabled;
        public string keyword = "";
    }

    [Serializable]
    internal class StartupTimingHistory
    {
        public string keyword = "";
        public List<double> seconds = new List<double>();

        internal bool Add(double elapsed)
        {
            if (double.IsNaN(elapsed) || elapsed < 2 || elapsed > 30)
                return false;
            seconds.Add(elapsed);
            if (seconds.Count > 50)
                seconds.RemoveRange(0, seconds.Count - 50);
            return true;
        }

        internal int? Recommendation
        {
            get
            {
                if (seconds.Count < 10) return null;
                var sorted = new List<double>(seconds);
                sorted.Sort();
                return (int)Math.Ceiling(sorted[(int)Math.Ceiling(sorted.Count * 0.9) - 1] + 1);
            }
        }
    }

    [InitializeOnLoad]
    internal static class StartupTiming
    {
        private const string PendingKey = "UnityMcp.StartupTiming.Pending";
        private const string EnteredKey = "UnityMcp.StartupTiming.Entered";
        private const string KeywordKey = "UnityMcp.StartupTiming.Keyword";
        private static readonly string ProjectRoot = Path.GetDirectoryName(Application.dataPath);
        private static readonly string SettingsPath = Path.Combine(ProjectRoot, "ProjectSettings/UnityMcpSettings.json");
        private static readonly string HistoryPath = Path.Combine(ProjectRoot, "Library/UnityMcp/StartupTimes.json");
        private static readonly string WaitPrefKey = "UnityMcp.StartupWaitSeconds." + Application.dataPath;
        private static long _settingsStamp = -1;
        private static StartupTimingSettings _settings = new StartupTimingSettings();
        private static StartupTimingHistory _history;

        static StartupTiming()
        {
            RefreshSettings();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged += state => { if (state == PauseState.Paused) Cancel(); };
            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                if (SessionState.GetBool(EnteredKey, false)) Cancel();
            };
            Application.logMessageReceived += OnLogMessage;
        }

        internal static StartupTimingSettings Settings => _settings;
        internal static float WaitSeconds => ParseWait(EditorPrefs.GetString(WaitPrefKey, ""));
        internal static string WaitText
        {
            get => EditorPrefs.GetString(WaitPrefKey, "");
            set => EditorPrefs.SetString(WaitPrefKey, value);
        }
        internal static int SampleCount { get { EnsureHistory(); return _history.seconds.Count; } }
        internal static int? Recommendation { get { EnsureHistory(); return _history.Recommendation; } }

        internal static float ParseWait(string text)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                && seconds > 0 && !float.IsInfinity(seconds) ? seconds : 5f;
        }

        internal static string EnterResponse(float seconds)
        {
            var number = seconds.ToString(CultureInfo.InvariantCulture);
            return "{\"message\":\"进游中，sleep " + number
                + " 秒后查询状态；Playing 仅表示 PlayMode，项目可用性需另行确认\",\"suggestedWaitSeconds\":" + number + "}";
        }

        internal static void RefreshSettings()
        {
            long stamp = File.Exists(SettingsPath) ? File.GetLastWriteTimeUtc(SettingsPath).Ticks : 0;
            if (stamp == _settingsStamp) return;
            _settingsStamp = stamp;
            _settings = Read<StartupTimingSettings>(SettingsPath) ?? new StartupTimingSettings();
            _settings.keyword = _settings.keyword ?? "";
            CancelIfSettingsChanged();
            EnsureHistory();
        }

        internal static void SaveSettings(bool enabled, string keyword)
        {
            _settings = new StartupTimingSettings { enabled = enabled, keyword = keyword };
            Write(SettingsPath, _settings);
            _settingsStamp = File.GetLastWriteTimeUtc(SettingsPath).Ticks;
            CancelIfSettingsChanged();
            EnsureHistory();
        }

        private static void CancelIfSettingsChanged()
        {
            if (!_settings.enabled || _settings.keyword != SessionState.GetString(KeywordKey, "")) Cancel();
        }

        internal static void ClearHistory()
        {
            Cancel();
            _history = new StartupTimingHistory { keyword = _settings.keyword };
            Write(HistoryPath, _history);
        }

        private static void EnsureHistory()
        {
            if (_history == null)
                _history = Read<StartupTimingHistory>(HistoryPath) ?? new StartupTimingHistory();
            if (_history.seconds == null) _history.seconds = new List<double>();
            if (_history.keyword != _settings.keyword)
                ClearHistory();
        }

        internal static void Begin()
        {
            RefreshSettings();
            if (!_settings.enabled || string.IsNullOrWhiteSpace(_settings.keyword)) return;
            if (!string.IsNullOrEmpty(SessionState.GetString(PendingKey, ""))) return;
            SessionState.SetBool(EnteredKey, false);
            SessionState.SetString(KeywordKey, _settings.keyword);
            SessionState.SetString(PendingKey, Stopwatch.GetTimestamp().ToString(CultureInfo.InvariantCulture));
        }

        private static void Cancel()
        {
            SessionState.EraseString(PendingKey);
            SessionState.SetBool(EnteredKey, false);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode: Begin(); break;
                case PlayModeStateChange.EnteredPlayMode: SessionState.SetBool(EnteredKey, true); break;
                case PlayModeStateChange.ExitingPlayMode:
                case PlayModeStateChange.EnteredEditMode: Cancel(); break;
            }
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(SessionState.GetString(PendingKey, ""))) return;
            if (message.IndexOf(_settings.keyword, StringComparison.Ordinal) < 0) return;
            RefreshSettings();
            if (!long.TryParse(SessionState.GetString(PendingKey, ""), out var start)) return;
            Cancel(); // 首次命中即结束本轮，过短/过长也不再尝试后续日志。
            var elapsed = (Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency;
            EnsureHistory();
            if (_history.Add(elapsed)) Write(HistoryPath, _history);
        }

        private static T Read<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            try { return JsonUtility.FromJson<T>(File.ReadAllText(path)); }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException)
            {
                UnityEngine.Debug.LogWarning("[McpServer] Cannot read " + path + ": " + ex.Message);
                return null;
            }
        }

        private static void Write(string path, object data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(data, true) + "\n");
        }
    }
}
