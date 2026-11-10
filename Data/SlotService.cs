using Microsoft.Data.Sqlite;
using Server.Slots;

namespace Server.Data;

public class SlotService
{
    private readonly string _connectionString = $"Data Source={Constants.DATABASE_CONNECTION}";
    
    public void CreateSlotWithSymbols(SlotParameters parameters)
    {
        if (GetIdByName(parameters.Name!) != null)
        {
            return;
        }
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        // 1. Добавляем слот
        var insertSlotCmd = connection.CreateCommand();
        insertSlotCmd.CommandText = @"
        INSERT INTO Slots (Id, Name, Rtp, Columns, Rows, BetMin, BetMax, Bonus)
        VALUES (@id, @name, @rtp, @columns, @rows, @betMin, @betMax, @bonus)";
    
        insertSlotCmd.Parameters.AddWithValue("@id", parameters.Id);
        insertSlotCmd.Parameters.AddWithValue("@name", parameters.Name);
        insertSlotCmd.Parameters.AddWithValue("@rtp", parameters.Rtp);
        insertSlotCmd.Parameters.AddWithValue("@columns", parameters.Columns);
        insertSlotCmd.Parameters.AddWithValue("@rows", parameters.Rows);
        insertSlotCmd.Parameters.AddWithValue("@betMin", parameters.BetMin);
        insertSlotCmd.Parameters.AddWithValue("@betMax", parameters.BetMax);
        insertSlotCmd.Parameters.AddWithValue("@bonus", parameters.Bonus);

        insertSlotCmd.ExecuteNonQuery();
        
        foreach (var symbol in parameters.Symbols)
        {
            var insertSymbolCmd = connection.CreateCommand();
            insertSymbolCmd.CommandText = @"
            INSERT INTO SlotSymbols (SlotId, Symbol, Weight, Payout)
            VALUES (@slotId, @symbol, @weight, @payout)";

            insertSymbolCmd.Parameters.AddWithValue("@slotId", parameters.Id);
            insertSymbolCmd.Parameters.AddWithValue("@symbol", symbol.Symbol);
            insertSymbolCmd.Parameters.AddWithValue("@weight", symbol.Weight);
            insertSymbolCmd.Parameters.AddWithValue("@payout", symbol.Payout);

            insertSymbolCmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }
    
    public string? GetIdByName(string name)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        
        command.CommandText = "SELECT Id FROM Slots WHERE Name = @name";
        command.Parameters.AddWithValue("@name", name);

        var result = command.ExecuteScalar();
        return result?.ToString();
    }

    public string[] GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Slots";
        
        using var reader = command.ExecuteReader();
        var result = new string[reader.FieldCount];

        return result;
    }
    
    public SlotParameters GetParametersById(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Slots WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null!;

        var symbols = GetSymbolsForSlot(id);
        
        var parameters = new SlotParameters(
            id: reader["Id"].ToString(),
            name: reader["Name"].ToString(),
            symbols: symbols
        )
        {
            Type = Convert.ToString(reader["Type"]),
            Rtp     = Convert.ToDouble(reader["Rtp"]),
            Columns = Convert.ToInt32(reader["Columns"]),
            Rows    = Convert.ToInt32(reader["Rows"]),
            BetMin  = Convert.ToDouble(reader["BetMin"]),
            BetMax  = Convert.ToDouble(reader["BetMax"]),
            Bonus   = Convert.ToDouble(reader["Bonus"])
        };

        return parameters;
    }
    
    private List<SlotSymbol> GetSymbolsForSlot(string slotId)
    {
        var symbols = new List<SlotSymbol>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Symbol, Weight, Payout FROM SlotSymbols WHERE SlotId = @slotId";
        command.Parameters.AddWithValue("@slotId", slotId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            symbols.Add(new SlotSymbol
            {
                Symbol = reader["Symbol"].ToString(),
                Weight = Convert.ToDouble(reader["Weight"]),
                Payout = Convert.ToDouble(reader["Payout"])
            });
        }

        return symbols;
    }
}