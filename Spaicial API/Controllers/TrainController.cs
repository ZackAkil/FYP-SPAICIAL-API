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
        public async Task<IHttpActionResult> GetTrainZone(int id, string dataSubject)
        {
            Zone zoneToTrain = await db.Zone.FindAsync(id);

            if (zoneToTrain == null)
            {
                return NotFound();
            }

            int predictedDataSubjectId = db.DataSubject.Where(d => d.label == dataSubject).First().dataSubjectId;

            var featuresToTrain = zoneToTrain.Feature1.Where(f => f.predictedDataSubjectId == predictedDataSubjectId);
           
            //get scout data that is in the area of the zone and has the data subject we want to predict
            var scoutData = db.ScoutData.Where(s => (s.ScoutDataPart.Where(p => p.dataSubjectId == predictedDataSubjectId)).Count() > 0)
                .Where(s => s.locationPoint.Intersects(zoneToTrain.locationArea));


            //join scout data with station data that has same recorded time
            var scoutDataAndStationData =
                from sc in scoutData
                join st in db.StationData on sc.dateTimeCollected equals st.dateTimeCollected
                select new { scout = sc, station = st };

            //get stations that are mentioned in prediction
            var stationsMentioned = zoneToTrain.Feature1.Where(f => f.predictedDataSubjectId == predictedDataSubjectId)
                .Select(f => f.Zone).Distinct();
            
            //get data subjects that are mentioned in the prediction
            var dataSubjectsMentioned = zoneToTrain.Feature1.Where(f => f.predictedDataSubjectId == predictedDataSubjectId)
                .Select(f => f.DataSubject).Distinct();

            //create jagged array of same size as all features that will be optimized (+1 for bias)
            double[][] trainingData = new double[featuresToTrain.Count() + 1][];

            //build up training data
            foreach (var scoutDataItem in scoutData)
            {
                foreach (var stationMentionedItem in stationsMentioned)
                {
                    foreach (var dataSubjectItem in dataSubjectsMentioned)
                    {
                        //if feature with current station and data subject is used in prediction
                        if(featuresToTrain.Where(f => (f.sourceZoneId == stationMentionedItem.zoneId)
                            &(f.sourceDataSubjectId == dataSubjectItem.dataSubjectId)).Any()){

                            var featureDataQuery = db.StationDataPart.Where(s => (s.StationData.zoneId == stationMentionedItem.zoneId)
                           & (s.dataSubjectId == dataSubjectItem.dataSubjectId));

                        }

                    }

                }
            }


            //var queryForDataSubject = db.StationDataPart.Where(s => s.dataSubjectId == 1);
            //var queryForDataSubject2 = db.StationDataPart.Where(s => s.dataSubjectId == 2);

            //var innerJoinQuery =
            //from d in queryForDataSubject
            //join d2 in queryForDataSubject2 on d.stationDataId equals d2.stationDataId
            //select new { stationDataId = d.stationDataId, windSpeed = d.dataValue, windDirection = d2.dataValue, date = d.StationData.dateTimeCollected }; //produces flat sequence

            return Ok("hello");
        }

    }
}
