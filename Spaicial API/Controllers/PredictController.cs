using Spaicial_API.Models;
using System;
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
        // GET: api/Predict?id=3&dataSubject=wind%20speed&apiKey=abcdefg
        [ResponseType(typeof(PredictionResponse))]
        public async Task<IHttpActionResult> GetPrediction(int id, string dataSubject, string apiKey)
        {
            ApiKeyAuthentication.CheckApiKey(apiKey,ref db);

            Zone zoneToTrain = await db.Zone.FindAsync(id);
            if (zoneToTrain == null)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("Zone not found."),
                    ReasonPhrase = "The zone ID provided does not belong to any zones in the system."
                });
            }

            if(zoneToTrain.isPredicted != true)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Zone is not predicted."),
                    ReasonPhrase = "The zone ID provided is not for a predicted zone. You may want to fetch station data instead."
                });
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
