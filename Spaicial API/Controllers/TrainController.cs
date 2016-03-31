using Spaicial_API.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
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
        private class FeatureFetch
        {
            public double value;
            public DateTime dateTimeCollected;
        }

        private class FeatureRelationship
        {
            public int sourceZoneId;
            public int sourceDataSubjectId;
        }

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
            var scoutData = db.ScoutData.Where(s => (s.ScoutDataPart.Any(p => p.dataSubjectId == predictedDataSubjectId)))
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

            //save unique feature relationships ignoring exponants
            List<FeatureRelationship> featureRelationshps = new List<FeatureRelationship>();
            foreach (var feature in featuresToTrain)
            {
                FeatureRelationship featureCheck = new FeatureRelationship
                {
                    sourceZoneId = feature.sourceZoneId,
                    sourceDataSubjectId = feature.sourceDataSubjectId
                };

                if (!featureRelationshps.Any(f => (f.sourceZoneId == featureCheck.sourceZoneId) &&
                (f.sourceDataSubjectId == featureCheck.sourceDataSubjectId)))
                {
                    featureRelationshps.Add(featureCheck);
                }
            }

            FeatureRelationship firstRelationship = featureRelationshps.First();

            //create query of valid dates that exists across all data relationships,get all data parts that match relationship
            IQueryable <DateTime> validDateTimes = from dataPart in db.StationDataPart
                                                  .Where(s => (s.StationData.zoneId == firstRelationship.sourceZoneId)
                                                   && (s.dataSubjectId == firstRelationship.sourceDataSubjectId))
                                                   .OrderByDescending(s => s.StationData.dateTimeCollected)
                                                   select (dataPart.StationData.dateTimeCollected);


            //foreach unique relationship collect data which exists in the stationData foreach  reationship with the same dateTimeCollected feild
            foreach (var uniqueRelationship in featureRelationshps.Skip(1))
            {
                //store current validDateTimes so that i can be used within updating itself
                var tempValidDateTimes = validDateTimes;
                //for the next interation update the list of valid dates with ones that exist in each relationship
                validDateTimes = from dataPart in db.StationDataPart
                                                  .Where(s => (s.StationData.zoneId == uniqueRelationship.sourceZoneId)
                                                   && (s.dataSubjectId == uniqueRelationship.sourceDataSubjectId)
                                                   &&(tempValidDateTimes.Any(v => v == s.StationData.dateTimeCollected)))
                                                   .OrderByDescending(s => s.StationData.dateTimeCollected)
                                 select (dataPart.StationData.dateTimeCollected);
            }


            //fill list with dateTimes for each feature in order of the dateTimes

            //create jagged array of same size as all features that will be optimized (+1 for bias)
            double[][] trainingData = new double[featuresToTrain.Count() + 1][];

            //build up training data

            //clean data so that only data that matches via its dateTimeCollected feild is used


            var dataBuild = new List<List<FeatureFetch>>(); 

            foreach (var featureItem in featuresToTrain)
            {
                //get corrisponding feature data 
                var featureDataQuery = db.StationDataPart.Where(s => (s.StationData.zoneId == featureItem.sourceZoneId)
                    && (s.dataSubjectId == featureItem.sourceDataSubjectId));

                var count = featureDataQuery.Count();

                var featureDataArray = (from feat in featureDataQuery select (
                                        new FeatureFetch { value = Math.Pow((feat.dataValue/(feat.DataSubject.maxValue - feat.DataSubject.minValue)),featureItem.expValue),
                                            dateTimeCollected = feat.StationData.dateTimeCollected})).ToList();

                dataBuild.Add(featureDataArray);
                
                var numFound = featureDataArray.Count();
            }


            var countZ = dataBuild.Count;

    
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
