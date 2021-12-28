using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length != 1 && args.Length != 2)
        {
            Console.Error.WriteLine("Usage: %program_name% [get-processed-images|delete-all|detect-objects imageDirectory]");
            Environment.Exit(1);
        }
        HttpClient client = new HttpClient();
        try
        {
            if (args[0] == "get-processed-images")
            {
                string response = await client.GetStringAsync("http://localhost:5000/get-processed-images");
                Console.WriteLine(response);
            }
            else if (args[0] == "delete-all")
            {
                await client.DeleteAsync("http://localhost:5000/delete-all");
            }
            else if (args[0] == "detect-objects")
            {
                var response = await client.GetStringAsync("http://localhost:5000/detect-objects?dir=" + args[1]);
                Console.WriteLine(response);
            }

        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
