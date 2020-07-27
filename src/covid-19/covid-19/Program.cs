using System;

namespace covid_19
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("**** Welcome to world of data science and machine learning using Microsoft .Net!!! ****");

            var exploratoryDataAnalysis = new ExploratoryDataAnalysis();
            exploratoryDataAnalysis.Run();

            Console.WriteLine("******* END *********");
            Console.ReadLine();
        }
    }
}
