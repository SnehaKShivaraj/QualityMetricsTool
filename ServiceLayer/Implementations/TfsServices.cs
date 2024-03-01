using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.Discussion.Client;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using WebModels = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using ServiceLayer.Dtos;
using ServiceLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace ServiceLayer.Implementations
{
    public class TfsServices : ITfsServices
    {
        private const string codeReviewRequest = "Code Review Request";
        private const string branchHead = "refs/heads/";

        private TfsTeamProjectCollection projectCollection;
        private Guid projectId;
        private List<WebApiTeamRef> teamIds;

        private static readonly CustomConfiguration customConfig = (CustomConfiguration)ConfigurationManager.GetSection("customConfig");
        private readonly string tfsUrlString = ConfigurationManager.AppSettings.Get("TfsUrl").Trim().ToString();
        private readonly string projectName = ConfigurationManager.AppSettings.Get("Project").Trim().ToString();
        private readonly List<string> teams = ConfigurationManager.AppSettings.Get("Teams").Split(',').Select(i=>i.Trim()).ToList();
        private readonly string versionControl = ConfigurationManager.AppSettings.Get("VersionControlPath").Trim().ToString();
        private readonly List<GitElement> gitConfig = customConfig.GitElements.ToList();

        public List<TeamSettingsIteration> Iterations { get; private set; }

        public void Initialize()
        {
            projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(tfsUrlString));
            projectId = FindProject(projectCollection, projectName).Id;
            teamIds = FindTeams(projectCollection, projectId).ToList();
            // TODO: Manage for all teams
            Iterations = GetIterations(projectCollection, projectId, teamIds.Select(i=>i.Id).ToList());
        }

        public List<TeamSettingsIteration> GetIterationsForASprint(string sprintName)
        {
            return Iterations.Where(i => i.Name.Equals(sprintName, StringComparison.InvariantCultureIgnoreCase)).ToList();
        }

        private static TeamProjectReference FindProject(TfsTeamProjectCollection collection, string projectName)
        {
            return InternalFindProject(collection, projectName, null);
        }

        private static TeamProjectReference FindProject(TfsTeamProjectCollection collection, Guid projectId)
        {
            return InternalFindProject(collection, null, projectId);
        }

        private static TeamProjectReference InternalFindProject(TfsTeamProjectCollection collection, string projectName, Guid? projectId)
        {
            var projectClient = collection.GetClient<ProjectHttpClient>();

            TeamProjectReference project = null;
            if (projectId == null)
            {
                Console.WriteLine();
                Console.WriteLine("---- Finding projects ----");
                // Get the first project
                foreach (var prj in projectClient.GetProjects(null).Result)
                {
                    //Console.WriteLine($"{prj.Name} {prj.Id}");
                    if (prj.Name.Equals(projectName))
                    {
                        Console.WriteLine($"Project found: {prj.Name}");
                        return prj;
                    }
                }
            }
            else
            {
                // Get the details for this project
                project = projectClient.GetProject(projectId.ToString()).Result;
            }

            if (project == null)
            {
                throw new Exception("No project found. Review the configuration");
            }
            return project;
        }

        private ICollection<WebApiTeamRef> FindTeams(TfsTeamProjectCollection collection, Guid projectId)
        {
            var foundTeams = FindTeams(collection, projectId, teams);
            if (!foundTeams.Any())
            {
                throw new Exception("No teams have been found. Review the configuration");
            }

            return foundTeams;
        }

        private static ICollection<WebApiTeamRef> FindTeams(TfsTeamProjectCollection collection, Guid projectId, ICollection<string> teamNameListToFind)
        {
            var teamClient = collection.GetClient<TeamHttpClient>();
            var foundTeams = new List<WebApiTeamRef>();

            Console.WriteLine();
            Console.WriteLine("---- Finding teams ----");
            foreach (var teamRecord in teamClient.GetTeamsAsync(projectId.ToString(), top: 100, skip: 0).Result)
            {
                //Console.WriteLine($"{teamRecord.Name} {teamRecord.Id}");
                if (teamNameListToFind.Any(name => teamRecord.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Console.WriteLine($"Team found: {teamRecord.Name}");
                    foundTeams.Add(teamRecord);
                }
            }

            return foundTeams;
        }
        public static WebModels.Wiql GetWiql(string query)
        {
            return new WebModels.Wiql
            {
                Query = query
            };
        }

        public List<WebModels.WorkItem> GetIterationsProductBacklogItemsDetails(string ItertionPath, List<string> fields, string state)
        {
            string query = string.Format("Select [Id] from WorkItems " +
                            "Where [Work Item Type] ='Product Backlog Item' " +
                            "And [State] ='{0}' " +
                            "And [Team Project] = '{1}'" +
                            "And [Iteration Path] = '{2}'", state, projectName, ItertionPath);

            var workClient = projectCollection.GetClient<WorkItemTrackingHttpClient>();
            var workItemQueryResult = workClient.QueryByWiqlAsync(GetWiql(query)).Result;

            var ids = workItemQueryResult.WorkItems.Select(i => i.Id).ToList();

            return GetWorkItemsBatchAsync(fields, workClient, workItemQueryResult, ids);

        }

        private static List<WebModels.WorkItem> GetWorkItemsBatchAsync(List<string> fields, WorkItemTrackingHttpClient workClient, WebModels.WorkItemQueryResult workItemQueryResult, List<int> ids)
        {
            return workClient.GetWorkItemsBatchAsync(
                                new WebModels.WorkItemBatchGetRequest
                                {
                                    Ids = ids,
                                    Fields = fields,
                                    AsOf = workItemQueryResult.AsOf
                                }).Result;
        }

        public List<TeamSettingsIteration> GetIterations(TfsTeamProjectCollection collection, Guid projectId, List<Guid> teamIds)
        {
            var teamSettingsIterations = new List<TeamSettingsIteration>();

            var workClient = collection.GetClient<WorkHttpClient>();

            foreach (Guid teamId in teamIds)
            {
                var context = new TeamContext(projectId, teamId);
                
                var teamSettingsIteration = workClient.GetTeamIterationsAsync(context).Result;
                teamSettingsIterations.AddRange(teamSettingsIteration);
            }

            return teamSettingsIterations;
        }


        private WorkItem GetWorkItemById(int workItemId)
        {
            var workItemStore = projectCollection.GetService<WorkItemStore>();
            return workItemStore.GetWorkItem(workItemId);
        }

        public List<CodeReviewGitPullRequest> GetGitPullRequestsDetails(Guid iterationId)
        {
            if (!gitConfig.Any())
            {
                Console.WriteLine();
                Console.WriteLine("---- No GIT repositories have been configured. Skipping it ----");
                return new List<CodeReviewGitPullRequest>();
            }

            var iteration = Iterations.First(i => i.Id == iterationId);

            Console.WriteLine();
            Console.WriteLine("---- Finding GIT repositories ----");

            var gitClient = projectCollection.GetClient<GitHttpClient>();
            var repositories = gitClient.GetRepositoriesAsync(projectId).Result
                .Where(r => gitConfig.Select(g => g.Repository).Any(n => r.Name.Equals(n, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();

            if (!repositories.Any())
            {
                throw new Exception("No repository has been found. Review the configuration");
            }
            Console.WriteLine($"Repositories found: {string.Join(", ", repositories.Select(r => r.Name))}");

            var prComments = new List<CodeReviewGitPullRequest>();
            foreach (var repository in repositories)
            {
                var pullRequests = GetPullRequests(repository, iteration.Attributes.StartDate.Value, iteration.Attributes.FinishDate.Value);

                Console.WriteLine($"Retrieving comments for {pullRequests.Count} Pull Requests ...");
                foreach (var pullRequest in pullRequests)
                {
                    var pullTheads = gitClient.GetThreadsAsync(projectId, repository.Id, pullRequest.PullRequestId).Result;
                    var comments = pullTheads.SelectMany(t => t.Comments.Where(c => c.CommentType == Microsoft.TeamFoundation.SourceControl.WebApi.CommentType.Text));

                    prComments.Add(new CodeReviewGitPullRequest
                    {
                        PullRequest = pullRequest.PullRequestId,
                        Owner = pullRequest.CreatedBy.DisplayName,
                        Reviewers = pullRequest.Reviewers.Select(r => new CodeReviewReviewer 
                        { 
                            Name = r.DisplayName, 
                            HasAccepted = r.Vote != 0,
                            ReviewStatus = r.Vote == 10 ? CodeReviewReviewStatus.Approved 
                                : r.Vote == 5 ? CodeReviewReviewStatus.ApprovedWithSuggestions 
                                : r.Vote == -10 ? CodeReviewReviewStatus.Rejected 
                                : CodeReviewReviewStatus.None
                        }).ToList(),
                        ClosedDate = pullRequest.ClosedDate,
                        ReviewedDate = pullRequest.CreationDate,
                        Title = pullRequest.Title,
                        CodeReviewComments = comments.Select(c => new CodeReviewComment
                        {
                            Author = c.Author.DisplayName,
                            ReviewedDate = c.PublishedDate,
                            Comments = c.Content
                        }).ToList(),
                        Status = pullRequest.Status.ToString(),
                        Repository = repository.Name,
                        TargetBranch = pullRequest.TargetRefName.Replace(branchHead, "")
                    });
                }
            }

            return prComments;
        }

        private List<GitPullRequest> GetPullRequests(GitRepository repository, DateTime startDate, DateTime toDate)
        {
            var gitClient = projectCollection.GetClient<GitHttpClient>();
            var pullRequests = new List<GitPullRequest>();
            var matchingPrs = new List<GitPullRequest>();

            Console.WriteLine();
            Console.WriteLine($"---- Retrieving Pull requests for {repository.Name} ----");

            //return new[] { gitClient.GetPullRequestAsync(projectId, repository.Id, 47701).Result }.ToList();

            var branches = gitConfig.Single(g => g.Repository.Equals(repository.Name, StringComparison.InvariantCultureIgnoreCase)).TargetBranches?.ToList() ?? new[] { "" }.ToList();
            if (!branches.Any())
            {
                branches.Add("");
            }
            foreach (var branch in branches.Select(b => branchHead + b.Replace(branchHead, "")))
            {
                var countToRetrieve = 50;
                var countToSkip = 0;
                do
                {
                    var searchCriterias = new GitPullRequestSearchCriteria
                    {
                        Status = PullRequestStatus.All,
                        TargetRefName = branch.Equals(branchHead) ? null : branch
                    };
                    var prChunkList = gitClient.GetPullRequestsAsync(projectId, repository.Id, searchCriterias, top: countToRetrieve, skip: countToSkip).Result;
                    matchingPrs = prChunkList
                        .Where(pr => pr.ClosedDate >= startDate && pr.ClosedDate <= toDate)
                        .ToList();
                    pullRequests.AddRange(matchingPrs);
                    countToSkip += countToRetrieve;
                    if (prChunkList.Count < countToRetrieve)
                    {
                        countToRetrieve = 0;
                    }
                } while (countToRetrieve > 0 && (!pullRequests.Any() || matchingPrs.Any()));
            }

            return pullRequests;
        }

        private IEnumerable<CodeReviewComment> GetReviewComments(int workItemId, string createdBy)
        {
            TeamFoundationDiscussionService service = new TeamFoundationDiscussionService();
            service.Initialize(projectCollection);
            IDiscussionManager discussionManager = service.CreateDiscussionManager();
            IAsyncResult discussions = discussionManager.BeginQueryByCodeReviewRequest(workItemId, QueryStoreOptions.ServerAndLocal, null, null);
            var discussionThreads = discussionManager.EndQueryByCodeReviewRequest(discussions);
            List<CodeReviewComment> codeReviewComments = new List<CodeReviewComment>();
            foreach (DiscussionThread discussionThread in discussionThreads)
            {
                if (discussionThread.RootComment != null && discussionThread.RootComment.Author.DisplayName != createdBy)
                {
                    codeReviewComments.Add(new CodeReviewComment
                    {
                        Author = discussionThread.RootComment.Author.DisplayName,
                        Comments = discussionThread.RootComment.Content,
                        ReviewedDate = discussionThread.RootComment.PublishedDate.Date
                    });

                }
            }

            return codeReviewComments;

        }

        public IEnumerable<Changeset> GetChangesetsBetweenDates(DateTime startDate, DateTime toDate)
        {

            VersionSpec fromDateVersion = new DateVersionSpec(startDate);
            VersionSpec toDateVersion = new DateVersionSpec(toDate);

            VersionControlServer versionControl = projectCollection.GetService<VersionControlServer>();
            var path = versionControl.GetTeamProject(projectName).ServerItem + this.versionControl;

            return versionControl.QueryHistory(path, VersionSpec.Latest, 0, RecursionType.Full, "",
                                        fromDateVersion, toDateVersion, int.MaxValue, true, true).OfType<Changeset>().AsEnumerable();

        }

        public CodeReviewDetails GetCodeReviewDetails(Guid iterationId)
        {
            Console.WriteLine();
            Console.WriteLine("---- Fetching Changeset Details ----");
            List<CodeReviewCompletedChangeset> codeReviewCompletedChangesets = new List<CodeReviewCompletedChangeset>();
            List<NoCodeReviewRequestChangesets> noCodeReviewRequestChangesets = new List<NoCodeReviewRequestChangesets>();
            List<CodeReviewNotDoneChangesets> codeReviewNotDoneChangesets = new List<CodeReviewNotDoneChangesets>();

            var iteration = Iterations.Single(i => i.Id == iterationId);
            IEnumerable<Changeset> changesets = GetChangesetsBetweenDates(iteration.Attributes.StartDate.Value, iteration.Attributes.FinishDate.Value);

            foreach (Changeset changeset in changesets)
            {
                Console.WriteLine($"Fetching details of the changeset - {changeset.ChangesetId}");
                if (changeset.WorkItems.Any(i => i.Type.Name == codeReviewRequest))
                {

                    AddIntoCompletedOrRequestedCodeReviewChangesets(changeset,
                                                                    changeset.WorkItems,
                                                                    ref codeReviewCompletedChangesets,
                                                                    ref codeReviewNotDoneChangesets);

                }
                else
                {
                    AddIntoCodeReviewRequestChangesets(changeset, ref noCodeReviewRequestChangesets);
                }
            }
            Console.WriteLine();
            Console.WriteLine("Collected Changeset Details");
            return new CodeReviewDetails
            {
                codeReviewCompletedChangesets = codeReviewCompletedChangesets,
                noCodeReviewRequestChangesets = noCodeReviewRequestChangesets,
                codeReviewNotDoneChangesets = codeReviewNotDoneChangesets
            };
        }

        private void AddIntoCompletedOrRequestedCodeReviewChangesets(Changeset changeset, IEnumerable<WorkItem> workItems, ref List<CodeReviewCompletedChangeset> codeReviewCompletedChangesets, ref List<CodeReviewNotDoneChangesets> codeReviewNotDoneChangesets)
        {
            List<string> codeReviewAssignedToTheReviewersList = new List<string>();
            List<string> reviewersWhoCompletedReview = new List<string>();
            List<string> finalCodeReviewStatus = new List<string>();
            List<CodeReviewComment> codeReviewComments = new List<CodeReviewComment>();
            var availableStatuses = new List<string> { "Needs Work", "With Comments", "Looks Good" };

            workItems = workItems.Where(workItem => workItem.Type.Name == codeReviewRequest);

            foreach (WorkItem workItem in workItems)
            {
                List<int> relatedLinkIds = workItem.Links.OfType<RelatedLink>().Select(r => r.RelatedWorkItemId).ToList();
                foreach (int relatedLinkId in relatedLinkIds)
                {
                    var item = GetWorkItemById(relatedLinkId).Fields.OfType<Field>();
                    codeReviewAssignedToTheReviewersList.Add(item.FirstOrDefault(f => f.Name == "Reviewed By")?.Value?.ToString());
                    if (!string.IsNullOrEmpty(item.FirstOrDefault(f => f.Name == "Closed By")?.Value?.ToString()))
                    {
                        string closedBy = item.FirstOrDefault(f => f.Name == "Closed By")?.Value?.ToString();
                        if (closedBy != workItem.CreatedBy)
                        {
                            reviewersWhoCompletedReview.Add(closedBy);

                            finalCodeReviewStatus.Add(item.FirstOrDefault(f => f.Name == "Closed Status")?.Value.ToString());
                        }
                    }
                }
                codeReviewComments.AddRange(GetReviewComments(workItem.Id, workItem.CreatedBy));
            }

            string status = string.Empty;
            finalCodeReviewStatus = finalCodeReviewStatus.Where(s => !string.IsNullOrEmpty(s) && availableStatuses.Contains(s)).ToList();
            if (finalCodeReviewStatus.Distinct().Count() == 1)
                status = finalCodeReviewStatus.First();
            else if (finalCodeReviewStatus.Any(s => s == "Needs Work") && (finalCodeReviewStatus.Any(s => s == "With Comments") || finalCodeReviewStatus.Any(s => s == "Looks Good")))
                status = "Needs Work";
            else if (finalCodeReviewStatus.Any(s => s == "With Comments") && finalCodeReviewStatus.Any(s => s == "Looks Good"))
                status = "With Comments";

            if (reviewersWhoCompletedReview.Any())
            {
                codeReviewCompletedChangesets.Add(
                    new CodeReviewCompletedChangeset
                    {
                        Changeset = changeset.ChangesetId,
                        Owner = changeset.OwnerDisplayName,
                        Reviewers = reviewersWhoCompletedReview.Distinct().OrderBy(s => s).ToList(),
                        CheckedInDate = changeset.CreationDate.Date,
                        ReviewedDate = codeReviewComments.Min(i => i.ReviewedDate) ?? changeset.CreationDate.Date,
                        Status = status,
                        Title = changeset.Comment,
                        CodeReviewComments = codeReviewComments
                    });
            }
            else
            {
                codeReviewNotDoneChangesets.Add(
                     new CodeReviewNotDoneChangesets
                     {
                         Changeset = changeset.ChangesetId,
                         Title = changeset.Comment,
                         Owner = changeset.OwnerDisplayName,
                         CheckedInDate = changeset.CreationDate.Date,
                         Reviewers = codeReviewAssignedToTheReviewersList
                     });
            }
        }

        private void AddIntoCodeReviewRequestChangesets(Changeset changeset, ref List<NoCodeReviewRequestChangesets> noCodeReviewRequestChangesets)
        {
            noCodeReviewRequestChangesets.Add(
                new NoCodeReviewRequestChangesets
                {
                    Changeset = changeset.ChangesetId,
                    Owner = changeset.OwnerDisplayName,
                    Title = changeset.Comment,
                    CheckedInDate = changeset.CreationDate.Date
                });
        }

        public List<GetTeamDetails> GetTeamMembersOfTheSprint(List<TeamSettingsIteration> iterations)
        {
            var workClient = projectCollection.GetClient<WorkHttpClient>();

            List<GetTeamDetails> teamDetails = new List<GetTeamDetails>();
            foreach (var teamRef in teamIds)
            {
                var context = new TeamContext(projectId, teamRef.Id);
                var itrs = workClient.GetTeamIterationsAsync(context).Result.Select(i=>i.Id);
                Guid iterationId = iterations.First(i=>itrs.Contains(i.Id)).Id;
                var capacityList = workClient.GetCapacitiesAsync(context, iterationId).Result;

                var teamMembers = new List<string>();
                foreach (var teamMember in capacityList)
                {
                    if (teamMember.Activities.Any(i => i.CapacityPerDay > 0.0))
                        teamMembers.Add(TransformUserNameToSimpleFormat(teamMember.TeamMember.DisplayName));
                }
                teamDetails.Add(new GetTeamDetails { TeamName = teamRef.Name, Members = teamMembers });
            }
            return teamDetails;
        }

        private string TransformUserNameToSimpleFormat(string userName)
        {
            var position = userName.IndexOf(" <");
            return position >= 0 ? userName.Substring(0, position) : userName;
        }
    }
}
