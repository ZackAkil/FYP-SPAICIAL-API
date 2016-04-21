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
    /// Responsible for providing a way for station data sources to submit data into the system
    /// </summary>
    public class StationDataController : ApiController
    {
        private spaicial_dbEntities db = new spaicial_dbEntities();

        /// <summary>
        /// Parses and submits station data into the system
        /// </summary>
        /// <param name="stationDataCollector">data conatiner for station data</param>
        /// <returns>confirmation string message when complete</returns>
        [ResponseType(typeof(string))]
        public async Task<IHttpActionResult> PostStationData(StationDataCollector stationDataCollector)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Zone stationZone = await db.Zone.FindAsync(stationDataCollector.zoneId);
            if (stationZone == null)
            {
                return NotFound();
            }

            ApiKeyAuthentication.CheckApiKey(stationDataCollector.apiKey, stationZone, ref db);

            int apiKeyId = db.ApiKey.Where(a => a.keyValue == stationDataCollector.apiKey).First().apiKeyId;

            StationData stationData = stationDataCollector.ConvertToDb(apiKeyId,db);

            db.StationData.Add(stationData);
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