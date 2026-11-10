using Microsoft.Extensions.DependencyInjection;
using Server.Data;
using Server.Data.Interface;
using Server.Logger;
using Server.Logger.Interface;
using Server.Managers;
using Server.Network;
using Server.Network.Interface;
using SQLitePCL; 

namespace Server;

public static class ServiceContainer
{
    private static IServiceCollection? _serviceCollection;
    public static IServiceProvider? Provider;

    public static void Initialize()
    {
        _serviceCollection = new ServiceCollection();

        _serviceCollection.AddSingleton<IConfig, NetworkConfig>();
        _serviceCollection.AddSingleton<ILogger, ConsoleLogger>();
        _serviceCollection.AddSingleton<IDatabaseService, SqliteDatabaseService>();
        
        _serviceCollection.AddSingleton<ConnectionManager>();
        
        _serviceCollection.AddSingleton<AccountService>();
        _serviceCollection.AddSingleton<TonService>();

        _serviceCollection.AddSingleton<NetworkMail>();

        Provider = _serviceCollection.BuildServiceProvider();
    }
}