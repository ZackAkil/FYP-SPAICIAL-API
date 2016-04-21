using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Spaicial_API.Models;

namespace Spaicial_API.Controllers
{
    /// <summary>
    /// Responsible for providing a way for scout data sources to submit data into the system
    /// </summary>
    public class ScoutDataController : ApiController
    {
        private spaicial_dbEntities db = new spaicial_dbEntities();

            /// <summary>
            /// Parses and submits scout data into teh system
            /// </summary>
            /// <param name="scoutDataCollector">data container for scout data</param>
            /// <returns>structure of input data if succeful</returns>
        [ResponseType(typeof(string))]
        public async Task<IHttpActionResult> PostScoutData(ScoutDataCollector scoutDataCollector)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ApiKeyAuthentication.CheckApiKey(scoutDataCollector.apiKey, ref db);

            int apiKeyId = db.ApiKey.Where(a => a.keyValue == scoutDataCollector.apiKey).First().apiKeyId;

            ScoutData scoutData = scoutDataCollector.ConvertToDb(apiKeyId, db);

            db.ScoutData.Add(scoutData);
            await db.SaveChangesAsync();

            return Ok("Submission successful");
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