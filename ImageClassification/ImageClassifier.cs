using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using YOLOv4MLNet.DataStructures;
using Microsoft.ML;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;


public class ImageClassifier
{
    private string imageDir;
    private string modelPath;
    private Task<Tuple<string, List<YoloV4Result>>>[] tasks;
    private CancellationTokenSource cts = new CancellationTokenSource();
    private static readonly string[] classesNames = new string[] {
            "person", "bicycle", "car", "motorbike", "aeroplane", "bus",
            "train", "truck", "boat", "traffic light", "fire hydrant",
            "stop sign", "parking meter", "bench", "bird", "cat", "dog",
            "horse", "sheep", "cow", "elephant", "bear", "zebra",
            "giraffe", "backpack", "umbrella", "handbag", "tie",
            "suitcase", "frisbee", "skis", "snowboard", "sports ball",
            "kite", "baseball bat", "baseball glove", "skateboard",
            "surfboard", "tennis racket", "bottle", "wine glass", "cup",
            "fork", "knife", "spoon", "bowl", "banana", "apple",
            "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza",
            "donut", "cake", "chair", "sofa", "pottedplant", "bed",
            "diningtable", "toilet", "tvmonitor", "laptop", "mouse",
            "remote", "keyboard", "cell phone", "microwave", "oven",
            "toaster", "sink", "refrigerator", "book", "clock", "vase",
            "scissors", "teddy bear", "hair drier", "toothbrush" };

    public ImageClassifier(string imageDir,
                           string modelPath = "YOLOv4MLNet/yolov4.onnx")
    {
        this.imageDir = imageDir;
        this.modelPath = modelPath;
    }

    private Task<Tuple<string, List<YoloV4Result>>>[] CreateTasks()
    {
        string[] files = Directory.GetFiles(imageDir);
        tasks = new Task<Tuple<string, List<YoloV4Result>>>[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            Task<Tuple<string, List<YoloV4Result>>> task = Task.Factory.StartNew(iCopy =>
            {
                int idx = (int) iCopy;
                string file = files[idx];
                List<YoloV4Result> results = Predict(modelPath, imageDir, file);
                return Tuple.Create(file, results);
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

    private static List<YoloV4Result>
    Predict(string modelPath, string imageDir, string imageName)
    {
        MLContext mlContext = new MLContext();

        var pipeline = mlContext.Transforms.ResizeImages(inputColumnName: "bitmap",
                                                         outputColumnName: "input_1:0",
                                                         imageWidth: 416,
                                                         imageHeight: 416,
                                                         resizing: ResizingKind.IsoPad)
            .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input_1:0",
                                                       scaleImage: 1f / 255f,
                                                       interleavePixelColors: true))
            .Append(mlContext.Transforms.ApplyOnnxModel(
                shapeDictionary: new Dictionary<string, int[]>()
                {
                    { "input_1:0", new[] { 1, 416, 416, 3 } },
                    { "Identity:0", new[] { 1, 52, 52, 3, 85 } },
                    { "Identity_1:0", new[] { 1, 26, 26, 3, 85 } },
                    { "Identity_2:0", new[] { 1, 13, 13, 3, 85 } },
                },
                inputColumnNames: new[]
                {
                    "input_1:0"
                },
                outputColumnNames: new[]
                {
                    "Identity:0",
                    "Identity_1:0",
                    "Identity_2:0"
                },
                modelFile: modelPath, recursionLimit: 100));

        // Fit on empty list to obtain input data schema
        var model = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new List<YoloV4BitmapData>()));

        // Create prediction engine
        var predictionEngine = mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(model);

        using (var bitmap = new Bitmap(Image.FromFile(Path.Combine(imageDir, imageName))))
        {
            // predict
            var predict = predictionEngine.Predict(new YoloV4BitmapData() { Image = bitmap });
            var results = predict.GetResults(classesNames, 0.3f, 0.7f);
            return (List<YoloV4Result>) results;
        }
    }
}
