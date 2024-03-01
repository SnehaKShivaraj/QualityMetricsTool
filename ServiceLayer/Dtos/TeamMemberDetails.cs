using Microsoft.TeamFoundation.Work.WebApi;
using System.Collections.Generic;


namespace ServiceLayer.Dtos
{
    public class TeamMemberDetails
    {
        public List<TeamMemberCapacity> Value { get; set; }
    }
}
