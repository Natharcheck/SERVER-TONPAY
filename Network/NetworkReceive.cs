using Microsoft.Extensions.DependencyInjection;
using Riptide;
using Server.Data;
using Server.Data.Interface;
using Server.Logger.Interface;
using Server.Managers;
using Server.Network.Interface;

namespace Server.Network;

public enum ClientPackets : ushort
{
    BindSessionToken = 1,
    
    Registration,
    Authentication,
    AutoAuthorization,
    ChangePassword,
    ChangeUsername,
    ValidationCode,
    
    SlotChoose,
    
    RequestTonUp,
}

public enum AdminPackets : ushort
{
    BindSessionToken = 2001,
}

public class NetworkReceive
{
    private static readonly ILogger Logger = ServiceContainer.Provider.GetRequiredService<ILogger>();
    private static readonly IDatabaseService DatabaseService = ServiceContainer.Provider.GetRequiredService<IDatabaseService>();
    private static readonly ConnectionManager ConnectionManager = ServiceContainer.Provider.GetRequiredService<ConnectionManager>();
    private static readonly AccountService AccountService = ServiceContainer.Provider.GetRequiredService<AccountService>();
    private static readonly NetworkMail NetworkMail = ServiceContainer.Provider.GetRequiredService<NetworkMail>();
    
    private static readonly Dictionary<string, PendingAccount> PendingAccounts = new();
    
    [MessageHandler((ushort)1001)]
    private static void PingSync(ushort connectionId, Message message) { }

    [MessageHandler((ushort)ClientPackets.BindSessionToken)]
    private static void BindSessionToken(ushort connectionId, Message message)
    {
        var sessionToken = message.GetString();
    
        ConnectionManager.Connection(connectionId, sessionToken);
    }

    [MessageHandler((ushort)ClientPackets.Registration)]
    private static void Registration(ushort connectionId, Message message)
    {
         var email = message.GetString();
         var password = message.GetString();

         if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
             return;

         if (AccountService.GetAccountIdByEmail(email) != null)
         {
             return;
         }
         
         var code = NetworkMail.SendMailCodeToAccount(email);
         
         PendingAccounts[email] = new PendingAccount(email, password, code);
         
         NetworkSend.ValidationRegistration(connectionId);
         NetworkSend.MessageValidation(connectionId, "Code sent to email.", EMessageTypes.Info);
    }

    [MessageHandler((ushort)ClientPackets.Authentication)]
    private static void Authorization(ushort connectionId, Message message)
    {
        var email = message.GetString();
        var password = message.GetString();
        var deviceId = message.GetString();
        var memberMe = message.GetBool();
        
        var accountIdByEmail = AccountService.GetAccountIdByEmail(email);
        var accountIdByPassword = AccountService.GetAccountIdByPassword(password, email);
        
        if (accountIdByEmail == null || accountIdByPassword == null || accountIdByEmail != accountIdByPassword)
        {
            NetworkSend.MessageValidation(connectionId, "Email or Password is not correct.", EMessageTypes.Failed);
            return;
        }
        
        ConnectionManager.Authorization(connectionId, accountIdByEmail, deviceId, memberMe);    
    }
    
    [MessageHandler((ushort)ClientPackets.AutoAuthorization)]
    private static void AutoAuthorization(ushort connectionId, Message message)
    {
        var deviceId = message.GetString();
        
        ConnectionManager.TryAutoAuthorize(connectionId, deviceId);
    }

    [MessageHandler((ushort)ClientPackets.ValidationCode)]
    private static void ValidationCode(ushort connectionId, Message message)
    {
        var email = message.GetString();
        var inputCode = message.GetString();

        if (PendingAccounts.TryGetValue(email, out var pendingData))
        {
            if (pendingData.Code == inputCode)
            {
                var success = AccountService.CreateAccount(email, pendingData.Password, string.Empty);
                if (success)
                {
                    PendingAccounts.Remove(email);
                    NetworkSend.ValidationVerificationCode(connectionId);
                    NetworkSend.MessageValidation(connectionId, "Account created!", EMessageTypes.Success);
                }
                else
                {
                    NetworkSend.MessageValidation(connectionId, "Failed to create account.", EMessageTypes.Failed);
                }
            }
            else
            {
                NetworkSend.MessageValidation(connectionId, "Invalid code.", EMessageTypes.Failed);
            }
        }
        else
        {
            NetworkSend.MessageValidation(connectionId, "No registration found.", EMessageTypes.Failed);
        }
    }

    [MessageHandler((ushort)ClientPackets.RequestTonUp)]
    private static void RequestTonUp(ushort connectionId, Message message)
    {
        var service = ServiceContainer.Provider!.GetRequiredService<TonService>();
    
        var amount = message.GetFloat();
        var link = service.GeneratePayLink(connectionId, amount);
    
        NetworkSend.SendTonUpLink(connectionId, link); ;
    }
}