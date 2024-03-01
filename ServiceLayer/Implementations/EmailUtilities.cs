
using ServiceLayer.Dtos;
using ServiceLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace ServiceLayer.Implementations
{
    public class EmailUtilities:IEmailUtilities
    {
        private readonly List<string> recipients = ConfigurationManager.AppSettings.Get("Recipients").Split(',').Select(i => i.Trim()).ToList();
        public void SendEmail(SendEmailRequest sendEmailRequest)
        {
            MailMessage message = new MailMessage();

            message.From = GetFromMailAddress();
            AddMailRecipients(message);

            message.Subject = string.Format("{0} | Quality Metrics of the {1}", sendEmailRequest.applicationName, sendEmailRequest.sprintName);
            message.Body = "Hi,\n\nPFA peer code review report for the " + sendEmailRequest.sprintName + ".\n\nThanks!";
            message.Attachments.Add(new Attachment(sendEmailRequest.filePath));

            SmtpClient smtpClient = GetSmtpClient();
            try
            {
                smtpClient.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured while sending an email", ex.InnerException);
                throw;
            }
        }

        private void AddMailRecipients(MailMessage message)
        {
            foreach (string recipient in recipients)
                message.To.Add(recipient);
        }

        private static SmtpClient GetSmtpClient()
        {
            SmtpClient smtpClient = new SmtpClient("", 25);
            smtpClient.EnableSsl = false;
            smtpClient.UseDefaultCredentials = true;
            //smtpClient.Credentials = (ICredentialsByHost)new NetworkCredential("name, "password");
         
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            return smtpClient;
        }
        private static MailAddress GetFromMailAddress()
        {
            return new MailAddress("abcd@gmail.com");
        }
    }
}
