
using System.Collections.Generic;


namespace ServiceLayer.Dtos
{
    public class GetTeamDetails
    {
        public string TeamName { get; set; }
        public List<string> Members { get; set; }
    }
}
