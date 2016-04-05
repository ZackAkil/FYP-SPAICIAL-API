using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Spaicial_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
            CheckApiKey(apiKey);

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

        private void CheckApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                var response = new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("No API key"),
                    ReasonPhrase = "The API key was not supplied."
                });

                var challenge = new AuthenticationHeaderValue("valid_ApiKey_required");
                response.Response.Headers.WwwAuthenticate.Add(challenge);

                throw response;

            }
            else if (!db.ApiKey.Any(a => a.keyValue == apiKey))
            {
                var response = new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Invalid API key"),
                    ReasonPhrase = "The API key used does not exist in the system."
                });

                var challenge = new AuthenticationHeaderValue("valid_ApiKey_required");
                response.Response.Headers.WwwAuthenticate.Add(challenge);

                throw response;
            }
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
