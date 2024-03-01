using System;
using System.Collections.Generic;

namespace ServiceLayer.Dtos
{
    public class CodeReviewNotDoneChangesets
    {
        public int Changeset { get; set; }
        public string Title { get; set; }
        public string Owner { get; set; }
        public DateTime CheckedInDate { get; set; }
        public List<string> Reviewers { get; set; }
    }
}