using Server.Logger.Interface;

namespace Server.Logger;

public class ConsoleLogger : ILogger
{
    public void Print(string message)
    { 
        Console.WriteLine("[" + GetTimestamp(DateTime.Now) + "]" + " (LOGGER): " + $"{ message}");
    }
    
    private string GetTimestamp(DateTime time)
    {
        return time.ToString("HH:mm:ss");
    }
}