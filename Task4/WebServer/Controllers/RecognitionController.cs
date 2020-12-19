﻿using Microsoft.AspNetCore.Mvc;
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
using System.Diagnostics;
using System.Net.Http;

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
        public List<RecognitionContract> Post([FromBody] Dictionary<string, string> imgs)
        {
           
            List<RecognitionContract> predictionResults = new List<RecognitionContract>();

            imgs.Values.ToArray();
            model.MakePrediction(imgs);

            lock (model.recognitionLibraryContext)
            {
                foreach (var i in imgs)
                {
                    RecognitionInfo temp = new RecognitionInfo(i.Key, "", 0);
                    temp.Image = Convert.FromBase64String(i.Value);
                    var res = model.recognitionLibraryContext.FindOne(temp);
                    if (res == null)
                    {
                        Trace.WriteLine("Post null");
                    }
                    else
                    {
                        byte[] tmp1 = res.ImageDetails.Image;
                        var converted = Convert.ToBase64String(tmp1);
                        predictionResults.Add(new RecognitionContract(i.Key, res.Label.ToString(), res.Confidence, converted));
                    }
                    Trace.WriteLine("Post " + i.Key + " " + res.Label + res.Confidence);
                }
            }

            return predictionResults;
        }

        [HttpGet]
        public IQueryable<string> Get()
        {
            var q = from item in model.recognitionLibraryContext.RecognitionImages
                        group item by item.Label into g
                        select g.Key + ":  " + g.Count();
            
            return q;            
        }


        [HttpPost("statistic")]
        public string GetImage([FromBody] List<string> img)
        {
            RecognitionInfo tmp = new RecognitionInfo(img[0],"", 0);
            tmp.Image = Convert.FromBase64String(img[1]);
            var res = model.recognitionLibraryContext.FindOne(tmp);

            return res.Statistic.ToString();
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
