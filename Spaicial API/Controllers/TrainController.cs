using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Spaicial_API.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace Spaicial_API.Controllers
{
    public class TrainController : ApiController
    {

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
            //add api check

            Zone zoneToTrain = await db.Zone.FindAsync(id);

            if (zoneToTrain == null)
            {
                return NotFound();
            }

            DataSubject predictedDataSubject = db.DataSubject.Where(d => d.label == dataSubject).First();
            var featuresToTrain = zoneToTrain.Feature1.Where(f => f.predictedDataSubjectId == predictedDataSubject.dataSubjectId);
            //get scout data that is in the area of the zone and has the data subject we want to predict
            var validScoutData = db.ScoutData.Where(s => (s.ScoutDataPart.Any(p => p.dataSubjectId == predictedDataSubject.dataSubjectId)))
                .Where(s => s.locationPoint.Intersects(zoneToTrain.locationArea));
            //store unique feature relationships ignoring exponants
            List<FeatureRelationship> featureRelationshps = getUniqueFeatureRelationships(featuresToTrain);
            //get dateTimes of complete data and take first 100 rows
            var validDatesConcideringScoutData = getLatestDatesOfCompleteData(100, featuresToTrain,validScoutData, featureRelationshps,ref db);
            //get list of data per unquie feature relationship 
            List<ValidFeatureData> validFeatureDataValues = getFeatureData(featureRelationshps, validDatesConcideringScoutData, ref db);
            //get valid scout data to be used as result data
            double[] validScoutDataValues = getScoutData(validScoutData, validDatesConcideringScoutData, predictedDataSubject);
            //create current feature weights array
            List<double> currentFeatureWeights = getCurrentFeatureWeights(zoneToTrain, predictedDataSubject, featuresToTrain, ref db);
            //create training data matrix
            Matrix<Double> trainingDataMatrix = createTrainingDataMatrix(validDatesConcideringScoutData, featuresToTrain, validFeatureDataValues);
            //get scale of predicted data
            double predictionScale = predictedDataSubject.maxValue - predictedDataSubject.minValue;
            //create vectore of result data
            Vector<Double> trainingResultData = DenseVector.OfArray(validScoutDataValues).Divide(predictionScale);
            //create array of itial feature values
            double[] intialFeatureWeights = currentFeatureWeights.ToArray();
            //send to learning method to optimise values of feature weights
            double[] newFeatureWeights = Learning.Learn(intialFeatureWeights, trainingDataMatrix, trainingResultData);
            var biasToUpdate = db.Bias.Find(zoneToTrain.zoneId, predictedDataSubject.dataSubjectId);
            //save vnew feature values to database
            saveFeatureValues(newFeatureWeights, biasToUpdate, featuresToTrain);

            return Ok("hello");
        }

        private static double[] getScoutData(IQueryable<ScoutData> validScoutData, IQueryable<DateTime> validDatesConcideringScoutData
            , DataSubject predictedDataSubject)
        {

            double[] validScoutDataValues = (from dataPart in validScoutData.Where(v => validDatesConcideringScoutData
                                        .Any(d => d == v.dateTimeCollected))
                                        .OrderByDescending(d => d.dateTimeCollected)
                                        select (dataPart.ScoutDataPart.Where(s => s.dataSubjectId == predictedDataSubject.dataSubjectId)
                                        .FirstOrDefault().dataValue)).ToArray();

            return validScoutDataValues;
        }

        private static List<double> getCurrentFeatureWeights(Zone zoneToTrain, DataSubject predictedDataSubject
            , IEnumerable<Feature> featuresToTrain, ref spaicial_dbEntities db)
        {
            //create current feature weights array
            List<double> currentFeatureWeights = new List<double>();
            //add bias
            currentFeatureWeights.Add(db.Bias.Find(zoneToTrain.zoneId, predictedDataSubject.dataSubjectId).multiValue);

            foreach (var feature in featuresToTrain)
            {
                //add current feature weight values
                currentFeatureWeights.Add(feature.multiValue);
            }

            return currentFeatureWeights;
        }

        private static Matrix<Double> createTrainingDataMatrix(IQueryable<DateTime> validDatesConcideringScoutData
            , IEnumerable<Feature> featuresToTrain, List<ValidFeatureData> validFeatureDataValues)
        {
            Matrix<Double> trainingDataMatrix = Matrix<Double>.Build.Dense(validDatesConcideringScoutData.Count(), 1, 1.0);
            //build up each column of the training data set
            foreach (var feature in featuresToTrain)
            {
                //get objecct conatining data relivent to current feature
                var currentFeatureData = validFeatureDataValues.Where(v => (v.sourceZoneId == feature.sourceZoneId)
                                                    && (v.sourceDataSubjectId == feature.sourceDataSubjectId)).First();
                //apply feature scalling and exponant values to stored featur data
                double featureScale = feature.DataSubject.maxValue - feature.DataSubject.minValue;

                trainingDataMatrix = trainingDataMatrix.InsertColumn(trainingDataMatrix.ColumnCount, (currentFeatureData.valuesVector
                                                                                    .Divide(featureScale))
                                                                                    .PointwisePower(feature.expValue));
            }
            return trainingDataMatrix;
        }

        private static List<ValidFeatureData> getFeatureData(IList<FeatureRelationship> featureRelationships
            , IQueryable<DateTime> validDatesConcideringScoutData,  ref spaicial_dbEntities db)
        {

            //fill list with dateTimes for each feature in order of the dateTimes
            List<ValidFeatureData> validFeatureDataValues = new List<ValidFeatureData>();

            foreach (var uniqueRelationship in featureRelationships)
            {
                ValidFeatureData fetchedValidData = new ValidFeatureData
                {
                    sourceZoneId = uniqueRelationship.sourceZoneId,
                    sourceDataSubjectId = uniqueRelationship.sourceDataSubjectId
                };

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
            return validFeatureDataValues;
        }

        private static IQueryable<DateTime> getLatestDatesOfCompleteData(int numOfRows, IEnumerable<Feature> featuresToTrain
            , IQueryable<ScoutData> validScoutData, List<FeatureRelationship> featureRelationships,ref spaicial_dbEntities db)
        {

            FeatureRelationship firstRelationship = featureRelationships.First();

            //create query of valid dates that exists across all data relationships,get all data parts that match relationship
            var validDateTimes = from dataPart in db.StationDataPart
                                                  .Where(s => (s.StationData.zoneId == firstRelationship.sourceZoneId)
                                                   && (s.dataSubjectId == firstRelationship.sourceDataSubjectId))
                                                   .OrderByDescending(s => s.StationData.dateTimeCollected)
                                 select (dataPart.StationData.dateTimeCollected);

            //foreach unique relationship collect data which exists in the stationData foreach  reationship with the same dateTimeCollected feild
            foreach (var uniqueRelationship in featureRelationships.Skip(1))
            {
                //store current validDateTimes so that i can be used within updating itself
                var tempValidDateTimes = validDateTimes;
                //for the next interation update the list of valid dates with ones that exist in each relationship
                validDateTimes = from dataPart in db.StationDataPart
                                                  .Where(s => (s.StationData.zoneId == uniqueRelationship.sourceZoneId)
                                                   && (s.dataSubjectId == uniqueRelationship.sourceDataSubjectId)
                                                   && (tempValidDateTimes.Any(v => v == s.StationData.dateTimeCollected)))
                                                   .OrderByDescending(s => s.StationData.dateTimeCollected)
                                 select (dataPart.StationData.dateTimeCollected);
            }

            //get dateTimes of  station data that matches with scout data and take first 100 rows
            var validDatesConcideringScoutData = from dataPart in validScoutData
                                                 .Where(s => (validDateTimes.Any(v => v == s.dateTimeCollected)))
                                                 .OrderByDescending(s => s.dateTimeCollected)
                                                 .Take(numOfRows)
                                                 select (dataPart.dateTimeCollected);

            return validDatesConcideringScoutData;
        }

        private static List<FeatureRelationship> getUniqueFeatureRelationships(IEnumerable<Feature> featuresToTrain)
        {
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
            return featureRelationshps;
        }

        /// <summary>
        /// Save array of optimised feature values to database within their repective tables 
        /// first value of array should be for bias and remaining are for features in matching order.
        /// </summary>
        /// <param name="optimisedValues">newly optimised multiplier values of features</param>
        /// <param name="biasObject">bias object of specific prediction</param>
        /// <param name="featureObjects">feature objects of specific prediction</param>
        /// <param name="dbObject">refference to current database connection object </param>
        /// <returns></returns>
        private bool saveFeatureValues(double[] optimisedValues, Bias biasObject, IEnumerable<Feature> featureObjects)
        {
            biasObject.multiValue = optimisedValues[0];
            db.Entry(biasObject).State = EntityState.Modified;
            int savedIndex = 1;
            foreach (var featureToSave in featureObjects)
            {
                featureToSave.multiValue = optimisedValues[savedIndex];
                db.Entry(featureToSave).State = EntityState.Modified;
                savedIndex++;
            }
            db.SaveChanges();
            return true;
        }
    }


}
