using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.Entity.Spatial;
using System.Web.Http;
using System.Net;
using System.Net.Http;

namespace Spaicial_API.Models
{
    public abstract class DataCollector
    {
        /// <summary>
        /// datTime that the data was collected at
        /// </summary>
        public DateTime dateTimeCollected { get; set; }
        /// <summary>
        /// valid api key
        /// </summary>
        public string apiKey { get; set; }
        /// <summary>
        /// array of labels that correspond to the feature labels of the data being submitted
        /// </summary>
        public string[] dataLables { get; set; }
        /// <summary>
        /// array of data value that correspond to the feature labels of the data being submitted
        /// </summary>
        public string[] dataValues { get; set; }

    }

    /// <summary>
    /// Container for scout data
    /// </summary>
    public class ScoutDataCollector : DataCollector
    {
        /// <summary>
        /// longitude of location that the data was collected at
        /// </summary>
        public double longitude { get; set; }
        /// <summary>
        /// latitude of location that the data was collected at
        /// </summary>
        public double latitude { get; set; }

        public ScoutData ConvertToDb(int confirmedApiKeyId, spaicial_dbEntities db)
        {
            ScoutData convert = new ScoutData();
            convert.apiKeyId = confirmedApiKeyId;
            convert.dateTimeCollected = this.dateTimeCollected;

            string point = string.Format("POINT({1} {0})", latitude, longitude);
            convert.locationPoint = DbGeometry.FromText(point);

            //turn data arrays into data part objects
            for (int i = 0; i < dataLables.Length; i++)
            {
                string lable = dataLables[i];
                DataSubject dataSubject = DataSubjectFetcher.getDataSubject(lable, ref db);

                ScoutDataPart dataPartToAdd = new ScoutDataPart();
                dataPartToAdd.dataSubjectId = dataSubject.dataSubjectId;
                dataPartToAdd.dataValue = System.Convert.ToDouble(dataValues[i]);

                convert.ScoutDataPart.Add(dataPartToAdd);
            }

            return convert;
        }
    }

    /// <summary>
    /// Container for station data
    /// </summary>
    public class StationDataCollector : DataCollector
    {
        /// <summary>
        /// id of station zone
        /// </summary>
        public int zoneId { get; set; }

        public StationData ConvertToDb(int confirmedApiKeyId, spaicial_dbEntities db)
        {
            StationData convert = new StationData();
            convert.apiKeyId = confirmedApiKeyId;
            convert.dateTimeCollected = this.dateTimeCollected;
            convert.zoneId = this.zoneId;

            //turn data arrays into data part objects
            for (int i = 0; i < dataLables.Length; i++)
            {
                string lable = dataLables[i];
                DataSubject dataSubject = DataSubjectFetcher.getDataSubject(lable,ref db);

                StationDataPart dataPartToAdd = new StationDataPart();
                dataPartToAdd.dataSubjectId = dataSubject.dataSubjectId;
                dataPartToAdd.dataValue = System.Convert.ToDouble(dataValues[i]);

                convert.StationDataPart.Add(dataPartToAdd);
            }

            return convert;
        }
    }
}