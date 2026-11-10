using Microsoft.Data.Sqlite;

namespace Server.Data;

public class AccountService
{
    private readonly string _connectionString = $"Data Source={Constants.DATABASE_CONNECTION}";
    
    public bool CreateAccount(string email, string password, string deviceId)
    {
        var existAccount = GetAccountIdByEmail(email);

        if (existAccount != null) return false;
        
        var accountId = Guid.NewGuid().ToString();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
            
        var username = $"Guest_{Guid.NewGuid().ToString().Substring(0, 6)}";
        var rawPassword = password + email; 
        var hashedPassword = Hasher.Hash(rawPassword);

        using var insertCommand = connection.CreateCommand();
        
        insertCommand.CommandText = @"
                INSERT INTO Accounts (Id, Email, DeviceId, Username, Password, Balance, Online) 
                VALUES (@id, @email, @deviceId, @username, @password, @balance, @online)";
                
        insertCommand.Parameters.AddWithValue("@id", accountId);
        insertCommand.Parameters.AddWithValue("@email", email);
        insertCommand.Parameters.AddWithValue("@deviceId", deviceId);
        insertCommand.Parameters.AddWithValue("@username", username);
        insertCommand.Parameters.AddWithValue("@password", hashedPassword);
        insertCommand.Parameters.AddWithValue("@balance", 0.0);
        insertCommand.Parameters.AddWithValue("@online", 0);

        insertCommand.ExecuteNonQuery();

        return true;
    }

    // Удаление аккаунта
    public void DeleteAccount(string accountId)
    {
        using var connection = new SqliteConnection(_connectionString);
        
        connection.Open();
        using var command = connection.CreateCommand();
        
        command.CommandText = "DELETE FROM Accounts WHERE Id = @id";
        command.Parameters.AddWithValue("@id", accountId);
                
        command.ExecuteNonQuery();
    }

    // Добавление баланса
    public void AddBalance(string accountId, double amount)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE Accounts SET Balance = Balance + @amount WHERE Id = @id";
                command.Parameters.AddWithValue("@amount", amount);
                command.Parameters.AddWithValue("@id", accountId);
                
                command.ExecuteNonQuery();
            }
        }
    }
    
    public void SetDeviceIdByAccountId(string accountId, string deviceId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE Accounts SET DeviceId = @deviceId WHERE Id = @id";
                command.Parameters.AddWithValue("@deviceId", deviceId);
                command.Parameters.AddWithValue("@id", accountId);

                command.ExecuteNonQuery();
            }
        }
    }

    // Получение баланса
    public double GetBalance(string accountId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Balance FROM Accounts WHERE Id = @id";
                command.Parameters.AddWithValue("@id", accountId);

                var result = command.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToDouble(result) : 0.0;
            }
        }
    }

    public void SetUsernameByAccountId(string accountId, string username)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE Accounts SET Username = @username WHERE Id = @id";
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@id", accountId);
                
                command.ExecuteNonQuery();
            }
        }
    }
    
    public string? GetUsername(string accountId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Username FROM Accounts WHERE Id = @id";
                command.Parameters.AddWithValue("@id", accountId);

                var result = command.ExecuteScalar();
                return result?.ToString();
            }
        }
    }


    public string? GetAccountIdByUsername(string username)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id FROM Accounts WHERE Username = @username";
                command.Parameters.AddWithValue("@username", username);

                var result = command.ExecuteScalar();
                return result?.ToString();
            }
        }
    }

    public string? GetAccountIdByEmail(string email)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id FROM Accounts WHERE Email = @email";
                command.Parameters.AddWithValue("@email", email);

                var result = command.ExecuteScalar();
                return result?.ToString();
            }
        }
    }

    public string? GetAccountIdByPassword(string password, string addEmail)
    {
        var hashedInput = Hasher.Hash(password + addEmail);
        
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id FROM Accounts WHERE Password = @password";
                command.Parameters.AddWithValue("@password", hashedInput);

                var result = command.ExecuteScalar();
                return result?.ToString();
            }
        }
    }

    public string? GetAccountIdByDeviceId(string deviceId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id FROM Accounts WHERE DeviceId = @deviceId";
                command.Parameters.AddWithValue("@deviceId", deviceId);

                var result = command.ExecuteScalar();
                return result?.ToString();
            }
        }
    }

    public string? GetDeviceId(string accountId)
    {
        return null;
    }
}

public class PendingAccount(string email, string password, string code)
{
    public string Email { get; set; } = email;
    public string Password { get; set; } = password;
    public string Code { get; set; } = code;
}