using Microsoft.Extensions.DependencyInjection;
using Server;
using Server.Data.Interface;
using Server.Network;
using Server.Network.Interface;

ServiceContainer.Initialize(); 
ServiceContainer.Provider.GetRequiredService<IConfig>();
ServiceContainer.Provider.GetRequiredService<IDatabaseService>();
ServiceContainer.Provider.GetRequiredService<TonService>();
