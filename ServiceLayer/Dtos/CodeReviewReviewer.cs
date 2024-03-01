namespace ServiceLayer.Dtos
{
    public class CodeReviewReviewer
    {
        public string Name { get; set; }
        public bool HasAccepted { get; set; }
        public CodeReviewReviewStatus ReviewStatus { get; set; }
    }

    public enum CodeReviewReviewStatus
    {
        None,
        Approved,
        ApprovedWithSuggestions,
        Rejected
    }
}