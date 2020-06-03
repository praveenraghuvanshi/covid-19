# Part-1: COVID-19 Data Analysis using .Net DataFrame API

## COVID-19
- As per [Wiki](https://en.wikipedia.org/wiki/Coronavirus_disease_2019) **Coronavirus disease 2019** (**COVID-19**) is an infectious disease caused by severe acute respiratory syndrome coronavirus 2 (SARS-CoV-2). The disease was first identified in 2019 in Wuhan, the capital of China's Hubei province, and has since spread globally, resulting in the ongoing 2019–20 coronavirus pandemic.
- The virus had caused a pandemic across the globe and spreading/affecting most of the nations. 
- The purpose of notebook is to visualize the trends of virus spread in various countries and explore features present in ML.Net such as DataFrame.

### Acknowledgement
- [Johns Hopkins CSSE](https://github.com/CSSEGISandData/COVID-19/raw/master/csse_covid_19_data) for dataset
- [COVID-19 data visualization](https://www.kaggle.com/akshaysb/covid-19-data-visualization) by Akshay Sb

### Dataset

- [2019 Novel Coronavirus COVID-19 (2019-nCoV) Data Repository by Johns Hopkins CSSE - Daily reports](https://github.com/CSSEGISandData/COVID-19/raw/master/csse_covid_19_data/csse_covid_19_daily_reports).

### Introduction 

[**DataFrame**](https://devblogs.microsoft.com/dotnet/an-introduction-to-dataframe/): DataFrame is a new type introduced in .Net. It is similar to DataFrame in Python which is used to manipulate data in notebooks. It's a collection of columns containing data similar to a table and very helpful in analyzing tabular data. It works flawlessly without creating types/classes mapped to columns in a table which we used to do with [ML.Net](https://dotnet.microsoft.com/apps/machinelearning-ai/ml-dotnet). It has support for GroupBy, Sort, Filter which makes analysis very handy. It's a in-memory representation of structured data.

In this tutorial we'll cover below features
- Load a CSV
- Metadata
    - Description
    - Info
- Display records
    - Head
    - Sample
    - 
- Filtering
- Grouping
- Aggregate

For overview, please refer below links
- [An Introduction to DataFrame](https://devblogs.microsoft.com/dotnet/an-introduction-to-dataframe/)
- [Exploring the C# Dataframe API](https://www.youtube.com/watch?v=FI3VxXClJ7Y)

[**Part-2**](../part-2) covers time series analysis and prediction using ML.Net  

### Summary

Below is the summary of steps we'll be performing

1. Define application level items
    - Nuget packages
    - Namespaces
    - Constants
    
2. Utility Functions
    - Formatters    

3. Load Dataset
    - Download Dataset from [Johns Hopkins CSSE](https://github.com/CSSEGISandData/COVID-19/raw/master/csse_covid_19_data)
    - Load dataset in DataFrame
    
4. Analyze Data
    - Date Range
    - Display Dataset - display(dataframe)
    - Display Top 5 Rows - dataframe.Head(5)
    - Display Random 6 Rows - dataframe.Sample(6)    
    - Display Dataset Statistics - dataframe.Description()
    - Display Dataset type information - dataframe.Info()

5. Data Cleaning
    - Remove Invalid cases

6. Data Visualization
    - Global
        - Confirmed Vs Deaths Vs Recovered
        - Top 5 Countries with Confirmed cases
        - Top 5 Countries with Death cases
        - Top 5 Countries with Recovered cases
    - India
        - Confirmed Vs Deaths Vs Recovered
        

**Note** : Graphs/Plots may not render in GitHub due to security reasons, however if you run this notebook locally/binder they will render.

### 1. Define Application wide Items

#### Nuget Packages



```C#
// ML.NET Nuget packages installation
#r "nuget:Microsoft.ML"
#r "nuget:Microsoft.Data.Analysis"

// Install XPlot package
#r "nuget:XPlot.Plotly"
    
// CSV Helper Package for reading CSV
#r "nuget:CsvHelper"
```


    Installed package XPlot.Plotly version 3.0.1
    Installed package Microsoft.Data.Analysis version 0.4.0
    Installed package CsvHelper version 15.0.5
    Installed package Microsoft.ML version 1.5.0


#### Namespaces


```C#
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Data.Analysis;
using Microsoft.AspNetCore.Html;
using System.IO;
using System.Net.Http;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using XPlot.Plotly;
```

#### Constants


```C#
// Column Names
const string FIPS = "FIPS";
const string ADMIN = "Admin2";
const string STATE = "Province_State";
const string COUNTRY = "Country_Region";
const string LAST_UPDATE = "Last_Update";
const string LATITUDE = "Lat";
const string LONGITUDE = "Long_";
const string CONFIRMED = "Confirmed";
const string DEATHS = "Deaths";
const string RECOVERED = "Recovered";
const string ACTIVE = "Active";
const string COMBINED_KEY = "Combined_Key";

// File
const string DATASET_FILE = "05-27-2020";
const string FILE_EXTENSION = ".csv";
const string NEW_FILE_SUFFIX = "_new";
const char SEPARATOR = ',';
const char SEPARATOR_REPLACEMENT = '_';
const string DATASET_GITHUB_DIRECTORY = "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_daily_reports/";

// DataFrame/Table
const int TOP_COUNT = 5;
const int DEFAULT_ROW_COUNT = 10;
const string VALUES = "Values";
const string INDIA = "India";
```

### 2. Utility Functions

#### Formatters

By default the output of DataFrame is not proper and in order to display it as a table, we need to have a custom formatter implemented as shown in next cell. 


```C#
// Formats the table

Formatter<DataFrame>.Register((df, writer) =>
{
    var headers = new List<IHtmlContent>();
    headers.Add(th(i("index")));
    headers.AddRange(df.Columns.Select(c => (IHtmlContent) th(c.Name)));
    var rows = new List<List<IHtmlContent>>();
    var take = DEFAULT_ROW_COUNT;
    for (var i = 0; i < Math.Min(take, df.Rows.Count); i++)
    {
        var cells = new List<IHtmlContent>();
        cells.Add(td(i));
        foreach (var obj in df.Rows[i])
        {
            cells.Add(td(obj));
        }
        rows.Add(cells);
    }

    var t = table(
        thead(
            headers),
        tbody(
            rows.Select(
                r => tr(r))));

    writer.Write(t);
}, "text/html");
```

#### Copy dataset csv and replace Separator in cells


```C#
// Replace a characeter in a cell of csv with a defined separator
private void CreateCsvAndReplaceSeparatorInCells(string inputFile, string outputFile, char separator, char separatorReplacement)
{
    var culture = CultureInfo.InvariantCulture;
    using var reader = new StreamReader(inputFile);
    using var csvIn = new CsvReader(reader, new CsvConfiguration(culture));
    using var recordsIn = new CsvDataReader(csvIn);
    using var writer = new StreamWriter(outputFile);
    using var outCsv = new CsvWriter(writer, culture);

    // Write Header
    csvIn.ReadHeader();
    var headers = csvIn.Context.HeaderRecord;
    foreach (var header in headers)
    {
        outCsv.WriteField(header.Replace(separator, separatorReplacement));
    }
    outCsv.NextRecord();

    // Write rows
    while (recordsIn.Read())
    {
        var columns = recordsIn.FieldCount;
        for (var index = 0; index < columns; index++)
        {
            var cellValue = recordsIn.GetString(index);
            outCsv.WriteField(cellValue.Replace(separator, separatorReplacement));
        }
        outCsv.NextRecord();
    }
}
```

### 3. Load Dataset

#### Download Dataset from [Johns Hopkins CSSE](https://github.com/CSSEGISandData/COVID-19/raw/master/csse_covid_19_data)

We'll be using COVID-19 dataset from [Johns Hopkins CSSE](https://github.com/CSSEGISandData/COVID-19/raw/master/csse_covid_19_data). The **csse_covid_19_data directory** has .csv file for each day and we'll be performing analysis on latest file present. Latest file present at the time of last modification of this notebook was **05-27-2020.csv**. If you wish to use a different file, update **DATASET_FILE** constant in Constants cell above.

We'll download file to current directory.


```C#
// Download csv from github
var originalFileName = $"{DATASET_FILE}{FILE_EXTENSION}";
if (!File.Exists(originalFileName))
{
    var remoteFilePath = $"{DATASET_GITHUB_DIRECTORY}/{originalFileName}";
    display(remoteFilePath);
    var contents = new HttpClient()
        .GetStringAsync(remoteFilePath).Result;
        
    File.WriteAllText(originalFileName, contents);
}
```

https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_daily_reports//05-27-2020.csv


#### Load dataset in DataFrame

**Issue**: We can load csv using LoadCsv(..) method of DataFrame. However, there is an [issue](https://github.com/dotnet/corefxlab/issues/2787) of not allowing quotes and separator(comma in this case) in a cell value. 
The dataset, we are using has both of them and LoadCsv fails for it. 
As a workaround, we'll use CSVHelper to read the csv file and replace command separator with underscore, save the file and use it to load in DataFrame LoadCsv(..) method.

<img src=".\assets\invalid-characters.png" alt="Invalid Character" style="zoom: 80%;" />


```C#
// Load and create a copy of dataset file
var newFileName = $"{DATASET_FILE}{NEW_FILE_SUFFIX}{FILE_EXTENSION}";
display(newFileName);
CreateCsvAndReplaceSeparatorInCells(originalFileName, newFileName, SEPARATOR, SEPARATOR_REPLACEMENT);
```

05-27-2020_new.csv

```C#
var covid19Dataframe = DataFrame.LoadCsv(newFileName);
```

### 4. Data Analysis

Data analysis is a critical activity in the field of Data science. It provides ways to uncover the hidden attributes of a dataset which can't be analyzed or predicted by simply looking at the data source. DataFrame makes the analysis simple by providing great API's such as GroupBy, Sort, Filter etc. Jupyter notebook is great tool for this kind of activity which maintains values of variables executed in a cell and providing it to other cells.

##### Finding the range in the records in Dataset

In DataFrame, Columns property allows access to values within a column by specifying column name. we'll use Last_Update column to get the date and sort it to get the start and end date


```C#
// Gets the data range

var dateRangeDataFrame = covid19Dataframe.Columns[LAST_UPDATE].ValueCounts();
var dataRange = dateRangeDataFrame.Columns[VALUES].Sort();
var lastElementIndex = dataRange.Length - 1;

var startDate = DateTime.Parse(dataRange[0].ToString()).ToShortDateString();
var endDate  = DateTime.Parse(dataRange[lastElementIndex].ToString()).ToShortDateString(); // Last Element

display(h4($"The data is between {startDate} and {endDate}"));
```


<h4>The data is between 5/28/2020 and 5/28/2020</h4>


##### Display 10 records

Here we have 12 columns which includes Country, State, Confirmed, Deaths, Recovered and Active cases


```C#
display(covid19Dataframe)
```


<table><thead><th><i>index</i></th><th>FIPS</th><th>Admin2</th><th>Province_State</th><th>Country_Region</th><th>Last_Update</th><th>Lat</th><th>Long_</th><th>Confirmed</th><th>Deaths</th><th>Recovered</th><th>Active</th><th>Combined_Key</th></thead><tbody><tr><td>0</td><td>45001</td><td>Abbeville</td><td>South Carolina</td><td>US</td><td>2020-05-28 02:32:31</td><td>34.223335</td><td>-82.46171</td><td>35</td><td>0</td><td>0</td><td>35</td><td>Abbeville_ South Carolina_ US</td></tr><tr><td>1</td><td>22001</td><td>Acadia</td><td>Louisiana</td><td>US</td><td>2020-05-28 02:32:31</td><td>30.295065</td><td>-92.4142</td><td>397</td><td>22</td><td>0</td><td>375</td><td>Acadia_ Louisiana_ US</td></tr><tr><td>2</td><td>51001</td><td>Accomack</td><td>Virginia</td><td>US</td><td>2020-05-28 02:32:31</td><td>37.76707</td><td>-75.63235</td><td>780</td><td>12</td><td>0</td><td>768</td><td>Accomack_ Virginia_ US</td></tr><tr><td>3</td><td>16001</td><td>Ada</td><td>Idaho</td><td>US</td><td>2020-05-28 02:32:31</td><td>43.452656</td><td>-116.241554</td><td>798</td><td>22</td><td>0</td><td>776</td><td>Ada_ Idaho_ US</td></tr><tr><td>4</td><td>19001</td><td>Adair</td><td>Iowa</td><td>US</td><td>2020-05-28 02:32:31</td><td>41.330757</td><td>-94.47106</td><td>7</td><td>0</td><td>0</td><td>7</td><td>Adair_ Iowa_ US</td></tr><tr><td>5</td><td>21001</td><td>Adair</td><td>Kentucky</td><td>US</td><td>2020-05-28 02:32:31</td><td>37.1046</td><td>-85.281296</td><td>96</td><td>19</td><td>0</td><td>77</td><td>Adair_ Kentucky_ US</td></tr><tr><td>6</td><td>29001</td><td>Adair</td><td>Missouri</td><td>US</td><td>2020-05-28 02:32:31</td><td>40.190586</td><td>-92.600784</td><td>46</td><td>0</td><td>0</td><td>46</td><td>Adair_ Missouri_ US</td></tr><tr><td>7</td><td>40001</td><td>Adair</td><td>Oklahoma</td><td>US</td><td>2020-05-28 02:32:31</td><td>35.88494</td><td>-94.65859</td><td>82</td><td>3</td><td>0</td><td>79</td><td>Adair_ Oklahoma_ US</td></tr><tr><td>8</td><td>8001</td><td>Adams</td><td>Colorado</td><td>US</td><td>2020-05-28 02:32:31</td><td>39.87432</td><td>-104.33626</td><td>3006</td><td>118</td><td>0</td><td>2888</td><td>Adams_ Colorado_ US</td></tr><tr><td>9</td><td>16003</td><td>Adams</td><td>Idaho</td><td>US</td><td>2020-05-28 02:32:31</td><td>44.893337</td><td>-116.45452</td><td>3</td><td>0</td><td>0</td><td>3</td><td>Adams_ Idaho_ US</td></tr></tbody></table>


##### Display Top 5 records


```C#
covid19Dataframe.Head(5)
```




<table><thead><th><i>index</i></th><th>FIPS</th><th>Admin2</th><th>Province_State</th><th>Country_Region</th><th>Last_Update</th><th>Lat</th><th>Long_</th><th>Confirmed</th><th>Deaths</th><th>Recovered</th><th>Active</th><th>Combined_Key</th></thead><tbody><tr><td>0</td><td>45001</td><td>Abbeville</td><td>South Carolina</td><td>US</td><td>2020-05-28 02:32:31</td><td>34.223335</td><td>-82.46171</td><td>35</td><td>0</td><td>0</td><td>35</td><td>Abbeville_ South Carolina_ US</td></tr><tr><td>1</td><td>22001</td><td>Acadia</td><td>Louisiana</td><td>US</td><td>2020-05-28 02:32:31</td><td>30.295065</td><td>-92.4142</td><td>397</td><td>22</td><td>0</td><td>375</td><td>Acadia_ Louisiana_ US</td></tr><tr><td>2</td><td>51001</td><td>Accomack</td><td>Virginia</td><td>US</td><td>2020-05-28 02:32:31</td><td>37.76707</td><td>-75.63235</td><td>780</td><td>12</td><td>0</td><td>768</td><td>Accomack_ Virginia_ US</td></tr><tr><td>3</td><td>16001</td><td>Ada</td><td>Idaho</td><td>US</td><td>2020-05-28 02:32:31</td><td>43.452656</td><td>-116.241554</td><td>798</td><td>22</td><td>0</td><td>776</td><td>Ada_ Idaho_ US</td></tr><tr><td>4</td><td>19001</td><td>Adair</td><td>Iowa</td><td>US</td><td>2020-05-28 02:32:31</td><td>41.330757</td><td>-94.47106</td><td>7</td><td>0</td><td>0</td><td>7</td><td>Adair_ Iowa_ US</td></tr></tbody></table>



##### Display Random 6 records


```C#
covid19Dataframe.Sample(6)
```




<table><thead><th><i>index</i></th><th>FIPS</th><th>Admin2</th><th>Province_State</th><th>Country_Region</th><th>Last_Update</th><th>Lat</th><th>Long_</th><th>Confirmed</th><th>Deaths</th><th>Recovered</th><th>Active</th><th>Combined_Key</th></thead><tbody><tr><td>0</td><td>18025</td><td>Crawford</td><td>Indiana</td><td>US</td><td>2020-05-28 02:32:31</td><td>38.288143</td><td>-86.44519</td><td>23</td><td>0</td><td>0</td><td>23</td><td>Crawford_ Indiana_ US</td></tr><tr><td>1</td><td>51065</td><td>Fluvanna</td><td>Virginia</td><td>US</td><td>2020-05-28 02:32:31</td><td>37.84158</td><td>-78.27715</td><td>87</td><td>6</td><td>0</td><td>81</td><td>Fluvanna_ Virginia_ US</td></tr><tr><td>2</td><td>13241</td><td>Rabun</td><td>Georgia</td><td>US</td><td>2020-05-28 02:32:31</td><td>34.883896</td><td>-83.403046</td><td>17</td><td>1</td><td>0</td><td>16</td><td>Rabun_ Georgia_ US</td></tr><tr><td>3</td><td>12003</td><td>Baker</td><td>Florida</td><td>US</td><td>2020-05-28 02:32:31</td><td>30.3306</td><td>-82.284676</td><td>29</td><td>3</td><td>0</td><td>26</td><td>Baker_ Florida_ US</td></tr><tr><td>4</td><td>21133</td><td>Letcher</td><td>Kentucky</td><td>US</td><td>2020-05-28 02:32:31</td><td>37.123066</td><td>-82.85346</td><td>4</td><td>0</td><td>0</td><td>4</td><td>Letcher_ Kentucky_ US</td></tr><tr><td>5</td><td>12077</td><td>Liberty</td><td>Florida</td><td>US</td><td>2020-05-28 02:32:31</td><td>30.23766</td><td>-84.88293</td><td>209</td><td>0</td><td>0</td><td>209</td><td>Liberty_ Florida_ US</td></tr></tbody></table>



##### Display Dataset Statistics such as Total, Max, Min, Mean of items in a column


```C#
covid19Dataframe.Description()
```




<table><thead><th><i>index</i></th><th>Description</th><th>FIPS</th><th>Lat</th><th>Long_</th><th>Confirmed</th><th>Deaths</th><th>Recovered</th><th>Active</th></thead><tbody><tr><td>0</td><td>Length (excluding null values)</td><td>3009</td><td>3346</td><td>3346</td><td>3414</td><td>3414</td><td>3414</td><td>3414</td></tr><tr><td>1</td><td>Max</td><td>99999</td><td>71.7069</td><td>178.065</td><td>370680</td><td>37460</td><td>391508</td><td>229780</td></tr><tr><td>2</td><td>Min</td><td>0</td><td>-52.368</td><td>-164.03539</td><td>0</td><td>0</td><td>0</td><td>-364117</td></tr><tr><td>3</td><td>Mean</td><td>27622.38</td><td>35.3851</td><td>-78.927086</td><td>1667.1909</td><td>104.16784</td><td>688.3679</td><td>882.6784</td></tr></tbody></table>



##### Display Dataset type information for each column


```C#
covid19Dataframe.Info()
```




<table><thead><th><i>index</i></th><th>Info</th><th>FIPS</th><th>Admin2</th><th>Province_State</th><th>Country_Region</th><th>Last_Update</th><th>Lat</th><th>Long_</th><th>Confirmed</th><th>Deaths</th><th>Recovered</th><th>Active</th><th>Combined_Key</th></thead><tbody><tr><td>0</td><td>DataType</td><td>System.Single</td><td>System.String</td><td>System.String</td><td>System.String</td><td>System.String</td><td>System.Single</td><td>System.Single</td><td>System.Single</td><td>System.Single</td><td>System.Single</td><td>System.Single</td><td>System.String</td></tr><tr><td>1</td><td>Length (excluding null values)</td><td>3009</td><td>3414</td><td>3414</td><td>3414</td><td>3414</td><td>3346</td><td>3346</td><td>3414</td><td>3414</td><td>3414</td><td>3414</td><td>3414</td></tr></tbody></table>



### 5. Data Cleaning

Data Cleaning is another important activity in which remove the irrelevant data present in our dataset. This irrelevant data can be due missing values, invalid values or an outlier. The columns with less significance is removed for better analysis and prediction of our data. In order to keep this notebook simple, we'll use one of the techniques to remove invalid data. In this we are going to remove invalid Active cases such as the ones having negative values. The other techniques we can apply on data  could be DropNull to remove rows with null values, FillNull to fill null values with other such as mean, average. We can transform DataFrame and remove some of the unnecessary columns.

#### Remove invalid Active cases


```C#
covid19Dataframe.Description()
```




<table><thead><th><i>index</i></th><th>Description</th><th>FIPS</th><th>Lat</th><th>Long_</th><th>Confirmed</th><th>Deaths</th><th>Recovered</th><th>Active</th></thead><tbody><tr><td>0</td><td>Length (excluding null values)</td><td>3009</td><td>3346</td><td>3346</td><td>3414</td><td>3414</td><td>3414</td><td>3414</td></tr><tr><td>1</td><td>Max</td><td>99999</td><td>71.7069</td><td>178.065</td><td>370680</td><td>37460</td><td>391508</td><td>229780</td></tr><tr><td>2</td><td>Min</td><td>0</td><td>-52.368</td><td>-164.03539</td><td>0</td><td>0</td><td>0</td><td>-364117</td></tr><tr><td>3</td><td>Mean</td><td>27622.38</td><td>35.3851</td><td>-78.927086</td><td>1667.1909</td><td>104.16784</td><td>688.3679</td><td>882.6784</td></tr></tbody></table>



From the above description table, we could see negative value for Active cases which seems to be incorrect as number of active cases is cases is calculated by the below formula

**Active = Confirmed - Deaths - Recovered**

In order to check for invalid active cases, we'll use DataFrame **Filter** to retrieve active column values whose value is less than 0.0


```C#
// Filter : Gets active records with negative calues

PrimitiveDataFrameColumn<bool> invalidActiveFilter = covid19Dataframe.Columns[ACTIVE].ElementwiseLessThan(0.0);
var invalidActiveDataFrame = covid19Dataframe.Filter(invalidActiveFilter);
display(invalidActiveDataFrame)
```


<table><thead><th><i>index</i></th><th>FIPS</th><th>Admin2</th><th>Province_State</th><th>Country_Region</th><th>Last_Update</th><th>Lat</th><th>Long_</th><th>Confirmed</th><th>Deaths</th><th>Recovered</th><th>Active</th><th>Combined_Key</th></thead><tbody><tr><td>0</td><td>90004</td><td>Unassigned</td><td>Arizona</td><td>US</td><td>2020-05-28 02:32:31</td><td>&lt;null&gt;</td><td>&lt;null&gt;</td><td>0</td><td>2</td><td>0</td><td>-2</td><td>Unassigned_ Arizona_ US</td></tr><tr><td>1</td><td>90018</td><td>Unassigned</td><td>Indiana</td><td>US</td><td>2020-05-28 02:32:31</td><td>&lt;null&gt;</td><td>&lt;null&gt;</td><td>0</td><td>159</td><td>0</td><td>-159</td><td>Unassigned_ Indiana_ US</td></tr><tr><td>2</td><td>90022</td><td>Unassigned</td><td>Louisiana</td><td>US</td><td>2020-05-28 02:32:31</td><td>&lt;null&gt;</td><td>&lt;null&gt;</td><td>84</td><td>105</td><td>0</td><td>-21</td><td>Unassigned_ Louisiana_ US</td></tr><tr><td>3</td><td>90024</td><td>Unassigned</td><td>Maryland</td><td>US</td><td>2020-05-28 02:32:31</td><td>&lt;null&gt;</td><td>&lt;null&gt;</td><td>0</td><td>67</td><td>0</td><td>-67</td><td>Unassigned_ Maryland_ US</td></tr><tr><td>4</td><td>90032</td><td>Unassigned</td><td>Nevada</td><td>US</td><td>2020-05-28 02:32:31</td><td>&lt;null&gt;</td><td>&lt;null&gt;</td><td>0</td><td>6</td><td>0</td><td>-6</td><td>Unassigned_ Nevada_ US</td></tr><tr><td>5</td><td>90033</td><td>Unassigned</td><td>New Hampshire</td><td>US</td><td>2020-05-28 02:32:31</td><td>&lt;null&gt;</td><td>&lt;null&gt;</td><td>10</td><td>51</td><td>0</td><td>-41</td><td>Unassigned_ New Hampshire_ US</td></tr><tr><td>6</td><td>90038</td><td>Unassigned</td><td>North Dakota</td><td>US</td><td>2020-05-28 02:32:31</td><td>&lt;null&gt;</td><td>&lt;null&gt;</td><td>0</td><td>8</td><td>0</td><td>-8</td><td>Unassigned_ North Dakota_ US</td></tr><tr><td>7</td><td>90056</td><td>Unassigned</td><td>Wyoming</td><td>US</td><td>2020-05-28 02:32:31</td><td>&lt;null&gt;</td><td>&lt;null&gt;</td><td>0</td><td>13</td><td>0</td><td>-13</td><td>Unassigned_ Wyoming_ US</td></tr><tr><td>8</td><td>&lt;null&gt;</td><td></td><td>C. Valenciana</td><td>Spain</td><td>2020-05-28 02:32:31</td><td>39.484</td><td>-0.7533</td><td>11089</td><td>1332</td><td>9970</td><td>-213</td><td>C. Valenciana_ Spain</td></tr><tr><td>9</td><td>&lt;null&gt;</td><td></td><td>Cantabria</td><td>Spain</td><td>2020-05-28 02:32:31</td><td>43.1828</td><td>-3.9878</td><td>2283</td><td>202</td><td>2287</td><td>-206</td><td>Cantabria_ Spain</td></tr></tbody></table>


If we take any record(index 5) and apply above formula to calculate 

**Active(-13) = Confirmed(10) - Deaths(51) - Recovered(0)**

We could see invalid active cases.

In order to remove it, we'll apply a **Filter** to DataFrame to get active values greater than or equal to 0.0. 


```C#
// Remove invalid active cases by applying filter

PrimitiveDataFrameColumn<bool> activeFilter = covid19Dataframe.Columns[ACTIVE].ElementwiseGreaterThanOrEqual(0.0);
covid19Dataframe = covid19Dataframe.Filter(activeFilter);
display(covid19Dataframe.Description());
```


<table><thead><th><i>index</i></th><th>Description</th><th>FIPS</th><th>Lat</th><th>Long_</th><th>Confirmed</th><th>Deaths</th><th>Recovered</th><th>Active</th></thead><tbody><tr><td>0</td><td>Length (excluding null values)</td><td>3001</td><td>3338</td><td>3338</td><td>3395</td><td>3395</td><td>3395</td><td>3395</td></tr><tr><td>1</td><td>Max</td><td>99999</td><td>71.7069</td><td>178.065</td><td>370680</td><td>37460</td><td>142208</td><td>229780</td></tr><tr><td>2</td><td>Min</td><td>0</td><td>-52.368</td><td>-164.03539</td><td>0</td><td>0</td><td>0</td><td>0</td></tr><tr><td>3</td><td>Mean</td><td>27564.82</td><td>35.48979</td><td>-79.35968</td><td>1664.4899</td><td>103.38468</td><td>505.34668</td><td>1055.7584</td></tr></tbody></table>


**As seen above, negative active cases have been removed**

### 6. Visualization

Visualization of data helps business owners make better decisions. The DataFrame maintains data in a tabular format. In order to prepare data for different plots, I have used DataFrame features such as Sum, GroupBy, OrderBy, OrderByDescending etc. 

For visualization, I have used open source library called as [XPlot.Plotly](https://fslab.org/XPlot/plotly.html). Different plots have been used such as Bar, Pie and Line/Scatter Graph. 

#### Global

##### Collect Data


```C#
//  Gets the collection of confirmed, deaths and recovered

var confirmed = covid19Dataframe.Columns[CONFIRMED];
var deaths = covid19Dataframe.Columns[DEATHS];
var recovered = covid19Dataframe.Columns[RECOVERED];

// Gets the sum of collection by using Sum method of DataFrame
var totalConfirmed = Convert.ToDouble(confirmed.Sum());
var totalDeaths = Convert.ToDouble(deaths.Sum());
var totaRecovered = Convert.ToDouble(recovered.Sum());
```

##### Confirmed Vs Deaths Vs Recovered cases


```C#
display(Chart.Plot(
    new Graph.Pie()
    {
        values = new double[]{totalConfirmed, totalDeaths, totaRecovered},
        labels = new string[] {CONFIRMED, DEATHS, RECOVERED}
    }
));
```

<img src=".\assets\pie-confirmed-recovered-deaths.png" alt="Confirmed Vs Deaths Vs Recovered" style="zoom:80%;" />


##### Top 5 Countries with Confirmed cases

In order to get top 5 countries data, I have used DataFrame's GroupBy, Sum, OrderByDescending methods


```C#
![top-5-confirmed-countries-global](C:\Users\lenovo\Downloads\covid-19-master\covid-19-master\part-1\assets\top-5-confirmed-countries-global.png)// The data for top 5 countries is not present in the csv file.
// In order to get that, first DataFrame's GROUPBY is used aginst the country.
// Then it was aggregated using SUM on Confirmed column.
// In the last, ORDERBYDESCENDING is used to get the top five countries.

var countryConfirmedGroup = covid19Dataframe.GroupBy(COUNTRY).Sum(CONFIRMED).OrderByDescending(CONFIRMED);
var topCountriesColumn = countryConfirmedGroup.Columns[COUNTRY];
var topConfirmedCasesByCountry = countryConfirmedGroup.Columns[CONFIRMED];

HashSet<string> countries = new HashSet<string>(TOP_COUNT);
HashSet<long> confirmedCases = new HashSet<long>(TOP_COUNT);
for(int index = 0; index < TOP_COUNT; index++)
{
    countries.Add(topCountriesColumn[index].ToString());
    confirmedCases.Add(Convert.ToInt64(topConfirmedCasesByCountry[index]));
}
```


```C#
var title = "Top 5 Countries : Confirmed";
var series1 = new Graph.Bar{
        x = countries.ToArray(),
        y = confirmedCases.ToArray()
    };

var chart = Chart.Plot(new []{series1});
chart.WithTitle(title);
display(chart);
```

<img src="assets\top-5-confirmed-countries-global.png" alt="Top-5 Countries : Confirmed" style="zoom:80%;" />



##### Top 5 Countries with Deaths


```C#
// Get the data
var countryDeathsGroup = covid19Dataframe.GroupBy(COUNTRY).Sum(DEATHS).OrderByDescending(DEATHS);
var topCountriesColumn = countryDeathsGroup.Columns[COUNTRY];
var topDeathCasesByCountry = countryDeathsGroup.Columns[DEATHS];

HashSet<string> countries = new HashSet<string>(TOP_COUNT);
HashSet<long> deathCases = new HashSet<long>(TOP_COUNT);
for(int index = 0; index < TOP_COUNT; index++)
{
    countries.Add(topCountriesColumn[index].ToString());
    deathCases.Add(Convert.ToInt64(topDeathCasesByCountry[index]));
}
```


```C#
var title = "Top 5 Countries : Deaths";
var series1 = new Graph.Bar{
        x = countries.ToArray(),
        y = deathCases.ToArray()
    };

var chart = Chart.Plot(new []{series1});
chart.WithTitle(title);
display(chart);
```

<img src="assets\top-5-deaths-countries-global.png" alt="Top-5 Countries: Deaths" style="zoom:80%;" />




##### Top 5 Countries with Recovered cases


```C#
// Get the data
var countryRecoveredGroup = covid19Dataframe.GroupBy(COUNTRY).Sum(RECOVERED).OrderByDescending(RECOVERED);
var topCountriesColumn = countryRecoveredGroup.Columns[COUNTRY];
var topRecoveredCasesByCountry = countryRecoveredGroup.Columns[RECOVERED];

HashSet<string> countries = new HashSet<string>(TOP_COUNT);
HashSet<long> recoveredCases = new HashSet<long>(TOP_COUNT);
for(int index = 0; index < TOP_COUNT; index++)
{
    countries.Add(topCountriesColumn[index].ToString());
    recoveredCases.Add(Convert.ToInt64(topRecoveredCasesByCountry[index]));
}
```


```C#
var title = "Top 5 Countries : Recovered";
var series1 = new Graph.Bar{
        x = countries.ToArray(),
        y = recoveredCases.ToArray()
    };

var chart = Chart.Plot(new []{series1});
chart.WithTitle(title);
display(chart);
```

<img src="assets\top-5-recovered-countries-global.png" alt="Top-5 Countries : Recovered" style="zoom:80%;" />


#### India

##### Confirmed Vs Deaths Vs Recovered cases

Filtering on Country column with INDIA as value


```C#
// Filtering on Country column with INDIA as value

PrimitiveDataFrameColumn<bool> indiaFilter = covid19Dataframe.Columns[COUNTRY].ElementwiseEquals(INDIA);
var indiaDataFrame = covid19Dataframe.Filter(indiaFilter);
            
var indiaConfirmed = indiaDataFrame.Columns[CONFIRMED];
var indiaDeaths = indiaDataFrame.Columns[DEATHS];
var indiaRecovered = indiaDataFrame.Columns[RECOVERED];

var indiaTotalConfirmed = Convert.ToDouble(indiaConfirmed.Sum());
var indiaTotalDeaths = Convert.ToDouble(indiaDeaths.Sum());
var indiaTotaRecovered = Convert.ToDouble(indiaRecovered.Sum());
```


```C#
display(Chart.Plot(
    new Graph.Pie()
    {
        values = new double[]{indiaTotalConfirmed, indiaTotalDeaths, indiaTotaRecovered},
        labels = new string[] {CONFIRMED, DEATHS, RECOVERED}
    }
));
```

<img src="assets\pie-confirmed-recovered-deaths-india.png" alt="India - Confirmed Vs Recovered Vs Deaths" style="zoom:80%;" />



## Conclusion

I hope you have enjoyed reading the notebook, and might have got some idea on the powerful features of DataFrame in .Net. Data science capabilities are emerging fast in the .Net ecosystem which abstracts lot of complexity present in the field. The focus of this notebook is data analysis and there is nothing present from a Machine Learning perspective such as making a prediction. In [Part-2](../part-2), I have done time series analysis and predictions using ML.Net 

Feedback/Suggestion are welcome. Please reach out to me through below channels

Source code : https://github.com/praveenraghuvanshi1512/covid-19

**Contact**

**LinkedIn :** https://in.linkedin.com/in/praveenraghuvanshi  
**Github   :** https://github.com/praveenraghuvanshi1512  
**Twitter  :** @praveenraghuvan



## References
- [Using ML.NET in Jupyter notebooks](https://devblogs.microsoft.com/cesardelatorre/using-ml-net-in-jupyter-notebooks/)
- [An Introduction to DataFrame](https://devblogs.microsoft.com/dotnet/an-introduction-to-dataframe/)
- [DataFrame - Sample](https://github.com/dotnet/interactive/blob/master/NotebookExamples/csharp/Samples/HousingML.ipynb)
- [Getting started with ML.NET in Jupyter Notebooks](https://xamlbrewer.wordpress.com/2020/02/20/getting-started-with-ml-net-in-jupyter-notebooks/)
- [Tips and tricks for C# Jupyter notebook](https://ewinnington.github.io/posts/jupyter-tips-csharp)
- [Jupyter notebooks with C# and R running](https://github.com/ewinnington/noteb)
- [Data analysis using F# and Jupyter notebook — Samuele Resca](https://medium.com/@samueleresca/data-analysis-using-f-and-jupyter-notebook-samuele-resca-66a229e25306)
- [Exploring the C# Dataframe API](https://www.youtube.com/watch?v=FI3VxXClJ7Y) by Jon Wood
- [Coronavirus-COVID-19-Visualization-Prediction](https://www.kaggle.com/therealcyberlord/coronavirus-covid-19-visualization-prediction)
- [Data Modelling & Analysing Coronavirus (COVID19) Spread using Data Science & Data Analytics in Python Code](https://in.springboard.com/blog/data-modelling-covid/)

#  ******************** Be Safe **********************
