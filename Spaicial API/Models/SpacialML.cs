using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spaicial_API.Models
{
    public class SpacialML
    {
        private spaicial_dbEntities db = new spaicial_dbEntities();

        private Zone predictedZone;
        private DataSubject predictedDataSubject;
        private IQueryable<Feature> featuresToPredict;

        public SpacialML(Zone predictedZone, DataSubject predictedDataSubject)
        {
            this.predictedZone = predictedZone;
            this.predictedDataSubject = predictedDataSubject;

            featuresToPredict = db.Feature.Where(f => (f.predictedDataSubjectId == predictedDataSubject.dataSubjectId)
                                                                     && (f.predictedZoneId == predictedZone.zoneId));
        }



    }
}