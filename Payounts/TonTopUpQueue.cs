namespace Server.Network;

public static class TonTopUpQueue
{
    private static readonly Dictionary<string, PendingTopUp> Pending = new();

    public static void AddPending(ushort connectionId, string payload, float amount)
    {
        var topUp = new PendingTopUp
        {
            ConnectionId = connectionId,
            Payload = payload,
            Amount = amount,
            Timestamp = DateTime.UtcNow
        };

        Pending[payload] = topUp;
    }

    public static List<PendingTopUp> GetAllPending()
    {
        return Pending.Values.ToList();
    }

    public static void MarkAsPaid(ushort connectionId)
    {
        var entry = Pending.Values.FirstOrDefault(p => p.ConnectionId == connectionId);
        if (entry != null)
        {
            Pending.Remove(entry.Payload);
        }
    }

    public class PendingTopUp
    {
        public ushort ConnectionId;
        public string Payload;
        public float Amount;
        public DateTime Timestamp;
    }
}