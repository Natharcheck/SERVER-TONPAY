using Server.Data;
using Server.Network;

namespace Server.Slots;

public class SlotsManager
{
    private readonly Dictionary<ushort, SlotMachine> _playerSlots = new();
    private readonly SlotService _slotService;
    private string[] _slots;
    
    
    public SlotsManager(SlotService slotService)
    {
        _slotService = slotService;
        _slots = _slotService.GetAll();
    }

    public void UpdateSlotKeys()
    {
        _slots = _slotService.GetAll();
    }
    
    public void Connect(ushort connectionId, string slotId)
    {
        if (!_playerSlots.ContainsKey(connectionId))
        {
            if (IsSlotAvailable(slotId))
            {
                var parameters = _slotService.GetParametersById(slotId);
                _playerSlots[connectionId] = new SlotMachine(connectionId, slotId, parameters);
            }
        }
    }

    public void Disconnect(ushort connectionId)
    {
        _playerSlots.Remove(connectionId);
    }

    public void Spin(ushort connectionId)
    {
        if (_playerSlots.TryGetValue(connectionId, out var slot))
        {
            var result = slot.Spin();
            slot.Send(result);
        }
    }

    public void SetCountSpinBySlot(ushort connectionId, ushort count)
    {
        if (PlayerSlotExists(connectionId))
        {
            _playerSlots[connectionId].SpinCount = count;
        }
    }

    private bool PlayerSlotExists(ushort connectionId)
    {
        return _playerSlots.ContainsKey(connectionId);
    }
    
    private bool IsSlotAvailable(string slotId)
    {
        foreach (var slot in _slots)
        {
            if (slot == slotId)
            {
                return true;
            }
        }
        
        return false;
    }
}

public enum SlotType
{
    Lines,
    Match3,
    MatchLengthRow
}

public class SlotMachine
{
    public readonly ushort ConnectionId;
    public readonly string SlotId;
    public ushort SpinCount { get; set; } = 1;
    
    private readonly SlotParameters _parameters;
    private readonly Random _random = new();
    
    public SlotMachine(ushort connectionId, string slotId, SlotParameters parameters)
    {
        ConnectionId = connectionId;
        SlotId = slotId;
        
        _parameters = parameters;
    }

    public SlotResult Spin()
    {
        var grid = new char[_parameters.Rows, _parameters.Columns];
        var result = new SlotResult { Grid = grid };
        var totalWin = 0.0;

        var symbolPool = CreateSymbolPool(_parameters.Symbols);

        // Заполняем сетку
        for (int row = 0; row < _parameters.Rows; row++)
        for (int col = 0; col < _parameters.Columns; col++)
            grid[row, col] = symbolPool[_random.Next(symbolPool.Count)];

        // Проверяем выигрышные линии
        foreach (var payline in _parameters.Paylines)
        {
            if (payline.Positions.Count < 2) continue;

            var firstPos = payline.Positions[0];
            var firstSymbol = grid[firstPos.Row, firstPos.Col];
            bool isWin = true;

            foreach (var pos in payline.Positions.Skip(1))
            {
                if (grid[pos.Row, pos.Col] != firstSymbol)
                {
                    isWin = false;
                    break;
                }
            }

            if (isWin)
            {
                var payout = _parameters.Symbols.FirstOrDefault(s => s.Symbol == firstSymbol.ToString())?.Payout ?? 0.0;
                totalWin += payout;

                result.WinningLines.Add(new WinningLine
                {
                    RowIndex = -1, // не по строке, а по Payline Id
                    Symbol = firstSymbol,
                    Payout = payout,
                });
            }
        }

        result.WinAmount = totalWin;
        return result;
    }

    private List<char> CreateSymbolPool(List<SlotSymbol> symbols)
    {
        var pool = new List<char>();

        foreach (var symbol in symbols)
        {
            var count = (int)(symbol.Weight * 100); // Пример: 0.3 вес = 30 символов
            for (var i = 0; i < count; i++)
                pool.Add(symbol.Symbol[0]); // Предполагаем, что символ всегда один (вишенка — "C", лимон — "L" и т.д.)
        }

        return pool;
    }
    
    public double SimulateRtp(int spinCount, double betAmount)
    {
        var totalWin = 0.0;

        for (int i = 0; i < spinCount; i++)
        {
            var result = Spin();
            totalWin += result.WinAmount;
        }

        var rtp = (totalWin / (spinCount * betAmount)) * 100.0;
        Console.WriteLine($"{rtp}%");
        return rtp;
    }

    public void Send(SlotResult slotResult)
    {
        NetworkSend.SendResultSlot(ConnectionId, slotResult);
    }
}

public class SlotResult
{
    public char[,] Grid { get; set; }
    public double WinAmount { get; set; }
    public List<WinningLine> WinningLines { get; set; } = new();

    public void Print()
    {
        for (int row = 0; row < Grid.GetLength(0); row++)
        {
            string line = "";
            for (int col = 0; col < Grid.GetLength(1); col++)
            {
                line += Grid[row, col] + " ";
            }
            //Debug.Log(line);
        }

        //Debug.Log("Выигрыш: " + WinAmount);

        foreach (var win in WinningLines)
        {
            //Debug.Log($"Линия {win.RowIndex}: '{win.Symbol}' выплата: {win.Payout}");
        }
    }
}


public class SlotParameters(string? id, string? name, List<SlotSymbol> symbols)
{
    public string? Type { get; set; }
    public string? Id { get; } = id;
    public string? Name { get; } = name;

    public List<SlotSymbol> Symbols { get; set; } = symbols;
    public List<Payline> Paylines { get; set; } = new(); // <--- вот это

    public Dictionary<char, double> SymbolChance { get; set; }

    public double Rtp { get; set; }
    public int Columns { get; set; }
    public int Rows { get; set; }
    public double BetMin { get; set; }
    public double BetMax { get; set; }
    public double Bonus { get; set; }
}

public class SlotSymbol()
{
    public string Symbol { get; set; }
    public double Weight { get; set; } 
    public double Payout { get; set; } 
}

public class WinningLine
{
    public int RowIndex { get; set; }
    public char Symbol { get; set; }
    public double Payout { get; set; }
}

public class Payline
{
    public int Id { get; set; }
    public List<(int Row, int Col)> Positions { get; set; } = new();
}