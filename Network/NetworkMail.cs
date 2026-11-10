using System.Net;
using System.Net.Mail;
using Server.Data;

namespace Server.Network;

public class NetworkMail(AccountService accountService)
{
    private AccountService _accountService = accountService;
    
    private readonly string _mailAddressFrom = "ilopuke@gmail.com";
    private readonly string _mailAddressPassword = "tiqs gzwo xugs rgud";
    private readonly Random _random = new();
    
    public string SendMailCodeToAccount(string email)
    {
        var message = new MailMessage();
        var code = _random.Next(10001, 99999).ToString();
            
        message.From = new MailAddress(_mailAddressFrom);
        message.To.Add(email);
        message.Subject = $"{Constants.APP_NAME} - Registration";
        message.Body = $"Hello, your code: {code}";
        message.BodyEncoding = System.Text.Encoding.UTF8;
        
        var client = new SmtpClient
        {
            Host = "smtp.gmail.com",
            Port = 587,    
            Credentials = new NetworkCredential( message.From.Address, _mailAddressPassword),
            EnableSsl = true, 
        };
        
        ServicePointManager.ServerCertificateValidationCallback =
            (sender, certificate, chain, errors) => true;

        client.Send(message);

        return code;
    }
}