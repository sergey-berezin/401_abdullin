using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YOLOv4MLNet.DataStructures;


public class ImageClassifier
{
    private string dir;
    private Task<Tuple<string, List<YoloV4Result>>>[] tasks;
    private CancellationTokenSource cts = new CancellationTokenSource();

    public ImageClassifier(string dir)
    {
        this.dir = dir;
    }

    private Task<Tuple<string, List<YoloV4Result>>>[] CreateTasks()
    {
        string[] files = Directory.GetFiles(dir);
        tasks = new Task<Tuple<string, List<YoloV4Result>>>[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            Task<Tuple<string, List<YoloV4Result>>> task = Task.Factory.StartNew(iCopy =>
            {
                int idx = (int) iCopy;
                string file = files[idx];
                float[] bbox = {1f, 2f, 3f, 4f};
                List<YoloV4Result> res = new List<YoloV4Result>
                    {new YoloV4Result(bbox, "banana", 0.5f)};
                return Tuple.Create(file, res);
            }, i, cts.Token);
            tasks[i] = task;
        }
        return tasks;
    }

    // Process tasks as they complete
    public async IAsyncEnumerable<Tuple<string, List<YoloV4Result>>> ProcessDirectoryContentsAsync()
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
