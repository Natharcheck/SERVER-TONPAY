using Microsoft.Data.Sqlite;
using Server.Data.Interface;
using Server.Logger.Interface;

namespace Server.Data;

public class SqliteDatabaseService
    : IDatabaseService
{
    private readonly string _connectionString;
    private ILogger _logger;
    
    public SqliteDatabaseService(ILogger logger)
    {
        _logger = logger;
        
        _connectionString = $"Data Source={Constants.DATABASE_CONNECTION}";
        
        Initialize();
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            """
             CREATE TABLE IF NOT EXISTS Accounts (
                Id TEXT PRIMARY KEY,
                Username TEXT NOT NULL,
                Password TEXT NOT NULL,
                Balance REAL NOT NULL,
                Online INTEGER NOT NULL
                        );
                    
            CREATE TABLE IF NOT EXISTS Slots (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Rtp REAL NOT NULL,
                Columns INTEGER NOT NULL,
                Raws INTEGER NOT NULL,
                BetMin REAL NOT NULL,
                BetMax REAL NOT NULL,
                Bonus INT NOT NULL
                      );
                  
            """;
        
        command.ExecuteNonQuery();
        
        _logger.Print("SQLITE Database initialized.");
    }

    public void Execute(string query, params object[] parameters)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = query;

        for (int i = 0; i < parameters.Length; i++)
            command.Parameters.AddWithValue($"@p{i}", parameters[i]);

        command.ExecuteNonQuery();
    }

    public T QuerySingle<T>(string query, Func<SqliteDataReader, T> map, params object[] parameters)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = query;

        for (int i = 0; i < parameters.Length; i++)
            command.Parameters.AddWithValue($"@p{i}", parameters[i]);

        using var reader = command.ExecuteReader();
        return reader.Read() ? map(reader) : default;
    }

    public List<T> Query<T>(string query, Func<SqliteDataReader, T> map, params object[] parameters)
    {
        var result = new List<T>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = query;

        for (int i = 0; i < parameters.Length; i++)
            command.Parameters.AddWithValue($"@p{i}", parameters[i]);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(map(reader));
        }

        return result;
    }
}