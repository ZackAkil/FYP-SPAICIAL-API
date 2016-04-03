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

            Predictor predictor = new Predictor(zoneToTrain, predictedDataSubject);

            predictor.GetPrediction();
           
            return Ok(new PredictionResponse { value = predictor.predictionValue, latestDataUsed = predictor.predictionAge });
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
