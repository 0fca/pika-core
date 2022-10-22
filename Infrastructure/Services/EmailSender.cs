using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace PikaCore.Infrastructure.Services
{
    // Use Twilio client to do it using Twilio.
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Task.Factory.StartNew(() =>
            {
                Debug.WriteLine("Task started!");
                try
                {
                    MailMessage mail = new MailMessage("sadsoldier502@wp.pl", "lukasbownik99@gmail.com");
                    SmtpClient client = new SmtpClient();
                    client.Port = 587;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential("mail@gmail.com", "pass");
                    client.Host = "smtp.gmail.com";
                    mail.Subject = "this is a test email.";
                    mail.Body = "this is my test email body";
                    client.Send(mail);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.GetBaseException().Message);
                }
            });
        }
    }
}
