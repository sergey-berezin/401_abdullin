using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


public class ImageClassifier
{
    private string dir;
    private Task<string>[] tasks;
    private CancellationTokenSource cts = new CancellationTokenSource();

    public ImageClassifier(string dir)
    {
        this.dir = dir;
    }

    private Task<string>[] CreateTasks()
    {
        string[] files = Directory.GetFiles(dir);
        tasks = new Task<string>[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            Task<string> task = Task.Factory.StartNew(iCopy =>
            {
                int idx = (int) iCopy;
                string file = files[idx];
                return file;
            }, i, cts.Token);
            tasks[i] = task;
        }
        return tasks;
    }

    // Process tasks as they complete
    public async IAsyncEnumerable<string> ProcessDirectoryContentsAsync()
    {
        CreateTasks();
        foreach (var bucket in Interleaved(tasks))
        {
            var task = await bucket;
            yield return await task;
        }
    }

    public void Wait()
    {
        Task.WaitAll(tasks);
    }

    public void Stop()
    {
        cts.Cancel();
    }

    private static Task<Task<T>> [] Interleaved<T>(Task<T>[] inputTasks)
    {
        //var inputTasks = tasks.ToList();

        var buckets = new TaskCompletionSource<Task<T>>[inputTasks.Length];
        var results = new Task<Task<T>>[buckets.Length];
        for (int i = 0; i < buckets.Length; i++)
        {
            buckets[i] = new TaskCompletionSource<Task<T>>();
            results[i] = buckets[i].Task;
        }

        int nextTaskIndex = -1;
        Action<Task<T>> continuation = completed =>
        {
            var bucket = buckets[Interlocked.Increment(ref nextTaskIndex)];
            bucket.TrySetResult(completed);
        };

        foreach (var inputTask in inputTasks)
        {
            inputTask.ContinueWith(
                    continuation, CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        return results;
    }
}
