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
    public class TrainController : ApiController
    {

        private spaicial_dbEntities db = new spaicial_dbEntities();

        // GET: api/ScoutData/5
        [ResponseType(typeof(string))]
        public async Task<IHttpActionResult> GetTrainZone(int id)
        {
            Zone zoneToTrain = await db.Zone.FindAsync(id);
            var scoutData = db.ScoutData.Where(s => s.locationPoint.Intersects(zoneToTrain.locationArea));

            var count = scoutData.Count();

            if (zoneToTrain == null)
            {
                return NotFound();
            }

            return Ok("hello");
        }

    }
}
