using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.Entity.Spatial;

namespace Spaicial_API.Models
{
    public abstract class DataCollector
    {
        public DateTime dateTimeCollected { get; set; }
        public string apiKey { get; set; }
        public string[] dataLables { get; set; }
        public string[] dataValues { get; set; }

    }

    public class ScoutDataCollector : DataCollector
    {
        public double longitude { get; set; }
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
                DataSubject dataSubject = db.DataSubject.Where(d => d.label == lable).First();

                ScoutDataPart dataPartToAdd = new ScoutDataPart();
                dataPartToAdd.dataSubjectId = dataSubject.dataSubjectId;
                dataPartToAdd.dataValue = System.Convert.ToDouble(dataValues[i]);

                convert.ScoutDataPart.Add(dataPartToAdd);
            }

            return convert;
        }
    }

    public class StationDataCollector : DataCollector
    {
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
                DataSubject dataSubject = db.DataSubject.Where(d => d.label == lable).First();

                StationDataPart dataPartToAdd = new StationDataPart();
                dataPartToAdd.dataSubjectId = dataSubject.dataSubjectId;
                dataPartToAdd.dataValue = System.Convert.ToDouble(dataValues[i]);

                convert.StationDataPart.Add(dataPartToAdd);
            }

            return convert;
        }
    }
}