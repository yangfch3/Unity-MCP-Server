using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// MainThreadQueue 单元测试。
    /// 使用 [UnityTest] 协程让 EditorApplication.update 有机会触发队列消费。
    /// </summary>
    public class MainThreadQueueTests
    {
        private MainThreadQueue _queue;

        [SetUp]
        public void SetUp()
        {
            _queue = new MainThreadQueue();
            _queue.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _queue.Stop();
        }

        [UnityTest]
        public IEnumerator Enqueue_SimpleAction_ReturnsResult()
        {
            var task = _queue.Enqueue(() => Task.FromResult(ToolResult.Success("hello")));

            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;
            Assert.IsFalse(result.IsError);
            Assert.AreEqual("hello", result.Content[0].Text);
        }

        [UnityTest]
        public IEnumerator Enqueue_ThrowingAction_ReturnsError()
        {
            var task = _queue.Enqueue(() =>
            {
                throw new InvalidOperationException("boom");
#pragma warning disable CS0162
                return Task.FromResult(ToolResult.Success("unreachable"));
#pragma warning restore CS0162
            });

            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Content[0].Text.Contains("boom"),
                $"Error should contain exception message, got: {result.Content[0].Text}");
        }

        [UnityTest]
        public IEnumerator Enqueue_FailedTask_ReturnsError()
        {
            var task = _queue.Enqueue(() =>
                Task.FromException<ToolResult>(new InvalidOperationException("task failed")));

            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Content[0].Text.Contains("task failed"),
                $"Error should contain exception message, got: {result.Content[0].Text}");
        }

        [UnityTest]
        public IEnumerator Enqueue_MultipleItems_AllComplete()
        {
            var t1 = _queue.Enqueue(() => Task.FromResult(ToolResult.Success("first")));
            var t2 = _queue.Enqueue(() => Task.FromResult(ToolResult.Success("second")));
            var t3 = _queue.Enqueue(() => Task.FromResult(ToolResult.Success("third")));

            while (!t1.IsCompleted || !t2.IsCompleted || !t3.IsCompleted)
                yield return null;

            Assert.AreEqual("first", t1.Result.Content[0].Text);
            Assert.AreEqual("second", t2.Result.Content[0].Text);
            Assert.AreEqual("third", t3.Result.Content[0].Text);
        }

        [Test]
        public void Start_CalledTwice_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _queue.Start());
        }

        [Test]
        public void Stop_CalledTwice_DoesNotThrow()
        {
            _queue.Stop();
            Assert.DoesNotThrow(() => _queue.Stop());
        }
    }
}
