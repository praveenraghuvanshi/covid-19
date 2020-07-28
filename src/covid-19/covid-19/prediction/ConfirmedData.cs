using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ML.Data;

namespace covid_19.prediction
{
    /// <summary>
    /// Represent data for confirmed cases with a mapping to columns in a dataset
    /// </summary>
    public class ConfirmedData
    {
        /// <summary>
        /// Date of confirmed case
        /// </summary>
        [LoadColumn(0)]
        public DateTime Date;

        /// <summary>
        /// Total no of confirmed cases on a particular date
        /// </summary>
        [LoadColumn(1)]
        public float TotalConfirmed;
    }
}
