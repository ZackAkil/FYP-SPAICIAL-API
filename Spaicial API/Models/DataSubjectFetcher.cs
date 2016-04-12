using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace Spaicial_API.Models
{
    public class DataSubjectFetcher
    {
        public static DataSubject getDataSubject(string dataSubjectLable,ref spaicial_dbEntities db)
        {

            DataSubject dataSubject = db.DataSubject.Where(d => d.label == dataSubjectLable).FirstOrDefault();

            if (dataSubject == null)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Data subject is not valid."),
                    ReasonPhrase = "The sata subject passed is not part of the system. Did you spell it wrong?"
                });
            }

            return dataSubject;
        }
    }
}