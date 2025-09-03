using Cysharp.Threading.Tasks;
using Unity.Jobs;

namespace XGame.Extensions
{
    public static class JobExtensions
    {
        /// <summary>
        /// 异步等待Job完成
        /// </summary>
        public static UniTask CompleteAsync(this JobHandle handle)
        {
            var tcs = new UniTaskCompletionSource();

            JobHandle.ScheduleBatchedJobs(); // 通知JobSystem尽快启动已排队任务

            UniTask.Void(async () =>
            {
                while (!handle.IsCompleted)
                {
                    await UniTask.Yield();
                }

                handle.Complete();
                tcs.TrySetResult();
            });

            return tcs.Task;
        }
    }
}
