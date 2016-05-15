using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spaicial_API.Models
{
    public class PredictionResponse
    {
        /// <summary>
        /// value of prediction
        /// </summary>
        public double value;
        /// <summary>
        /// dateTime that the data used to generate prediction was collected
        /// </summary>
        public DateTime latestDataUsed;

    }
}