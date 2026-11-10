using Microsoft.Extensions.DependencyInjection;
using Riptide;
using Server.Logger.Interface;
using Server.Managers;
using Server.Network.Interface;
using Server.Slots;

namespace Server.Network;

public enum ServerPackets : ushort
{
    ValidationMessage = 1,
    
    ValidationRegistration,
    ValidationAuthorization,
    ValidationVerificationCode,
    
    SlotsInitialized,
    SlotResult,
    
    SendTonUpLink
}

public enum EMessageTypes : ushort
{
    Success = 1,
    Waiting,
    Failed,
    Info,
}

public static class NetworkSend
{
    private static readonly IConfig Config = ServiceContainer.Provider.GetRequiredService<IConfig>();
    private static readonly ILogger Logger = ServiceContainer.Provider.GetRequiredService<ILogger>();

    private static readonly ConnectionManager ConnectionManager = ServiceContainer.Provider.GetRequiredService<ConnectionManager>();

    public static void MessageValidation(ushort connectionId, string text, EMessageTypes type, ushort lifeTime = 3)
    {
        var message = Message.Create(MessageSendMode.Reliable,ServerPackets.ValidationMessage);
            message.AddString(text);
            message.AddUShort((ushort)type);
            message.AddUShort(lifeTime);
            
        Config.Server.Send(message, connectionId);
    }
    
    public static void ValidationRegistration(ushort connectionId)
    {
        var message = Message.Create(MessageSendMode.Reliable,ServerPackets.ValidationRegistration);
            
        Config.Server.Send(message, connectionId);
    }
    public static void ValidationAuthorization(ushort connectionId)
    {
        var message = Message.Create(MessageSendMode.Reliable,ServerPackets.ValidationAuthorization);
            
        Config.Server.Send(message, connectionId);
    }
    public static void ValidationVerificationCode(ushort connectionId)
    {
        var message = Message.Create(MessageSendMode.Reliable,ServerPackets.ValidationVerificationCode);
            
        Config.Server.Send(message, connectionId);
    }
    
    public static void SendTonUpLink(ushort connectionId, string link)
    {
        var message = Message.Create(MessageSendMode.Reliable,ServerPackets.SendTonUpLink);
            message.AddString(link);
            
        Config.Server.Send(message, connectionId);
    }

    public static void SendSlotsInitialized(ushort connectionId)
    {
        var message = Message.Create(MessageSendMode.Reliable,ServerPackets.SlotsInitialized);
        Config.Server.Send(message, connectionId);
    }
    
    public static void SendResultSlot(ushort connectionId, SlotResult slotResult)
    {
        var message = Message.Create(MessageSendMode.Reliable, ServerPackets.SlotResult);

        message.AddUShort((ushort)slotResult.Grid.GetLength(0)); // rows
        message.AddUShort((ushort)slotResult.Grid.GetLength(1)); // columns

        for (int row = 0; row < slotResult.Grid.GetLength(0); row++)
        {
            for (int col = 0; col < slotResult.Grid.GetLength(1); col++)
            {
                message.AddString(slotResult.Grid[row, col].ToString());
            }
        }

        message.AddFloat((float)slotResult.WinAmount);
        message.AddUShort((ushort)slotResult.WinningLines.Count);

        foreach (var line in slotResult.WinningLines)
        {
            
        }

        Config.Server.Send(message, connectionId);
    }
}