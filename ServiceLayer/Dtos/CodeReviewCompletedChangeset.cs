using System;
using System.Collections.Generic;

namespace ServiceLayer.Dtos
{
    public class CodeReviewCompletedChangeset
    {
        public int Changeset { get; set; }
        public string Owner { get; set; }
        public List<string> Reviewers { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public DateTime CheckedInDate { get; set; }
        public string Title { get; set; }
        public List<CodeReviewComment> CodeReviewComments { get; set; }
        public string Status { get; set; }
    }
}