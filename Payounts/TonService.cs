using Newtonsoft.Json;
using Server.Data;
using Server.Data.Interface;
using Server.Logger.Interface;
using Server.Managers;

namespace Server.Network;

public class TonService
{
    private readonly ILogger _logger;
    private readonly ConnectionManager _connectionManager;
    private readonly AccountService _accountService;
    private readonly IDatabaseService _databaseService;
    private readonly HttpClient _httpClient;
    
    private const string WalletAddress = "UQC4bX1rPZCA6YUF6RD2XXQmAX6OnxKvrYeEz-GRQtjtG9Da";
    private const string TonApiKey = "AEYDW4PBNDWMH2IAAAAGCIVBN7OM7VSAQZTSE6KUP5REELSWHMU2JICTKRC5B4LDMWQWCCI";

    public TonService(ILogger logger, ConnectionManager connectionManager, AccountService accountService, IDatabaseService databaseService)
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _accountService = accountService;
        _databaseService = databaseService;
        _httpClient = new HttpClient();

        _logger.Print($"{nameof(TonService)} started.");
        
        Task.Run(PollLoop);
    }

    public string GeneratePayLink(ushort connectionId, float amount, string description = "")
    {
        var client = _connectionManager.Get(connectionId);
        var accountId = client.AccountId;
        
        var payload = "0x" + BitConverter.ToString(BitConverter.GetBytes(connectionId)).Replace("-", "");
        
        TonTopUpQueue.AddPending(connectionId, payload, amount);
        
        return $"ton://transfer/{WalletAddress}?amount={amount}&text={description}&payload={payload}";
    }

    private async Task PollLoop()
    {
        while (true)
        {
            try
            {
                await CheckIncomingTransactions();
            }
            catch (Exception ex)
            {
                _logger.Print($"[TonService] Error: {ex.Message}");
            }

            await Task.Delay(10_000); // 10 сек
        }
    }

    private async Task CheckIncomingTransactions()
    {
        var url = $"https://tonapi.io/v2/blockchain/accounts/{WalletAddress}/transactions?limit=20";

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {TonApiKey}");

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.Print($"[TonService] API Error: {response.StatusCode}");
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<TonApiTransactionResponse>(content);

        foreach (var tx in data!.transactions)
        {
            if (tx.in_msg?.payload == null) continue;

            var payload = tx.in_msg.payload;
            if (TonTopUpQueue.GetAllPending().All(p => p.Payload != payload)) continue;

            var pending = TonTopUpQueue.GetAllPending().First(p => p.Payload == payload);

            TonTopUpQueue.MarkAsPaid(pending.ConnectionId);
            NetworkSend.MessageValidation(pending.ConnectionId, $"Ton {pending.Amount}", EMessageTypes.Success);

            var connection = _connectionManager.Get(pending.ConnectionId);
            _accountService.AddBalance(connection.AccountId, pending.Amount);
            
            _logger.Print($"[TonService] up {pending.Amount} TON by {tx.in_msg.source} -> " +
                          $"{_accountService.GetUsername(connection.AccountId)}");
        }
    }
    
    public class TonApiTransactionResponse
    {
        public List<TonTransaction> transactions { get; set; }
    }

    public class TonTransaction
    {
        public InMessage in_msg { get; set; }
    }

    public class InMessage
    {
        public string source { get; set; }
        public string payload { get; set; }
    }
}