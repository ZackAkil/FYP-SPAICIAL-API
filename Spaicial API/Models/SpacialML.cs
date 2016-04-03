using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spaicial_API.Models
{
    public class FeatureRelationship
    {
        public int sourceZoneId;
        public int sourceDataSubjectId;
    }

    public class ValidFeatureData
    {
        public List<double> values;
        public Vector<double> valuesVector;
        public int sourceZoneId;
        public int sourceDataSubjectId;
    }

    public class SpacialML
    {
        private spaicial_dbEntities db = new spaicial_dbEntities();

        private Zone predictedZone;
        private DataSubject predictedDataSubject;
        private IQueryable<Feature> featuresToPredict;
        private List<FeatureRelationship> uniqueFeatureRelationships;

        public SpacialML(Zone predictedZone, DataSubject predictedDataSubject)
        {
            this.predictedZone = predictedZone;
            this.predictedDataSubject = predictedDataSubject;

            featuresToPredict = db.Feature.Where(f => (f.predictedDataSubjectId == predictedDataSubject.dataSubjectId)
                                                                     && (f.predictedZoneId == predictedZone.zoneId));

            uniqueFeatureRelationships = GetUniqueFeatureRelationships();
        }

        private List<FeatureRelationship> GetUniqueFeatureRelationships()
        {

            List<FeatureRelationship> uniqueFeatureRelationshps = new List<FeatureRelationship>();

            foreach (var feature in featuresToPredict)
            {
                FeatureRelationship featureCheck = new FeatureRelationship
                {
                    sourceZoneId = feature.sourceZoneId,
                    sourceDataSubjectId = feature.sourceDataSubjectId
                };

                if (!uniqueFeatureRelationshps.Any(f => (f.sourceZoneId == featureCheck.sourceZoneId) &&
                (f.sourceDataSubjectId == featureCheck.sourceDataSubjectId)))
                {
                    uniqueFeatureRelationshps.Add(featureCheck);
                }
            }
            return uniqueFeatureRelationshps;
        }

        private IQueryable<DateTime> GetLatestDatesOfCompleteData(int numOfRows, IQueryable<ScoutData> validScoutData)
        {

            FeatureRelationship firstRelationship = uniqueFeatureRelationships.First();

            //create query of valid dates that exists across all data relationships,get all data parts that match relationship
            var validDateTimes = from dataPart in db.StationDataPart
                                                  .Where(s => (s.StationData.zoneId == firstRelationship.sourceZoneId)
                                                   && (s.dataSubjectId == firstRelationship.sourceDataSubjectId))
                                                   .OrderByDescending(s => s.StationData.dateTimeCollected)
                                 select (dataPart.StationData.dateTimeCollected);

            //foreach unique relationship collect data which exists in the stationData foreach  reationship with the same dateTimeCollected feild
            foreach (var uniqueRelationship in uniqueFeatureRelationships.Skip(1))
            {
                //store current validDateTimes so that i can be used within updating itself
                IQueryable<DateTime> tempValidDateTimes = validDateTimes;
                //for the next interation update the list of valid dates with ones that exist in each relationship
                validDateTimes = from dataPart in db.StationDataPart
                                                  .Where(s => (s.StationData.zoneId == uniqueRelationship.sourceZoneId)
                                                   && (s.dataSubjectId == uniqueRelationship.sourceDataSubjectId)
                                                   && (tempValidDateTimes.Any(v => v == s.StationData.dateTimeCollected)))
                                                   .OrderByDescending(s => s.StationData.dateTimeCollected)
                                 select (dataPart.StationData.dateTimeCollected);
            }

            //get dateTimes of  station data that matches with scout data and take first 'numOfRows' rows
            var validDatesConcideringScoutData = from dataPart in validScoutData
                                                 .Where(s => (validDateTimes.Any(v => v == s.dateTimeCollected)))
                                                 .OrderByDescending(s => s.dateTimeCollected)
                                                 .Take(numOfRows)
                                                 select (dataPart.dateTimeCollected);

            return validDatesConcideringScoutData;
        }

        private List<ValidFeatureData> GetFeatureData(IQueryable<DateTime> dateTimesOfCompleteData)
        {

            //fill list with dateTimes for each feature in order of the dateTimes
            List<ValidFeatureData> validFeatureDataValues = new List<ValidFeatureData>();

            foreach (var uniqueRelationship in uniqueFeatureRelationships)
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
                                            && (dateTimesOfCompleteData.Any(v => v == s.StationData.dateTimeCollected)))
                                            .OrderByDescending(s => s.StationData.dateTimeCollected)
                                           select value.dataValue).ToList();

                fetchedValidData.valuesVector = DenseVector.OfArray(fetchedValidData.values.ToArray());
                //add object to list obeject
                validFeatureDataValues.Add(fetchedValidData);
            }
            return validFeatureDataValues;
        }

        private double[] GetScoutData(IQueryable<ScoutData> validScoutData, IQueryable<DateTime> dateTimesOfCompleteData)
        {

            double[] validScoutDataValues = (from dataPart in validScoutData.Where(v => dateTimesOfCompleteData
                                        .Any(d => d == v.dateTimeCollected))
                                        .OrderByDescending(d => d.dateTimeCollected)
                                             select (dataPart.ScoutDataPart.Where(s => s.dataSubjectId == predictedDataSubject.dataSubjectId)
                                             .FirstOrDefault().dataValue)).ToArray();

            return validScoutDataValues;
        }

        private double[] GetFeatureWeights()
        {
            //create current feature weights array
            List<double> currentFeatureWeights = new List<double>();
            //add bias
            currentFeatureWeights.Add(db.Bias.Find(predictedZone.zoneId, predictedDataSubject.dataSubjectId).multiValue);

            IQueryable<Feature> featuresToTrain = db.Feature.Where(f => (f.predictedDataSubjectId == predictedDataSubject.dataSubjectId)
                                                                       && (f.predictedZoneId == predictedZone.zoneId));
            foreach (var feature in featuresToTrain)
            {
                //add current feature weight values
                currentFeatureWeights.Add(feature.multiValue);
            }

            return currentFeatureWeights.ToArray();
        }

    }
}