
using ClosedXML.Excel;
using ServiceLayer.Dtos;
using ServiceLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;

namespace ServiceLayer.Implementations
{
    public class ExcelUtilities:IExcelUtilities
    {
        private readonly string FileDropLocation = ConfigurationManager.AppSettings.Get("FileDropLocation").ToString();

        private static DataTable GetCodeReviewCompletedChangesetsData(List<CodeReviewCompletedChangeset> codeReviewCompletedChangesets)
        {
            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("ChangeSet", typeof(string));
            dataTable.Columns.Add("Owner", typeof(string));
            dataTable.Columns.Add("Reviewer", typeof(string));
            dataTable.Columns.Add("CheckedInDate", typeof(string));
            dataTable.Columns.Add("ReviewedDate", typeof(string));
            dataTable.Columns.Add("Title", typeof(string));
            dataTable.Columns.Add("Comments", typeof(string));
            dataTable.Columns.Add("Status", typeof(string));

            foreach (var changeset in codeReviewCompletedChangesets)
            {
                DataRow dataRow = dataTable.NewRow();
                dataRow["ChangeSet"] = changeset.Changeset;
                dataRow["Owner"] = changeset.Owner;
                dataRow["Reviewer"] = string.Join("\n", changeset.Reviewers);
                dataRow["CheckedInDate"] = changeset.CheckedInDate.Date.ToString("d");
                dataRow["ReviewedDate"] = changeset.ReviewedDate.Value.ToString("d");
                dataRow["Title"] = changeset.Title;

                var commentsString = new StringBuilder();
                var commentsbyauthors = changeset.CodeReviewComments.OrderBy(i => i.Author).GroupBy(i => i.Author);
                foreach (var item in commentsbyauthors)
                {
                    int counter = 1;
                    commentsString.AppendLine(item.Key);

                    foreach (var item1 in item.Distinct())
                    {
                        commentsString.AppendLine(counter + " " + item1.Comments);
                        counter++;
                    }

                    commentsString.AppendLine();
                }
                dataRow["Comments"] = commentsString.ToString();
                dataRow["Status"] = changeset.Status;

                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        private static DataTable GetCodeReviewGitPullRequestsData(List<CodeReviewGitPullRequest> codeReviewPrData)
        {
            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("Pull Request", typeof(string));
            dataTable.Columns.Add("Owner", typeof(string));
            dataTable.Columns.Add("Reviewer", typeof(string));
            dataTable.Columns.Add("ClosedDate", typeof(string));
            dataTable.Columns.Add("ReviewedDate", typeof(string));
            dataTable.Columns.Add("Title", typeof(string));
            dataTable.Columns.Add("Comments", typeof(string));
            dataTable.Columns.Add("Status", typeof(string));
            dataTable.Columns.Add("Repository", typeof(string));
            dataTable.Columns.Add("TargetBranch", typeof(string));

            foreach (var pullRequest in codeReviewPrData)
            {
                DataRow dataRow = dataTable.NewRow();
                dataRow["Pull Request"] = pullRequest.PullRequest;
                dataRow["Owner"] = pullRequest.Owner;
                dataRow["Reviewer"] = string.Join("\n", pullRequest.Reviewers.Select(r => r.Name + (r.HasAccepted ? GetReviewIcon(r) : "")));
                dataRow["ClosedDate"] = pullRequest.ClosedDate.Date.ToString("d");
                dataRow["ReviewedDate"] = pullRequest.ReviewedDate.Value.ToString("d");
                dataRow["Title"] = pullRequest.Title;

                var commentsString = new StringBuilder();
                var commentsbyauthors = pullRequest.CodeReviewComments.OrderBy(i => i.Author).GroupBy(i => i.Author);
                foreach (var item in commentsbyauthors)
                {
                    int counter = 1;
                    commentsString.AppendLine(item.Key);

                    foreach (var item1 in item.Distinct())
                    {
                        commentsString.AppendLine(counter + " " + item1.Comments);
                        counter++;
                    }

                    commentsString.AppendLine();
                }
                dataRow["Comments"] = commentsString.ToString();
                dataRow["Status"] = pullRequest.Status;
                dataRow["Repository"] = pullRequest.Repository;
                dataRow["TargetBranch"] = pullRequest.TargetBranch;

                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        private static string GetReviewIcon(CodeReviewReviewer reviewer)
        {
            return reviewer.ReviewStatus == CodeReviewReviewStatus.Approved ? " ☑"
                : reviewer.ReviewStatus == CodeReviewReviewStatus.ApprovedWithSuggestions ? " ☑🖹"
                : reviewer.ReviewStatus == CodeReviewReviewStatus.Rejected ? " ⚠"
                : "";
        }

        private static DataTable GetCodeReviewNotDoneChangesets(List<CodeReviewNotDoneChangesets> codeReviewNotDoneChangesets)
        {

            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("ChangeSet", typeof(string));
            dataTable.Columns.Add("Owner", typeof(string));
            dataTable.Columns.Add("Title", typeof(string));
            dataTable.Columns.Add("CheckedInDate", typeof(string));
            dataTable.Columns.Add("AssignedTo", typeof(string));

            foreach (var item in codeReviewNotDoneChangesets)
            {
                DataRow dataRow = dataTable.NewRow();
                dataRow["ChangeSet"] = item.Changeset;
                dataRow["Owner"] = item.Owner;
                dataRow["Title"] = item.Title;
                dataRow["CheckedInDate"] = item.CheckedInDate.Date.ToString("d");
                dataRow["AssignedTo"] = string.Join("\n", item.Reviewers);

                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        private static DataTable GetNoCodeReviewRequestChangesets(List<NoCodeReviewRequestChangesets> noCodeReviewRequestChangesets)
        {
            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("ChangeSet", typeof(string));
            dataTable.Columns.Add("Owner", typeof(string));
            dataTable.Columns.Add("Title", typeof(string));
            dataTable.Columns.Add("CheckedInDate", typeof(string));

            foreach (var item in noCodeReviewRequestChangesets)
            {
                DataRow dataRow = dataTable.NewRow();
                dataRow["ChangeSet"] = item.Changeset;
                dataRow["Owner"] = item.Owner;
                dataRow["Title"] = item.Title;
                dataRow["CheckedInDate"] = item.CheckedInDate.Date.ToString("d");
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;

        }
        private static List<string> FindChangsetOwnersWhoAreNotPartOfAnyTeam(List<GetTeamDetails> teamDetails, CodeReviewDetails codeReviewDetails)
        {
            var teamMembers = new List<string>();
            teamDetails.ForEach(team => teamMembers.AddRange(team.Members));
            var changesetOwners = new List<string>();
            changesetOwners.AddRange(codeReviewDetails.codeReviewCompletedChangesets.Select(i => i.Owner).Distinct());
            changesetOwners.AddRange(codeReviewDetails.codeReviewNotDoneChangesets.Select(i => i.Owner).Distinct());
            changesetOwners.AddRange(codeReviewDetails.noCodeReviewRequestChangesets.Select(i => i.Owner).Distinct());

            return changesetOwners.Except(teamMembers).ToList();
        }

        private static int CountChangsesetsTeamWise(List<string> teamMembers, CodeReviewDetails codeReviewDetails)
        {
            int count = 0;
            foreach (var teamMember in teamMembers)
            {
                count += codeReviewDetails.codeReviewCompletedChangesets.Count(changest => changest.Owner == teamMember);
                count += codeReviewDetails.codeReviewNotDoneChangesets.Count(changest => changest.Owner == teamMember);
                count += codeReviewDetails.noCodeReviewRequestChangesets.Count(changest => changest.Owner == teamMember);
                count += codeReviewDetails.codeReviewFromGitPullRequests.Count(changest => changest.Owner == teamMember && changest.CodeReviewComments.Any());
                count += codeReviewDetails.codeReviewFromGitPullRequests.Count(changest => changest.Owner == teamMember && !changest.Reviewers.Any(r => r.HasAccepted));
            }
            return count;
        }

        private static DataTable GetTeamDetails(List<GetTeamDetails> teamDetails, CodeReviewDetails codeReviewDetails)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Team", typeof(string));
            dataTable.Columns.Add("Members");
            dataTable.Columns.Add("Changesets having Comments", typeof(int));
            dataTable.Columns.Add("Changesets without completing code review", typeof(int));
            dataTable.Columns.Add("Changesets not requested for code review", typeof(int));
            dataTable.Columns.Add("Pull Requests having comments", typeof(int));
            dataTable.Columns.Add("Pull Requests without code review done", typeof(int));
            dataTable.Columns.Add("Total Number Of Changesets/PR", typeof(int));

            dataTable.Rows.Add(dataTable.NewRow());

            foreach (var team in teamDetails)
            {

                List<DataRow> Members = new List<DataRow>();

                DataRow teamRow = dataTable.NewRow();
                teamRow["Team"] = team.TeamName;

                if (team.TeamName == "IPW Support Squad")
                {
                    team.Members.AddRange(FindChangsetOwnersWhoAreNotPartOfAnyTeam(teamDetails, codeReviewDetails));
                }
                foreach (var teamMember in team.Members)
                {
                    if (codeReviewDetails.codeReviewCompletedChangesets.Any(c => c.Owner == teamMember) || codeReviewDetails.codeReviewNotDoneChangesets.Any(c => c.Owner == teamMember) || codeReviewDetails.noCodeReviewRequestChangesets.Any(c => c.Owner == teamMember) || codeReviewDetails.codeReviewFromGitPullRequests.Any(c => c.Owner == teamMember))
                    {
                        DataRow memberRow = dataTable.NewRow();
                        memberRow["Members"] = teamMember;
                        memberRow["Changesets having Comments"] = codeReviewDetails.codeReviewCompletedChangesets.Count(changest => changest.Owner == teamMember);
                        memberRow["Changesets without completing code review"] = codeReviewDetails.codeReviewNotDoneChangesets.Count(changest => changest.Owner == teamMember);
                        memberRow["Changesets not requested for code review"] = codeReviewDetails.noCodeReviewRequestChangesets.Count(changest => changest.Owner == teamMember);
                        memberRow["Pull Requests having comments"] = codeReviewDetails.codeReviewFromGitPullRequests.Count(changest => changest.Owner == teamMember && changest.CodeReviewComments.Any());
                        memberRow["Pull Requests without code review done"] = codeReviewDetails.codeReviewFromGitPullRequests.Count(changest => changest.Owner == teamMember && !changest.Reviewers.Any(r => r.HasAccepted));
                        Members.Add(memberRow);
                    }
                }

                teamRow[7] = CountChangsesetsTeamWise(team.Members, codeReviewDetails);
                dataTable.Rows.Add(teamRow);
                Members.ForEach(m => dataTable.Rows.Add(m));
                dataTable.Rows.Add(dataTable.NewRow());
            }

            //Total Calculation
            DataRow totalRow = dataTable.NewRow();
            totalRow["Team"] = "Total";
            var total1 = codeReviewDetails.codeReviewCompletedChangesets.Count;
            totalRow["Changesets having Comments"] = total1;
            var total2 = codeReviewDetails.codeReviewNotDoneChangesets.Count;
            totalRow["Changesets without completing code review"] = total2;
            var total3 = codeReviewDetails.noCodeReviewRequestChangesets.Count;
            totalRow["Changesets not requested for code review"] = total3;
            var total4 = codeReviewDetails.codeReviewFromGitPullRequests.Count(changest => changest.CodeReviewComments.Any());
            totalRow["Pull Requests having comments"] = total4;
            var total5 = codeReviewDetails.codeReviewFromGitPullRequests.Count(changest => !changest.Reviewers.Any(r => r.HasAccepted));
            totalRow["Pull Requests without code review done"] = total5;
            totalRow["Total Number Of Changesets/PR"] = total1 + total2 + total3 + total4 + total5;
            dataTable.Rows.Add(totalRow);
            return dataTable;
        }

        public string CreateExcelReport(CodeReviewDetails codeReviewDetails, List<GetTeamDetails> teamMembers, string sprint)
        {
            Console.WriteLine();
            Console.WriteLine("---- Generating Excel Report ----");
            var workBook = new XLWorkbook();

            DataTable CodeReviewComments = GetCodeReviewCompletedChangesetsData(codeReviewDetails.codeReviewCompletedChangesets);
            workBook.Worksheets.Add(CodeReviewComments, "CodeReviewComments");
            DataTable CodeReviewNotDoneChangesets = GetCodeReviewNotDoneChangesets(codeReviewDetails.codeReviewNotDoneChangesets);
            workBook.Worksheets.Add(CodeReviewNotDoneChangesets, "RequestedButZeroComments");
            DataTable CodeReviewNotRequested = GetNoCodeReviewRequestChangesets(codeReviewDetails.noCodeReviewRequestChangesets);
            workBook.Worksheets.Add(CodeReviewNotRequested, "CodeReviewNotRequested");
            DataTable CodeReviewPrComments = GetCodeReviewGitPullRequestsData(codeReviewDetails.codeReviewFromGitPullRequests);
            workBook.Worksheets.Add(CodeReviewPrComments, "CodeReviewGitPrComments");
            DataTable teamDetails = GetTeamDetails(teamMembers, codeReviewDetails);
            workBook.Worksheets.Add(teamDetails, "TeamDetails");


            SetColumnWidths(workBook.Worksheet("CodeReviewComments"), new int[] { 15, 16, 20, 20, 15, 15, 40, 50, 15 });
            SetColumnWidths(workBook.Worksheet("RequestedButZeroComments"), new int[] { 15, 20, 20, 40, 20, 22 });
            SetColumnWidths(workBook.Worksheet("CodeReviewNotRequested"), new int[] { 15, 20, 20, 40, 20, 22 });
            SetColumnWidths(workBook.Worksheet("CodeReviewGitPrComments"), new int[] { 15, 16, 20, 23, 15, 15, 40, 50, 15, 20, 25 });
            SetColumnWidths(workBook.Worksheet("TeamDetails"), new int[] { 15, 20, 30, 20, 20, 20, 20, 20, 20 });


            StyleWorkSheet(workBook.Worksheet("CodeReviewComments"), XLColor.Black, XLBorderStyleValues.Thin, XLTableTheme.TableStyleLight8);
            StyleWorkSheet(workBook.Worksheet("RequestedButZeroComments"), XLColor.Black, XLBorderStyleValues.Thin, XLTableTheme.TableStyleLight8);
            StyleWorkSheet(workBook.Worksheet("CodeReviewNotRequested"), XLColor.Black, XLBorderStyleValues.Thin, XLTableTheme.TableStyleLight8);
            StyleWorkSheet(workBook.Worksheet("CodeReviewGitPrComments"), XLColor.Black, XLBorderStyleValues.Thin, XLTableTheme.TableStyleLight8);
            StyleWorkSheet(workBook.Worksheet("TeamDetails"));

            string fileName = System.IO.Path.Combine(FileDropLocation, $"CodeReviewDetails_{sprint}.xlsx");
            workBook.SaveAs(fileName);
            Console.WriteLine($"Report is saved at the location {fileName}");
            return fileName;
        }

        private void StyleWorkSheet(IXLWorksheet worksheet, XLColor color = null, XLBorderStyleValues borderStyleValues = XLBorderStyleValues.None,
            XLTableTheme xLTableTheme = null)
        {

            worksheet.Cells().Style.Border.BottomBorderColor = color ?? XLColor.NoColor;
            worksheet.Cells().Style.Border.TopBorderColor = color ?? XLColor.NoColor;
            worksheet.Cells().Style.Border.RightBorderColor = color ?? XLColor.NoColor;
            worksheet.Cells().Style.Border.LeftBorderColor = color ?? XLColor.NoColor;
            worksheet.Cells().Style.Border.TopBorder = borderStyleValues;
            worksheet.Cells().Style.Border.BottomBorder = borderStyleValues;
            worksheet.Cells().Style.Border.LeftBorder = borderStyleValues;
            worksheet.Cells().Style.Border.RightBorder = borderStyleValues;
            worksheet.Row(1).Style.Font.SetBold(true);
            worksheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            worksheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            worksheet.Cells().Style.Alignment.WrapText = true;
            worksheet.Tables.First().Theme = xLTableTheme ?? XLTableTheme.None;
            worksheet.Tables.First().ShowAutoFilter = false;
        }

        private static void SetColumnWidths(IXLWorksheet worksheet, int[] columnWidths)
        {
            for (int i = 1; i < columnWidths.Length; i++)
            {
                worksheet.Columns(i.ToString()).Width = columnWidths[i];
            }
        }
    }
}
