using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spaicial_API.Models
{


    /// <summary>
    /// Class with static helper methods for fetching, cleaning executing the non-linear optimisation
    /// </summary>
    public static class TrainingDataHelpers
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


        /// <summary>
        /// Performs fetching and cleaning of data used to predict a specific data subject of a zone and applies 
        /// the non-linear optimisation in order to produce newly optimises feature values.
        /// </summary>
        /// <param name="zoneToTrain">zone which is being predicted</param>
        /// <param name="predictedDataSubject">data subject that is being predicted</param>
        /// <param name="numberOfRowsToUse">number of rows to uses as training data</param>
        /// <param name="db">reference to database object connection</param>
        /// <returns></returns>
        public static double[] GetOptimisedValuesOfPrediction(Zone zoneToTrain, DataSubject predictedDataSubject
            , int numberOfRowsToUse, ref spaicial_dbEntities db)
        {
            IQueryable<Feature> featuresToTrain = db.Feature.Where(f => (f.predictedDataSubjectId == predictedDataSubject.dataSubjectId)
                                                                       && (f.predictedZoneId == zoneToTrain.zoneId));

            //get scout data that is in the area of the zone and has the data subject we want to predict
            var validScoutData = db.ScoutData.Where(s => (s.ScoutDataPart.Any(p => p.dataSubjectId == predictedDataSubject.dataSubjectId)))
                .Where(s => s.locationPoint.Intersects(zoneToTrain.locationArea));
            //store unique feature relationships ignoring exponants
            List<FeatureRelationship> featureRelationshps = GetUniqueFeatureRelationships(featuresToTrain);
            //get dateTimes of complete data and take first 100 rows
            var validDatesConcideringScoutData = GetLatestDatesOfCompleteData(numberOfRowsToUse, featuresToTrain, validScoutData, featureRelationshps, ref db);
            //get list of data per unquie feature relationship 
            List<ValidFeatureData> validFeatureDataValues = GetFeatureData(featureRelationshps, validDatesConcideringScoutData, ref db);
            //get valid scout data to be used as result data
            double[] validScoutDataValues = GetScoutData(validScoutData, validDatesConcideringScoutData, predictedDataSubject);
            //create current feature weights array
            List<double> currentFeatureWeights = GetCurrentFeatureWeights(zoneToTrain, predictedDataSubject, featuresToTrain, ref db);
            //create training data matrix
            Matrix<Double> trainingDataMatrix = CreateTrainingDataMatrix(validDatesConcideringScoutData, featuresToTrain, validFeatureDataValues);
            //get scale of predicted data
            double predictionScale = predictedDataSubject.maxValue - predictedDataSubject.minValue;
            //create vectore of result data
            Vector<Double> trainingResultData = DenseVector.OfArray(validScoutDataValues).Divide(predictionScale);
            //create array of itial feature values
            double[] intialFeatureWeights = currentFeatureWeights.ToArray();
            //send to learning method to optimise values of feature weights
            return Learning.Learn(intialFeatureWeights, trainingDataMatrix, trainingResultData); ;
        }

        private static double[] GetScoutData(IQueryable<ScoutData> validScoutData, IQueryable<DateTime> validDatesConcideringScoutData
            , DataSubject predictedDataSubject)
        {

            double[] validScoutDataValues = (from dataPart in validScoutData.Where(v => validDatesConcideringScoutData
                                        .Any(d => d == v.dateTimeCollected))
                                        .OrderByDescending(d => d.dateTimeCollected)
                                             select (dataPart.ScoutDataPart.Where(s => s.dataSubjectId == predictedDataSubject.dataSubjectId)
                                             .FirstOrDefault().dataValue)).ToArray();

            return validScoutDataValues;
        }

        private static List<double> GetCurrentFeatureWeights(Zone zoneToTrain, DataSubject predictedDataSubject
            , IQueryable<Feature> featuresToTrain, ref spaicial_dbEntities db)
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

        private static Matrix<Double> CreateTrainingDataMatrix(IQueryable<DateTime> validDatesConcideringScoutData
            , IQueryable<Feature> featuresToTrain, List<ValidFeatureData> validFeatureDataValues)
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

        private static List<ValidFeatureData> GetFeatureData(IList<FeatureRelationship> featureRelationships
            , IQueryable<DateTime> validDatesConcideringScoutData, ref spaicial_dbEntities db)
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

        private static IQueryable<DateTime> GetLatestDatesOfCompleteData(int numOfRows, IQueryable<Feature> featuresToTrain
            , IQueryable<ScoutData> validScoutData, List<FeatureRelationship> featureRelationships, ref spaicial_dbEntities db)
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

        private static List<FeatureRelationship> GetUniqueFeatureRelationships(IQueryable<Feature> featuresToTrain)
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

    }
}