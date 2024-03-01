using ServiceLayer.Dtos;
using System.Collections.Generic;

namespace ServiceLayer.Interfaces
{
    public interface IExcelUtilities
    {
        string CreateExcelReport(CodeReviewDetails codeReviewDetails, List<GetTeamDetails> teamMembers, string sprint);
    }
}
