using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using covid_19.utilities;
using Microsoft.Data.Analysis;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using XPlot.Plotly;

namespace covid_19.prediction
{
    public class TimeSeriesPrediction
    {
        #region Constants

        const string CONFIRMED_DATASET_FILE = @"data\time_series_covid19_confirmed_global_transposed.csv";

        // Forecast API
        const int WINDOW_SIZE = 5;
        const int SERIES_LENGTH = 10;
        const int TRAIN_SIZE = 100;
        const int HORIZON = 7;

        // Dataset
        const int DEFAULT_ROW_COUNT = 10;
        const string TOTAL_CONFIRMED_COLUMN = "TotalConfirmed";
        const string DATE_COLUMN = "Date";

        #endregion

        public void Run()
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

            var dates = new List<string>();
            var totalConfirmedCases = new List<string>();
            for (int index = 0; index < totalConfirmedDateColumn.Length; index++)
            {
                dates.Add(totalConfirmedDateColumn[index].ToString());
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

            #endregion

            #region ML Pipeline

            var pipeline = context.Forecasting.ForecastBySsa(
                nameof(ConfirmedForecast.Forecast),
                nameof(ConfirmedData.TotalConfirmed),
                WINDOW_SIZE,
                SERIES_LENGTH,
                TRAIN_SIZE,
                HORIZON);

            #endregion

            #region Train Model

            var model = pipeline.Fit(data);

            #endregion

            #region Prediction/Forecasting - 7 days

            var forecastingEngine = model.CreateTimeSeriesEngine<ConfirmedData, ConfirmedForecast>(context);
            var forecasts = forecastingEngine.Predict();
            var forecastedConfirmedCases = forecasts.Forecast.Select(x => (int) x);

            Console.ForegroundColor = ConsoleColor.Cyan;
            foreach (var forecastedConfirmedCase in forecastedConfirmedCases)
            {
                Console.WriteLine(forecastedConfirmedCase);
            }

            #endregion

            #region Prediction Visualization

            var lastDate = DateTime.Parse(dates.LastOrDefault());
            var predictionStartDate = lastDate.AddDays(1);

            for (int index = 0; index < HORIZON; index++)
            {
                dates.Add(lastDate.AddDays(index + 1).ToShortDateString());
                totalConfirmedCases.Add(forecasts.Forecast[index].ToString());
            }

            title = "Number of Confirmed Cases over Time";
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

            var chart1 = Chart.Plot(
                new[]
                {
                    new Graph.Scattergl()
                    {
                        x = dates.ToArray(),
                        y = totalConfirmedCases.ToArray(),
                        mode = "lines+markers"
                    }
                },
                layout
            );

            chart1.WithTitle(title);
            // display(chart1);

            #endregion

            #endregion
        }
    }
}
