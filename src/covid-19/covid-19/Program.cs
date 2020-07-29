using System;
using covid_19.eda;
using covid_19.prediction;

namespace covid_19
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("**** Welcome to world of data science and machine learning using Microsoft .Net!!! ****");

            #region EDA

            Console.WriteLine("Performing Exploratory Data Analysis\n");

            var exploratoryDataAnalysis = new ExploratoryDataAnalysis();
            exploratoryDataAnalysis.Analyze();

            Console.WriteLine("Exploratory Data Completed\n");

            #endregion

            #region Time Series Prediction

            Console.WriteLine("\nPress any key to start Time Series Prediction\n");
            Console.ReadKey();

            var timeSeriesPrediction = new TimeSeriesPrediction();
            timeSeriesPrediction.Forecast();

            Console.WriteLine("Time Series Prediction Completed\n");

            #endregion

            Console.WriteLine("******* END *********");
            Console.ReadLine();
        }
    }
}
