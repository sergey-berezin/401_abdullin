using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YOLOv4MLNet.DataStructures;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: %program_name% imageDirectory");
            Environment.Exit(1);
        }
        string dir = args[0];
        ImageClassifier imageClassifierModel = new ImageClassifier(dir);
        await foreach (Tuple<string, List<YoloV4Result>> imgRes in
                       imageClassifierModel.ProcessDirectoryContentsAsync())
        {
            Console.WriteLine(imgRes.Item1); // file name

            foreach (YoloV4Result res in imgRes.Item2)
            {
                Console.Write("    "); // padding
                // x1, y1, x2, y2 in page coordinates.
                // left, top, right, bottom.
                float x1 = res.BBox[0];
                float y1 = res.BBox[1];
                float x2 = res.BBox[2];
                float y2 = res.BBox[3];
                Console.WriteLine($"{res.Label}: x1 = {x1}, y1 = {y1}, x2 = {x2}, y2 = {y2}");
            }
        }
    }
}
