using System;
using System.Threading.Tasks;

namespace UnityMcp.Editor
{
    /// <summary>
    /// 主线程调度队列接口。将工作项提交到主线程执行。
    /// </summary>
    public interface IMainThreadQueue
    {
        /// <summary>
        /// 将一个异步操作入队，在主线程执行。
        /// 返回的 Task 可在后台线程 await，直到主线程完成执行。
        /// </summary>
        Task<ToolResult> Enqueue(Func<Task<ToolResult>> action);
    }
}
