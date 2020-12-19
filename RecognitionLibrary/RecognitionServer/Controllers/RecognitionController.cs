using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using RecognitionLibrary;
using System.Threading.Tasks;
using System.IO;

namespace RecognitionServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RecognitionController : ControllerBase // eventhandler add new entity
    {
        NNModel model; 

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

            model = new NNModel(modelPath, classPath);
            return "SUCCESSFULLY CREATED MODEL: OK";
        }

        [HttpGet]
        public long Get()
        {
           
            var q = from item in model.recognitionLibraryContext.RecognitionImages
                    where model.recognitionLibraryContext.FindOne(new RecognitionInfo(item.Path, item.Label.ToString(), item.Confidence)) != null
                    select item.Statistic;
            return q.FirstOrDefault();
        }

        [HttpDelete]
        public void Delete()
        {
            lock (model.recognitionLibraryContext)
            {
                model.recognitionLibraryContext.Clear();
            }
        }



    }



}
