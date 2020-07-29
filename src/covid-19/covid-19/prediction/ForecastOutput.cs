using System;
using System.Collections.Generic;
using System.Text;

namespace covid_19.prediction
{
    /// <summary>
    /// Represents the output to be used for display
    /// </summary>
    public class ForecastOutput
    {
        /// <summary>
        /// Date of confirmed case
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Number of actual confirmed cases
        /// </summary>
        public float ActualConfirmed { get; set; }

        /// <summary>
        /// Lower bound confirmed cases
        /// </summary>
        public float LowerEstimate { get; set; }

        /// <summary>
        /// Predicted confirmed cases
        /// </summary>
        public float Forecast { get; set; }

        /// <summary>
        /// Upper bound confirmed cases
        /// </summary>
        public float UpperEstimate { get; set; }
    }
}
