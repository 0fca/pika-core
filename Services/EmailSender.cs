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
                    SmtpClient smtp = new SmtpClient   
                    {  
                        Host = "me.lukas-bownik.net",  
                        Port = 25,  
                        DeliveryMethod = SmtpDeliveryMethod.Network,  
                        Credentials = new System.Net.NetworkCredential("obsidiam@me.lukas-bownik.net", "@lt41r!bnL4h4d"),  
                        Timeout = 30000,  
                    };  
            MailMessage msg = new MailMessage("obsidiam@me.lukas-bownik.net", email, subject, message);  
            smtp.Send(msg);  
                }
                catch(Exception ex){ 
                    Debug.WriteLine(ex.GetBaseException().Message);
                }
            });
        }
    }
}
