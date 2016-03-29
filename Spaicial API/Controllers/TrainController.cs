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
        public async Task<IHttpActionResult> GetTrainZone(int id,string dataSubject)
        {
            Zone zoneToTrain = await db.Zone.FindAsync(id);

            int dataSubjectId = db.DataSubject.Where(d => d.label == dataSubject).First().dataSubjectId;

            //get scout data that is in the area of the zone and has the data subject we want to predict
            var scoutData = db.ScoutData.Where(s => (s.ScoutDataPart.Where(p=> p.dataSubjectId == dataSubjectId)).Count()>0)
                .Where(s => s.locationPoint.Intersects(zoneToTrain.locationArea));

            //get station that are mention in prediction
            var stations = zoneToTrain.Feature1.Where(f => f.predictedDataSubjectId == dataSubjectId)
                .Select(f => f.Zone).Distinct().ToList();

            //get data subjects that are mentioned in the prediction
            var dataSubjects = zoneToTrain.Feature1.Where(f => f.predictedDataSubjectId == dataSubjectId)
                .Select(f => f.DataSubject).Distinct();

            //Console.Write(stationData.ToArray().to);
            var count = scoutData.Count();

            if (zoneToTrain == null)
            {
                return NotFound();
            }

            return Ok("hello");
        }

    }
}
