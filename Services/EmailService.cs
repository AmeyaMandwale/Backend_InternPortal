using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace InternConnect_Backend.Services
{
    public class EmailService
    {
        private readonly string _fromEmail = "ishwarmodak55@gmail.com"; // replace this by creating ur own app on google account
        private readonly string _password = "cjhubxpeyjgpogxw"; //a 16 letter password will be given to u at that time of creation replace this with it

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            using (var smtp = new SmtpClient("smtp.gmail.com", 587))
            {
                smtp.Credentials = new NetworkCredential(_fromEmail, _password);
                smtp.EnableSsl = true;

                var mail = new MailMessage(_fromEmail, toEmail, subject, body)
                {
                    IsBodyHtml = true
                };

                await smtp.SendMailAsync(mail);
            }
        }
    }
}
