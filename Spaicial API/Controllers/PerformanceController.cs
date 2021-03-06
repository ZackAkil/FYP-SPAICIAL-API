﻿using Spaicial_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace Spaicial_API.Controllers
{
    /// <summary>
    /// Responsible for fetching the current performance of a prediction model
    /// </summary>
    public class PerformanceController : ApiController
    {
        private spaicial_dbEntities db = new spaicial_dbEntities();

        /// <summary>
        /// Returns the current performance of the prediction model
        /// </summary>
        /// <param name="id">id of predicted zone</param>
        /// <param name="dataSubject">label of predicted data subject</param>
        /// <param name="numOfRows">number of previous records to take into account for the performance</param>
        /// <param name="apiKey">valid API key</param>
        /// <returns>double of the current Mean-squared-error for the prediction model</returns>
        [ResponseType(typeof(double))]
        public async Task<IHttpActionResult> GetPerformanceZonePrediction(int id, string dataSubject, int numOfRows, string apiKey)
        {
            ApiKeyAuthentication.CheckApiKey(apiKey, ref db);

            Zone predictedZone = await db.Zone.FindAsync(id);
            if (predictedZone == null)
            {
                return NotFound();
            }

            DataSubject predictedDataSubject = db.DataSubject.Where(d => d.label == dataSubject).First();

            Predictor predictor = new Predictor(predictedZone, predictedDataSubject);

            return Ok(predictor.GetPerformance(numOfRows));
        }

    }
}
