using Microsoft.Extensions.DependencyInjection;
using Riptide;
using Riptide.Utils;
using Server.Logger.Interface;
using Server.Managers;
using Server.Network.Interface;

namespace Server.Network;

public class NetworkConfig : IConfig
{
    public Riptide.Server Server { get; private set; }
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger _logger;

    public ushort CurrentTick { get; set; } = 0;

    public NetworkConfig(ConnectionManager connectionManager, ILogger logger)
    {
        Console.Title = "Server";
        Console.Clear();
        
        RiptideLogger.Initialize(Console.WriteLine, true);
        
        Message.MaxPayloadSize = 2048;

        _connectionManager = connectionManager;
        _logger = logger;
        
        Server = new Riptide.Server();
        Server.TimeoutTime = 15_000;

        Server.ClientConnected += OnConnectionConnected;
        Server.ClientDisconnected += OnConnectionDisconnected;

        Server.Start(7777, 10000);

        var serverThread = new Thread(UpdateServer);
            serverThread.Start();
    }

    private void OnConnectionConnected(object sender, ServerConnectedEventArgs callBack)
    { 
        _connectionManager.Add(callBack.Client);
    }
        
    private void OnConnectionDisconnected(object sender, ServerDisconnectedEventArgs callBack)
    {
        _connectionManager.Remove(callBack.Client);
    }
    
    private void UpdateServer()
    {
        _logger.Print($"Close key [{ConsoleKey.Q}]");
        
        var isRunning = true;
        
        while (isRunning)
        {
            try
            {
                if (Server != null && Server.IsRunning)
                {
                    Server.Update();
                }
        
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
                    if (keyInfo.Key == ConsoleKey.Q)
                    {
                        isRunning = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Print($"Error in UpdateServer: {ex.Message}");
            }
        }
        
        _logger.Print("Server stopped");
    }
}