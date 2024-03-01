
using ServiceLayer.Dtos;

namespace ServiceLayer.Interfaces
{
    public interface IEmailUtilities
    {
        void SendEmail(SendEmailRequest sendEmailRequest);
    }
}
