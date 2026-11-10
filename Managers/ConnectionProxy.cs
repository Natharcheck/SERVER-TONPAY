using Riptide;

namespace Server.Managers;

public class ConnectionProxy(Connection connection)
{
    private Connection _connection = connection;

    public Connection Connection
    {
        get => _connection;
        set => _connection = value;
    }

    public ushort ConnectionId => _connection.Id;
    public bool   IsConnected  => _connection.IsConnected;
    
    public string EmailCode =    string.Empty;
    public string AccountId =    string.Empty;
    public string SessionToken = string.Empty;
    
    public bool IsAuthorization = false;
}