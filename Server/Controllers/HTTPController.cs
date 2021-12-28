using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YOLOv4MLNet.DataStructures;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;

using ImageClassification;
using ImageDatabase;

namespace Server.Controllers
{
    [ApiController]
    public class HTTPController : ControllerBase
    {
        private DatabaseStoreContext db;

        public HTTPController(DatabaseStoreContext dB)
        {
            this.db = dB;
        }

        [Route("get-processed-images")]
        public List<ProcessedImage> GetProcessedImages()
        {
            return db.ProcessedImages.ToList();
        }

        [Route("delete-all")]
        public void DeleteAll()
        {
            db.ProcessedImages.RemoveRange(db.ProcessedImages);
            db.RecognizedObjects.RemoveRange(db.RecognizedObjects);
            db.SaveChanges();
        }


        [Route("detect-objects")]
        public async Task<ConcurrentBag<Tuple<string, List<YoloV4Result>>>> DetectObjects(string dir)
        {
            ImageClassifier imageClassifierModel = new ImageClassifier(dir);
            var results = new ConcurrentBag<Tuple<string, List<YoloV4Result>>>();
            await foreach (Tuple<string, List<YoloV4Result>> imgRes in
                           imageClassifierModel.ProcessDirectoryContentsAsync())
            {
                results.Add(imgRes);
                ProcessedImage processedImage = new ProcessedImage(imgRes.Item1);

                foreach (YoloV4Result res in imgRes.Item2)
                {
                    // x1, y1, x2, y2 in page coordinates.
                    // left, top, right, bottom.
                    float x1 = res.BBox[0];
                    float y1 = res.BBox[1];
                    float x2 = res.BBox[2];
                    float y2 = res.BBox[3];
                    string label = res.Label;
                    RecognizedObject recObj = new RecognizedObject(x1, y1, x2, y2, label);
                    processedImage.RecognizedObjects.Add(recObj);
                }
                db.AddProcessedImage(processedImage);
                db.SaveChanges();
            }

            return results;
        }
    }
}
