using System.Net.Mail;
using System.Net;

public class EmailSender
{
    private readonly string _username;
    private readonly string _password;

    public EmailSender(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public async Task SendEmailAsync(string emailTO, string subject, string body)
    {

        // Config mail
        MailMessage mail = new MailMessage();
        mail.From = new MailAddress(_username);
        mail.To.Add(emailTO);
        mail.Subject = subject;
        mail.Body = body;
        mail.IsBodyHtml = true;

        // Config smtp to send mail => Can be change into a function
        SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
        smtpClient.EnableSsl = true;
        smtpClient.Credentials = new NetworkCredential(_username, _password);
        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

        try
        {
            await smtpClient.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}

