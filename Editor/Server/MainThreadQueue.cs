using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEditor;

namespace UnityMcp.Editor
{
    /// <summary>
    /// 主线程调度队列。后台线程通过 Enqueue 提交工作项，
    /// 由 EditorApplication.update 回调在主线程逐帧消费。
    /// 每个工作项附带 10 秒超时保护。
    /// </summary>
    public class MainThreadQueue : IMainThreadQueue
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private readonly ConcurrentQueue<WorkItem> _queue = new ConcurrentQueue<WorkItem>();
        private bool _running;

        /// <summary>启动队列消费（注册 EditorApplication.update）。</summary>
        public void Start()
        {
            if (_running) return;
            _running = true;
            EditorApplication.update += ProcessQueue;
        }

        /// <summary>停止队列消费（注销 EditorApplication.update）。</summary>
        public void Stop()
        {
            if (!_running) return;
            _running = false;
            EditorApplication.update -= ProcessQueue;
        }

        /// <summary>
        /// 将一个异步操作入队，在主线程执行。
        /// 返回的 Task 可在后台线程 await，直到主线程完成执行或超时。
        /// </summary>
        public Task<ToolResult> Enqueue(Func<Task<ToolResult>> action)
        {
            var tcs = new TaskCompletionSource<ToolResult>();
            var item = new WorkItem(action, tcs, DateTime.UtcNow);
            _queue.Enqueue(item);
            return tcs.Task;
        }

        /// <summary>每帧由 EditorApplication.update 调用，消费一个队列项。</summary>
        private void ProcessQueue()
        {
            if (!_queue.TryDequeue(out var item))
                return;

            if (DateTime.UtcNow - item.EnqueuedAt > Timeout)
            {
                item.Tcs.TrySetResult(ToolResult.Error("MainThreadQueue: execution timed out (10s)."));
                return;
            }

            try
            {
                var task = item.Action();
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        item.Tcs.TrySetResult(ToolResult.Error($"MainThreadQueue: {t.Exception?.InnerException?.Message ?? t.Exception?.Message}"));
                    else if (t.IsCanceled)
                        item.Tcs.TrySetResult(ToolResult.Error("MainThreadQueue: execution was canceled."));
                    else
                        item.Tcs.TrySetResult(t.Result);
                });
            }
            catch (Exception ex)
            {
                item.Tcs.TrySetResult(ToolResult.Error($"MainThreadQueue: {ex.Message}"));
            }
        }

        private readonly struct WorkItem
        {
            public readonly Func<Task<ToolResult>> Action;
            public readonly TaskCompletionSource<ToolResult> Tcs;
            public readonly DateTime EnqueuedAt;

            public WorkItem(Func<Task<ToolResult>> action, TaskCompletionSource<ToolResult> tcs, DateTime enqueuedAt)
            {
                Action = action;
                Tcs = tcs;
                EnqueuedAt = enqueuedAt;
            }
        }
    }
}
