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

        private spaicial_dbEntities db = new spaicial_dbEntities();

        // GET: api/Train?id=3&dataSubject=wind%20speed
        [ResponseType(typeof(string))]
        public async Task<IHttpActionResult> GetTrainZone(int id, string dataSubject)
        {

            Zone zoneToTrain = await db.Zone.FindAsync(id);

            if (zoneToTrain == null)
            {
                return NotFound();
            }

            DataSubject predictedDataSubject = db.DataSubject.Where(d => d.label == dataSubject).First();

            IQueryable<Feature> featuresToTrain = db.Feature.Where(f => (f.predictedDataSubjectId == predictedDataSubject.dataSubjectId)
                                                                        &&(f.predictedZoneId == zoneToTrain.zoneId));

            double[] newFeatureWeights = TrainingDataHelpers.getOptimisedValuesOfPrediction(zoneToTrain,predictedDataSubject,featuresToTrain,100, ref db);

            Bias biasToUpdate = db.Bias.Find(zoneToTrain.zoneId, predictedDataSubject.dataSubjectId);
            saveFeatureValues(newFeatureWeights, biasToUpdate, featuresToTrain);

            return Ok("hello");
        }



        /// <summary>
        /// Save array of optimised feature values to database within their repective tables 
        /// first value of array should be for bias and remaining are for features in matching order.
        /// </summary>
        /// <param name="optimisedValues">newly optimised multiplier values of features</param>
        /// <param name="biasObject">bias object of specific prediction</param>
        /// <param name="featuresToTrain">feature objects of specific prediction</param>
        /// <param name="dbObject">refference to current database connection object </param>
        /// <returns></returns>
        private bool saveFeatureValues(double[] optimisedValues, Bias biasObject, IQueryable<Feature> featuresToTrain)
        {
            biasObject.multiValue = optimisedValues[0];
            db.Entry(biasObject).State = EntityState.Modified;
            int savedIndex = 1;
            foreach (var featureToSave in featuresToTrain)
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
