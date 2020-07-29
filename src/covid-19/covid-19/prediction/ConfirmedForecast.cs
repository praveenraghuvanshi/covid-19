using System;
using System.Collections.Generic;
using System.Text;

namespace covid_19.prediction
{
    /// <summary>
    /// Prediction/Forecast for Confirmed cases
    /// </summary>
    internal class ConfirmedForecast
    {
        /// <summary>
        /// No of predicted confirmed cases for multiple days
        /// </summary>
        public float[] Forecast { get; set; }

        /// <summary>
        /// The predicted minimum values for the forecasted period.
        /// </summary>
        public float[] LowerBoundConfirmed { get; set; }

        /// <summary>
        /// The predicted maximum values for the forecasted period.
        /// </summary>
        public float[] UpperBoundConfirmed { get; set; }
    }
}
