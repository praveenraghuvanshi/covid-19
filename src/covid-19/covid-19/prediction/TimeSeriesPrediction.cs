using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using covid_19.utilities;
using Microsoft.Data.Analysis;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using XPlot.Plotly;

namespace covid_19.prediction
{
    /// <summary>
    /// A class to perform prediction/forecasting on a time series dataset
    /// </summary>
    public class TimeSeriesPrediction
    {
        #region Constants

        const string CONFIRMED_DATASET_FILE = @"data\time_series_covid19_confirmed_global_transposed.csv";
        const string MODEL_PATH = "model.zip";

        // Forecast API
        const int WINDOW_SIZE = 7;
        const int SERIES_LENGTH = 30;
        const int HORIZON = 7;
        const float CONFIDENCE_LEVEL = 0.95f;

        // Dataset
        const int DEFAULT_ROW_COUNT = 10;
        const string TOTAL_CONFIRMED_COLUMN = "TotalConfirmed";
        const string DATE_COLUMN = "Date";

        #endregion

        /// <summary>
        /// Predict/Forecast based on time-series
        /// </summary>
        public void Forecast()
        {
            #region Load Data

            var predictedDataFrame = DataFrame.LoadCsv(CONFIRMED_DATASET_FILE);

            #endregion

            #region Display data

            // Top 5 Rows
            var topRows = predictedDataFrame.Head(5);
            Console.WriteLine("------- Head: Top Rows(5) -------");
            topRows.PrettyPrint();

            // Bottom 5 Rows
            var bottomRows = predictedDataFrame.Tail(5);
            Console.WriteLine("------- Tail: Bottom Rows(5) -------");
            bottomRows.PrettyPrint();

            // Description
            var description = predictedDataFrame.Description();
            Console.WriteLine("------- Description -------");
            description.PrettyPrint();

            #endregion

            #region Visualization

            #region Number of Confirmed cases over Time

            // Number of confirmed cases over time
            var totalConfirmedDateColumn = predictedDataFrame.Columns[DATE_COLUMN];
            var totalConfirmedColumn = predictedDataFrame.Columns[TOTAL_CONFIRMED_COLUMN];

            var dates = new List<DateTime>();
            var totalConfirmedCases = new List<string>();
            for (int index = 0; index < totalConfirmedDateColumn.Length; index++)
            {
                dates.Add(Convert.ToDateTime(totalConfirmedDateColumn[index]));
                totalConfirmedCases.Add(totalConfirmedColumn[index].ToString());
            }

            var title = "Number of Confirmed Cases over Time";
            var confirmedTimeGraph = new Graph.Scattergl()
            {
                x = dates.ToArray(),
                y = totalConfirmedCases.ToArray(),
                mode = "lines+markers"
            };

            var chart = Chart.Plot(confirmedTimeGraph);
            chart.WithTitle(title);
            // display(chart);

            #endregion

            #endregion

            #region Prediction

            #region Load Data - ML Context

            var context = new MLContext();
            var data = context.Data.LoadFromTextFile<ConfirmedData>(CONFIRMED_DATASET_FILE, hasHeader: true, separatorChar: ',');

            #region Split dataset

            var totalRows = (int)data.GetColumn<float>("TotalConfirmed").ToList().Count;
            int numTrain = (int)(0.8 * totalRows);
            var confirmedAtSplit = (int)data.GetColumn<float>("TotalConfirmed").ElementAt(numTrain);
            var startingDate = data.GetColumn<DateTime>("Date").FirstOrDefault();
            var dateAtSplit = data.GetColumn<DateTime>("Date").ElementAt(numTrain);

            IDataView trainData = context.Data.FilterRowsByColumn(data, "TotalConfirmed", upperBound: confirmedAtSplit);
            IDataView testData = context.Data.FilterRowsByColumn(data, "TotalConfirmed", lowerBound: confirmedAtSplit);

            var totalRowsTrain = (int)trainData.GetColumn<float>("TotalConfirmed").ToList().Count;
            var totalRowsTest = (int)testData.GetColumn<float>("TotalConfirmed").ToList().Count;

            Console.WriteLine($"Training dataset range : {startingDate.ToShortDateString()} to {dateAtSplit.ToShortDateString()}");

            #endregion

            #endregion

            #region ML Pipeline

            var pipeline = context.Forecasting.ForecastBySsa(
                nameof(ConfirmedForecast.Forecast),
                nameof(ConfirmedData.TotalConfirmed),
                WINDOW_SIZE,
                SERIES_LENGTH,
                trainSize: numTrain,
                horizon: HORIZON,
                confidenceLevel: CONFIDENCE_LEVEL,
                confidenceLowerBoundColumn: nameof(ConfirmedForecast.LowerBoundConfirmed),
                confidenceUpperBoundColumn: nameof(ConfirmedForecast.UpperBoundConfirmed));

            #endregion

            #region Train Model

            var model = pipeline.Fit(trainData);

            #endregion

            #region Evaluate

            IDataView predictions = model.Transform(testData);

            IEnumerable<float> actual =
                context.Data.CreateEnumerable<ConfirmedData>(testData, true)
                    .Select(observed => observed.TotalConfirmed);

            IEnumerable<float> forecast =
                context.Data.CreateEnumerable<ConfirmedForecast>(predictions, true)
                    .Select(prediction => prediction.Forecast[0]);

            var metrics = actual.Zip(forecast, (actualValue, forecastValue) => actualValue - forecastValue);

            var MAE = metrics.Average(error => Math.Abs(error)); // Mean Absolute Error
            var RMSE = Math.Sqrt(metrics.Average(error => Math.Pow(error, 2))); // Root Mean Squared Error

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Evaluation Metrics");
            Console.WriteLine("---------------------");
            Console.WriteLine($"Mean Absolute Error: {MAE:F3}");
            Console.WriteLine($"Root Mean Squared Error: {RMSE:F3}\n");

            #endregion

            #region Save Model

            var forecastingEngine = model.CreateTimeSeriesEngine<ConfirmedData, ConfirmedForecast>(context);
            forecastingEngine.CheckPoint(context, MODEL_PATH);

            #endregion

            #region Prediction/Forecasting - 7 days

            var forecasts = forecastingEngine.Predict();

            var forecastOuputs = context.Data.CreateEnumerable<ConfirmedData>(testData, reuseRowObject: false)
                .Take(HORIZON)
                .Select((ConfirmedData confirmedData, int index) =>
                {
                    float lowerEstimate = Math.Max(0, forecasts.LowerBoundConfirmed[index]);
                    float estimate = forecasts.Forecast[index];
                    float upperEstimate = forecasts.UpperBoundConfirmed[index];

                    return new ForecastOutput
                    {
                        ActualConfirmed = confirmedData.TotalConfirmed,
                        Date = confirmedData.Date,
                        Forecast = estimate,
                        LowerEstimate = lowerEstimate,
                        UpperEstimate = upperEstimate
                    };
                });

            PrimitiveDataFrameColumn<DateTime> forecastDates = new PrimitiveDataFrameColumn<DateTime>("Date"); 
            PrimitiveDataFrameColumn<float> actualConfirmedCases = new PrimitiveDataFrameColumn<float>("ActualConfirmed"); 
            PrimitiveDataFrameColumn<float> forecastCases = new PrimitiveDataFrameColumn<float>("Forecast"); 
            PrimitiveDataFrameColumn<float> lowerEstimates = new PrimitiveDataFrameColumn<float>("LowerEstimate"); 
            PrimitiveDataFrameColumn<float> upperEstimates = new PrimitiveDataFrameColumn<float>("UpperEstimate"); 

            foreach (var output in forecastOuputs)
            {
                forecastDates.Append(output.Date);
                actualConfirmedCases.Append(output.ActualConfirmed);
                forecastCases.Append(output.Forecast);
                lowerEstimates.Append(output.LowerEstimate);
                upperEstimates.Append(output.UpperEstimate);
            }

            Console.WriteLine("Total Confirmed Cases Forecast");
            Console.WriteLine("---------------------");
            var forecastDataFrame = new DataFrame(forecastDates, actualConfirmedCases, lowerEstimates, forecastCases, upperEstimates);
            forecastDataFrame.PrettyPrint();

            Console.WriteLine(Environment.NewLine);
            Console.ForegroundColor = ConsoleColor.White;

            #endregion

            #region Prediction Visualization

            // var lastDate =  // DateTime.Parse(dates.LastOrDefault());
            var predictionStartDate = dateAtSplit.AddDays(-1); // lastDate.AddDays(1);

            var newDates = new List<DateTime>();
            var fullDates = new List<DateTime>();
            fullDates.AddRange(dates.Take(numTrain));

            var fullTotalConfirmedCases = new List<string>();
            fullTotalConfirmedCases.AddRange(totalConfirmedCases.Take(numTrain));

            for (int index = 0; index < HORIZON; index++)
            {
                var nextDate = predictionStartDate.AddDays(index + 1);
                newDates.Add(nextDate);
                fullTotalConfirmedCases.Add(forecasts.Forecast[index].ToString());
            }

            fullDates.AddRange(newDates);

            var layout = new Layout.Layout();
            layout.shapes = new List<Graph.Shape>
            {
                new Graph.Shape
                {
                    x0 = predictionStartDate.ToShortDateString(),
                    x1 = predictionStartDate.ToShortDateString(),
                    y0 = "0",
                    y1 = "1",
                    xref = 'x',
                    yref = "paper",
                    line = new Graph.Line() {color = "red", width = 2}
                }
            };

            var predictionChart = Chart.Plot(
                new[]
                {
                    new Graph.Scattergl()
                    {
                        x = fullDates.ToArray(),
                        y = fullTotalConfirmedCases.ToArray(),
                        mode = "lines+markers"
                    }
                },
                layout
            );

            predictionChart.WithTitle("Number of Confirmed Cases over Time");
            // display(predictionChart);

            Graph.Scattergl[] scatters = {
                new Graph.Scattergl() {
                    x = newDates,
                    y = forecasts.UpperBoundConfirmed,
                    fill = "tonexty",
                    name = "Upper bound"
                },
                new Graph.Scattergl() {
                    x = newDates,
                    y = forecasts.Forecast,
                    fill = "tonexty",
                    name = "Forecast"
                },
                new Graph.Scattergl() {
                    x = newDates,
                    y = forecasts.LowerBoundConfirmed,
                    fill = "tonexty",
                    name = "Lower bound"
                }
            };


            var predictionChart2 = Chart.Plot(scatters);
            chart.Width = 600;
            chart.Height = 600;
            // display(predictionChart2);

            #endregion

            #endregion
        }
    }
}
