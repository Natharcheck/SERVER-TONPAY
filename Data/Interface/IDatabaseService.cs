using Microsoft.Data.Sqlite;

namespace Server.Data.Interface;

public interface IDatabaseService
{
    void Initialize(); // создаёт таблицы
    void Execute(string query, params object[] parameters);
    T QuerySingle<T>(string query, Func<SqliteDataReader, T> map, params object[] parameters);
    List<T> Query<T>(string query, Func<SqliteDataReader, T> map, params object[] parameters);
}