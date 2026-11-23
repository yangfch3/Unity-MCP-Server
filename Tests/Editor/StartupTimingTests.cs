using NUnit.Framework;

namespace UnityMcp.Editor.Tests
{
    /// <summary>启动样本过滤、推荐值及等待提示测试。</summary>
    public class StartupTimingTests
    {
        [TestCase(1.99, false)]
        [TestCase(2, true)]
        [TestCase(30, true)]
        [TestCase(30.01, false)]
        [TestCase(double.NaN, false)]
        [TestCase(double.PositiveInfinity, false)]
        public void History_FiltersSamples(double seconds, bool accepted)
        {
            var history = new StartupTimingHistory();
            Assert.AreEqual(accepted, history.Add(seconds));
            Assert.AreEqual(accepted ? 1 : 0, history.seconds.Count);
        }

        [Test]
        public void History_RetainsLast50ValidSamples()
        {
            var history = new StartupTimingHistory();
            history.Add(2);
            for (int i = 0; i < 50; i++) history.Add(10);
            history.Add(31);
            Assert.AreEqual(50, history.seconds.Count);
            Assert.AreEqual(10, history.seconds[0]);
        }

        [Test]
        public void Recommendation_Requires10Samples_UsesNearestRankP90PlusOneRoundedUp()
        {
            var history = new StartupTimingHistory();
            for (int i = 0; i < 9; i++) history.Add(10.2);
            Assert.IsNull(history.Recommendation);
            history.Add(30);
            Assert.AreEqual(12, history.Recommendation);
            history.Add(30);
            Assert.AreEqual(31, history.Recommendation);
        }

        [TestCase("", 5f)]
        [TestCase("invalid", 5f)]
        [TestCase("0", 5f)]
        [TestCase("-1", 5f)]
        [TestCase("NaN", 5f)]
        [TestCase("Infinity", 5f)]
        [TestCase("7.5", 7.5f)]
        public void Wait_UsesPositiveNumberOrDefault(string text, float expected)
        {
            Assert.AreEqual(expected, StartupTiming.ParseWait(text));
        }

        [Test]
        public void EnterResponse_ContainsExplicitMachineReadableWait()
        {
            var response = StartupTiming.EnterResponse(7.5f);
            StringAssert.Contains("\"suggestedWaitSeconds\":7.5", response);
            StringAssert.Contains("sleep 7.5 秒后查询状态", response);
        }

        [Test]
        public void History_JsonRoundTrip_PreservesKeywordAndSamples()
        {
            var history = new StartupTimingHistory { keyword = "GameState: Enter" };
            history.Add(7.25);
            var restored = UnityEngine.JsonUtility.FromJson<StartupTimingHistory>(
                UnityEngine.JsonUtility.ToJson(history));
            Assert.AreEqual(history.keyword, restored.keyword);
            CollectionAssert.AreEqual(history.seconds, restored.seconds);
        }

        [Test]
        public void SharedSettings_JsonContainsOnlyToggleAndKeyword()
        {
            var json = UnityEngine.JsonUtility.ToJson(
                new StartupTimingSettings { enabled = true, keyword = "GameState: Enter" });
            var fields = (System.Collections.Generic.Dictionary<string, object>)MiniJson.Deserialize(json);
            CollectionAssert.AreEquivalent(new[] { "enabled", "keyword" }, fields.Keys);
            var restored = UnityEngine.JsonUtility.FromJson<StartupTimingSettings>(json);
            Assert.IsTrue(restored.enabled);
            Assert.AreEqual("GameState: Enter", restored.keyword);
        }
    }
}
