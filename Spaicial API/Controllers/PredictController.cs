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
    /// <summary>
    /// Responsible for fetching new predictions
    /// </summary>
    public class PredictController : ApiController
    {

        private spaicial_dbEntities db = new spaicial_dbEntities();

        /// <summary>
        /// Generates latest prediction for a specific prediction model
        /// </summary>
        /// <param name="id">zoneId of prediction zone</param>
        /// <param name="dataSubject">label of predicted data subject</param>
        /// <param name="apiKey">valid API key</param>
        /// <returns>value of prediction with dateTime of the data used to generate prediction</returns>
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

            DataSubject predictedDataSubject = DataSubjectFetcher.getDataSubject(dataSubject, ref db);

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
