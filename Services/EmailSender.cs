using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace FMS2.Services
{
    // This class is used by the application to send email for account confirmation and password reset.
    // For more details see https://go.microsoft.com/fwlink/?LinkID=532713
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Task.Factory.StartNew(() =>{
                Debug.WriteLine("Task started!");
                try{
                    MailMessage mail = new MailMessage("sadsoldier502@wp.pl", "lukasbownik99@gmail.com");
                    SmtpClient client = new SmtpClient();
                    client.Port = 587;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential("mail@gmail.com","pass");
                    client.Host = "smtp.gmail.com";
                    mail.Subject = "this is a test email.";
                    mail.Body = "this is my test email body";
                    client.Send(mail);
                }
                catch(Exception ex){ 
                    Debug.WriteLine(ex.GetBaseException().Message);
                }
            });
        }
    }
}
