
using Microsoft.TeamFoundation.Work.WebApi;
using System.Collections.Generic;

namespace ServiceLayer.Dtos
{
    public class GetIterationsResponse
    {
        public int Count { get; set; }
        public List<TeamSettingsIteration> Value { get; set; }
    }
}
