using System;
using System.Collections.Generic;

namespace ServiceLayer.Dtos
{
    public class CodeReviewGitPullRequest
    {
        public int PullRequest { get; set; }
        public string Owner { get; set; }
        public List<CodeReviewReviewer> Reviewers { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public DateTime ClosedDate { get; set; }
        public string Title { get; set; }
        public List<CodeReviewComment> CodeReviewComments { get; set; }
        public string Status { get; set; }
        public string Repository { get; set; }
        public string TargetBranch { get; set; }
    }
}