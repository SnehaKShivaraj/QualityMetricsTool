using System;

namespace ServiceLayer.Dtos
{
    public class NoCodeReviewRequestChangesets
    {
        public int Changeset { get; set; }
        public string Title { get; set; }
        public string Owner { get; set; }
        public DateTime CheckedInDate { get; set; }
    }
}