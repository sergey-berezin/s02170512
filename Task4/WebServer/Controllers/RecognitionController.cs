using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RecognitionLibrary;
using Contracts;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

namespace RecognitionServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RecognitionController : ControllerBase // eventhandler add new entity
    {
        NNModel model = new NNModel("", "");
        

        private readonly ILogger<RecognitionController> _logger;

        public RecognitionController(ILogger<RecognitionController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public List<RecognitionInfo> Post([FromBody] string dir)
        {
           
            List<RecognitionInfo> predictionResults = new List<RecognitionInfo>();
            model.MakePrediction(dir);
            lock (model.recognitionLibraryContext)
            {
                foreach (var path in Directory.GetFiles(dir).Where(s => s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".bmp") || s.EndsWith(".gif")))
                {
                    RecognitionInfo temp = new RecognitionInfo(path, "", 0);
                    var res = model.recognitionLibraryContext.FindOne(temp);
                    if (res == null)
                    {
                        predictionResults.Add(temp); 
                    }
                    predictionResults.Add(new RecognitionInfo(path, res.Label.ToString(), res.Confidence));
                }
            }

            return predictionResults;
        }

        [HttpPut]
        public String CreateModel([FromBody] string modelPathclassPath)  //try-catch
        {
            string modelPath = modelPathclassPath.Split("&")[0];
            string classPath = modelPathclassPath.Split("&")[1];
            //modelPath
            model = new NNModel(modelPath, classPath);
            return "SUCCESSFULLY CREATED MODEL: OK";
        }

        [HttpGet]
        public IEnumerable< long> Get()
        {
            //model = new NNModel("","");
            Console.WriteLine(model.ImageDirectory);
            using var dbcontext = new RecognitionLibraryContext();
            var q = from item in model.recognitionLibraryContext.RecognitionImages
                    where model.recognitionLibraryContext.FindOne(new RecognitionInfo(item.Path, item.Label.ToString(), item.Confidence)) != null
                    select item.Statistic;
            return q;
        }

        [HttpDelete]
        public void Delete()
        {
            lock (model.recognitionLibraryContext)
            {
                model.recognitionLibraryContext.Clear();
            }
        }


        /*public void PrintResult(NNModel sender, ConcurrentQueue<RecognitionInfo> result)
        {
            RecognitionInfo tmp;
            result.TryDequeue(out tmp);
            lock (model.recognitionLibraryContext)
            {
                Blob resBlob = new Blob { Image = tmp.Image };
                model.recognitionLibraryContext.Add(new RecognitionImage
                {
                    Path = tmp.Path,
                    Confidence = tmp.Confidence,
                    Statistic = 0,
                    ImageDetails = resBlob,
                    Label = int.Parse(tmp.Class)
                });
                model.recognitionLibraryContext.Blobs.Add(resBlob);
                model.recognitionLibraryContext.SaveChanges();
            }
        }*/
    }
}
