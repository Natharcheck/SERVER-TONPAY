using Riptide;
using Server.Data;
using Server.Network;

namespace Server.Managers;

public class ConnectionManager(AccountService accountService)
{
    private readonly AccountService _accountService = accountService;
    
    private readonly Dictionary<ushort, ConnectionProxy> _connections = new();
    private readonly Dictionary<ushort, ConnectionProxy> _disconnections = new();
    
    private readonly Dictionary<string, ConnectionProxy> _connectionSessionTokens = new();      
    private readonly Dictionary<string, string> _sessionTokenToAccountId = new();    

    public Dictionary<ushort, ConnectionProxy> Connections => _connections;
    
    public void Add(Connection connection)
    {
        _connections[connection.Id] = new ConnectionProxy(connection);
    }

    public void Remove(Connection connection)
    {
        if(_connections.TryGetValue(connection.Id, out var outConnection))
        {
            _disconnections.Add(connection.Id, outConnection);
        }
        
        _connections.Remove(connection.Id);
    }

    public void Authorization(ushort connectionId, string accountId, string deviceId, bool memberMe)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            connection.AccountId = accountId;
            connection.IsAuthorization = true;

            if (memberMe)
            {
                _accountService.SetDeviceIdByAccountId(accountId, deviceId);
            }
            
            BindAccountIdToSessionToken(connection.SessionToken, connection.AccountId);
            
            NetworkSend.ValidationAuthorization(connectionId);
            NetworkSend.MessageValidation(connectionId, "Welcome!", EMessageTypes.Success);
        }
    }
    
    public void TryAutoAuthorize(ushort connectionId, string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            return;
        }
        
        var accountId = _accountService.GetAccountIdByDeviceId(deviceId); 
        
        if (string.IsNullOrEmpty(accountId))
        {
            return;
        }
        
        Authorization(connectionId, accountId, deviceId, false);
    }

    public void Connection(ushort connectionId, string sessionToken)
    {
        if (_connectionSessionTokens.TryGetValue(sessionToken, out var oldConnection))
        {
            _disconnections.Remove(oldConnection.ConnectionId);

            var restored = new ConnectionProxy(oldConnection.Connection)
            {
                SessionToken = sessionToken,
                AccountId = oldConnection.AccountId,
                IsAuthorization = oldConnection.IsAuthorization
            };

            restored.Connection = _connections[connectionId].Connection;

            _connections[connectionId] = restored;
            _connectionSessionTokens[sessionToken] = restored;

            if (_sessionTokenToAccountId.TryGetValue(sessionToken, out var accountId))
            {
                restored.AccountId = accountId;
                restored.IsAuthorization = true;
            }
            
            return;
        }

        BindConnectionToSessionToken(connectionId, sessionToken);
    }

    public void BindConnectionToSessionToken(ushort connectionId, string sessionToken)
    {
        if(_connections.TryGetValue(connectionId, out var connection))
        {
            if (_connectionSessionTokens.ContainsKey(sessionToken) == false)
            {
                connection.SessionToken = sessionToken;
            
                _connectionSessionTokens[sessionToken] = connection;  
            }
            
            if (_sessionTokenToAccountId.TryGetValue(sessionToken, out var accountIdNew))
            {
                connection.AccountId = accountIdNew;
                connection.IsAuthorization = true;
            }
        }
    }

    public void BindAccountIdToSessionToken(string sessionToken, string accountToken)
    {
        var existingToken = _sessionTokenToAccountId.FirstOrDefault(x => x.Value == accountToken).Key;
        
        if (!string.IsNullOrEmpty(existingToken))
        {
            _sessionTokenToAccountId.Remove(existingToken);
        }
        
        _sessionTokenToAccountId[sessionToken] = accountToken;
    }

    public ConnectionProxy? Get(ushort connectionId) => _connections.GetValueOrDefault(connectionId);
}