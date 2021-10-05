using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ass1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string dir = Console.ReadLine();
            ImageClassifier imageClassifierModel = new ImageClassifier(dir);
            await foreach (string file in imageClassifierModel.ProcessDirectoryContentsAsync())
            {
                Console.WriteLine(file);
            }
        }
    }
}
