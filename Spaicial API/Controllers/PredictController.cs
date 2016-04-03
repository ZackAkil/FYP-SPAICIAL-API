using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Spaicial_API.Models;
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
    public class PredictController : ApiController
    {

        private class PredictionResponse
        {
            public double value;
            public DateTime latestDataUsed;
        }

        private spaicial_dbEntities db = new spaicial_dbEntities();

        // GET: api/ScoutData/5
        [ResponseType(typeof(PredictionResponse))]
        public async Task<IHttpActionResult> GetPrediction(int id, string dataSubject)
        {

            Zone zoneToTrain = await db.Zone.FindAsync(id);
            if (zoneToTrain == null)
            {
                return NotFound();
            }
            DataSubject predictedDataSubject = db.DataSubject.Where(d => d.label == dataSubject).First();

            //get latest complete record
            DateTime latestRowDate =  TrainingDataHelpers.GetLatestCompleteRow(zoneToTrain, predictedDataSubject, ref db);

            //get data on that date for the feature relationships

            //apply feature scaling and feature transposes 


            //get feature weights
            double[] currentFeatureWeights = TrainingDataHelpers.GetCurrentFeatureWeights(zoneToTrain, predictedDataSubject, ref db);

            Vector<Double> theta = DenseVector.OfArray(currentFeatureWeights); 

            //pass data to learning class to get prediction


            return Ok(new PredictionResponse { value = 0.0 , latestDataUsed = latestRowDate });
        }


    }
}
