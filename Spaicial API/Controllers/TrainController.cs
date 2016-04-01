using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
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

        private class ValidFeatureData
        {
            public List<double> values;
            public Vector<double> valuesVector;
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

            var predictedDataSubject = db.DataSubject.Where(d => d.label == dataSubject).First();
            int predictedDataSubjectId = predictedDataSubject.dataSubjectId;

            var featuresToTrain = zoneToTrain.Feature1.Where(f => f.predictedDataSubjectId == predictedDataSubjectId);
           
            //get scout data that is in the area of the zone and has the data subject we want to predict
            var validScoutData = db.ScoutData.Where(s => (s.ScoutDataPart.Any(p => p.dataSubjectId == predictedDataSubjectId)))
                .Where(s => s.locationPoint.Intersects(zoneToTrain.locationArea));


            //store unique feature relationships ignoring exponants
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

            //get dateTimes of  station data that matches with scout data and take first 100 rows
            var validDatesConcideringScoutData = from dataPart in validScoutData.Where(s => (validDateTimes.Any(v => v == s.dateTimeCollected)))
                                      .OrderByDescending(s => s.dateTimeCollected)
                                      .Take(100)
                                      select (dataPart.dateTimeCollected);


            //fill list with dateTimes for each feature in order of the dateTimes
            List<ValidFeatureData> validFeatureDataValues = new List<ValidFeatureData>();

            foreach (var uniqueRelationship in featureRelationshps)
            {
                ValidFeatureData fetchedValidData = new ValidFeatureData {
                    sourceZoneId = uniqueRelationship.sourceZoneId,
                    sourceDataSubjectId = uniqueRelationship.sourceDataSubjectId };

                //fill objects list feild with data from valid dateTimes
                fetchedValidData.values = (from value in db.StationDataPart
                                           .Where(s => (s.StationData.zoneId == uniqueRelationship.sourceZoneId)
                                            && (s.dataSubjectId == uniqueRelationship.sourceDataSubjectId)
                                            && (validDatesConcideringScoutData.Any(v => v == s.StationData.dateTimeCollected)))
                                            .OrderByDescending(s => s.StationData.dateTimeCollected)
                                            select value.dataValue).ToList();

                fetchedValidData.valuesVector = DenseVector.OfArray(fetchedValidData.values.ToArray());
                //add object to list obeject
                validFeatureDataValues.Add(fetchedValidData);
            }

            //get scout data values to use as result data for training
            //var validScoutDataValues = from dataPart in validScoutData.Where(d => (validDatesConcideringScoutData.Any(v => v == d.dateTimeCollected)))
            //                           .OrderByDescending(s => s.dateTimeCollected)
            //                           select (dataPart.ScoutDataPart.Where(s => s.dataSubjectId == predictedDataSubjectId).First().dataValue);

            //var validScoutDataValues = (from dataPart in db.ScoutDataPart.Where(s => (s.dataSubjectId == predictedDataSubjectId)
            //                           && (validDatesConcideringScoutData.Any(v => v == s.ScoutData.dateTimeCollected))
            //                           &&(validScoutData.Any(v => v.ScoutDataPart == s)))
            //                           .OrderByDescending(s => s.ScoutData.dateTimeCollected)
            //                           select (dataPart.dataValue)).ToArray();

            var validScoutDataValues = (from dataPart in validScoutData.Where(v => validDatesConcideringScoutData.Any(d => d == v.dateTimeCollected))
                             select (dataPart.ScoutDataPart.Where(s => s.dataSubjectId == predictedDataSubjectId).FirstOrDefault().dataValue)).ToArray();

            //create current feature weights array
            List < double > currentFeatureWeights = new List<double>();

            //add bias
            currentFeatureWeights.Add(db.Bias.Find(zoneToTrain.zoneId, predictedDataSubjectId).multiValue);


            //build initial matrix will first column of 1's for bias
            Matrix<Double> trainingDataMatrix = Matrix<Double>.Build.Dense(validDatesConcideringScoutData.Count(), 1 ,1.0);


            //build up each column of the training data set
            foreach (var feature in featuresToTrain)
            {
                //add current feature weight values
                currentFeatureWeights.Add(feature.multiValue);

                //get objecct conatining data relivent to current feature
                var currentFeatureData = validFeatureDataValues.Where(v => (v.sourceZoneId == feature.sourceZoneId) 
                                                    && (v.sourceDataSubjectId == feature.sourceDataSubjectId)).First();

                //apply feature scalling and exponant values to stored featur data
                double featureScale = feature.DataSubject.maxValue - feature.DataSubject.minValue;

                trainingDataMatrix = trainingDataMatrix.InsertColumn(trainingDataMatrix.ColumnCount, (currentFeatureData.valuesVector
                                                                                    .Divide(featureScale))
                                                                                    .PointwisePower(feature.expValue));
            }

            //get scale of predicted data
            double predictionScale = predictedDataSubject.maxValue - predictedDataSubject.minValue;

            //create vectore of result data
            Vector<Double> trainingResultData = DenseVector.OfArray(validScoutDataValues).Divide(predictionScale);

            //create array of itial feature values
            double[] intialFeatureWeights = currentFeatureWeights.ToArray();


            double[] newFeatureWeights = Learning.Learn(intialFeatureWeights, trainingDataMatrix, trainingResultData);




            return Ok("hello");
        }

    }
}
