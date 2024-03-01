using System.Collections.Generic;

namespace ServiceLayer.Dtos
{
    public class CodeReviewDetails
    {
        public List<CodeReviewCompletedChangeset> codeReviewCompletedChangesets { get; set; }
        public List<NoCodeReviewRequestChangesets> noCodeReviewRequestChangesets { get; set; }
        public List<CodeReviewNotDoneChangesets> codeReviewNotDoneChangesets { get; set; }
        public List<CodeReviewGitPullRequest> codeReviewFromGitPullRequests { get; set; }
    }
}
