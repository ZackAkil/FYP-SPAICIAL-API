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


           return Ok(new PredictionResponse { value = 0.0 });
        }


    }
}
