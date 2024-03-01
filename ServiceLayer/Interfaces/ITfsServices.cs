
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using ServiceLayer.Dtos;
using System;
using System.Collections.Generic;

namespace ServiceLayer.Interfaces
{
    public interface ITfsServices
    {
        void Initialize();
        List<TeamSettingsIteration> GetIterationsForASprint(string sprintName);
        CodeReviewDetails GetCodeReviewDetails(Guid iterationId);
        List<GetTeamDetails> GetTeamMembersOfTheSprint(List<TeamSettingsIteration> iterations);
        List<CodeReviewGitPullRequest> GetGitPullRequestsDetails(Guid iterationId);
        List<WorkItem> GetIterationsProductBacklogItemsDetails(string ItertionPath, List<string> fields, string state);
    }
}
