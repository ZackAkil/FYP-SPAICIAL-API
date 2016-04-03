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

        // GET: api/Predict?id=3&dataSubject=wind%20speed
        [ResponseType(typeof(PredictionResponse))]
        public async Task<IHttpActionResult> GetPrediction(int id, string dataSubject)
        {

            Zone zoneToTrain = await db.Zone.FindAsync(id);
            if (zoneToTrain == null)
            {
                return NotFound();
            }
            DataSubject predictedDataSubject = db.DataSubject.Where(d => d.label == dataSubject).First();

            // get feature data matrix
            Matrix<Double> featureData = TrainingDataHelpers.GetTrainingDataMatrix(zoneToTrain, predictedDataSubject,1,ref db);

            //get feature weights
            double[] currentFeatureWeights = TrainingDataHelpers.GetCurrentFeatureWeights(zoneToTrain, predictedDataSubject, ref db);
            Vector<Double> theta = DenseVector.OfArray(currentFeatureWeights);

            //get prediction calculation
            double prediction = (Learning.PredictFunction(featureData, theta))[0];

            double predictionScale = predictedDataSubject.maxValue - predictedDataSubject.minValue;

            prediction = prediction * predictionScale;

            DateTime latestDataUsed = TrainingDataHelpers.GetLatestCompleteRow(zoneToTrain, predictedDataSubject, ref db);

            return Ok(new PredictionResponse { value = prediction , latestDataUsed = latestDataUsed });
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
