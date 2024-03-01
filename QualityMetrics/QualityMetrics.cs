

using ServiceLayer.Interfaces;
using System;
using System.Linq;

namespace QualityMetrics
{
    class QualityMetrics : IQualityMetrics
    {
        private readonly IExcelUtilities _excelServices;
        private readonly ITfsServices _tfsServices;
        private readonly IEmailUtilities _emailUtilities;
        public QualityMetrics(ITfsServices tfsServices, IExcelUtilities excelServices, IEmailUtilities emailUtilities)
        {
            _excelServices = excelServices;
            _tfsServices = tfsServices;
            _emailUtilities = emailUtilities;
        }
        public void GatherQualityMetrics()
        {
            try
            {
                Console.WriteLine("Initializing ...");
                _tfsServices.Initialize();

                bool quit;
                do
                {
                    Console.WriteLine();
                    WriteColoredLine("Enter the Sprint Name in the format SprintXX_X", ConsoleColor.DarkBlue, ConsoleColor.White);
                    string sprint = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(sprint))
                    {
                        Console.WriteLine();
                        Console.WriteLine("Press any key to quit ...");
                        return;
                    }

                    var iterations = _tfsServices.GetIterationsForASprint(sprint);
                    var codeReviewDetails = _tfsServices.GetCodeReviewDetails(iterations.First().Id);
                    codeReviewDetails.codeReviewFromGitPullRequests = _tfsServices.GetGitPullRequestsDetails(iterations.First().Id);
                    var teamMembers = _tfsServices.GetTeamMembersOfTheSprint(iterations);
                    string filePath = _excelServices.CreateExcelReport(codeReviewDetails, teamMembers, sprint);
                    
                    _emailUtilities.SendEmail(new ServiceLayer.Dtos.SendEmailRequest 
                    {
                        sprintName=iterations.First().Name,
                        applicationName="IPW",
                        filePath=filePath
                    });
                    Console.WriteLine();
                    WriteColoredLine("- DONE -", ConsoleColor.Black, ConsoleColor.Green);

                    Console.WriteLine();
                    WriteColoredLine("Type Q to quit or any other character to generate another report", ConsoleColor.DarkBlue, ConsoleColor.White);
                    quit = Char.ToLower(Console.ReadKey().KeyChar) == 'q';
                } while (!quit);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Something Went Wrong "+ex.Message);
            }
            Console.WriteLine();
            Console.WriteLine("Press any key to quit ...");
        }
        private void WriteColoredLine(string value, ConsoleColor backgroundColor, ConsoleColor foregroundColor)
        {
            Console.BackgroundColor = backgroundColor;
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine(value);
            Console.ResetColor();
        }
    }
}
