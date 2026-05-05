using System.Linq;
using System.Text;
using System.Collections.Generic;

public class Program
{
    public static void Main(string[] args)
    {
        var inputs = new string[]
        {

            "TRY:5000|EUR:300|AZN:150&streamer1:USD:150|streamer2:EUR:100|streamer3:USD:200|streamer4:TRY:1400|streamer4:TRY:110|streamer6:AZN:10|streamer7:RUB:20|streamer16:TRY:8",
            "USD:276|EUR:300|TRY:1100&streamer7:USD:120|streamer2:EUR:112|streamer55:USD:200|streamer4:TRY:1000|streamer5:TRY:375"
        };

        foreach (var input in inputs)
        {
            Console.WriteLine(input);
            var output = CodingChallenge(input);
            Console.WriteLine(output);
        }
    }

    private static string CodingChallenge(string input)
    {
        var (balanceEntries, streamerRequests) = InputParser.Parser(input.AsSpan());

        var currencies = new List<Currency>() { new Currency("TRY", 1), new Currency("USD", 2), new Currency("EUR", 2) };
        var currencyService = new CurrencyService(currencies);
        var paymentProcessor = new PaymentProcessor(currencyService);

        var (accountBalances, payments) = paymentProcessor.Process(balanceEntries, streamerRequests);

        return OutputFormatter.Format(accountBalances, payments);
    }

    public static class InputParser
    {
        private static char groupSeparator = '&';
        private static char itemSeparator = '|';
        private static char valueSeparator = ':';
        public static (List<BalanceEntry> balanceEntries, List<StreamerRequest> streamerRequests) Parser(ReadOnlySpan<char> input)
        {
            int separatorIndex = input.IndexOf(groupSeparator);
            if (separatorIndex == -1) return (new List<BalanceEntry>(), new List<StreamerRequest>());

            return (ParseBalanceEntries(input.Slice(0, separatorIndex)), ParseStreamerRequests(input.Slice(separatorIndex + 1)));
        }

        private static List<BalanceEntry> ParseBalanceEntries(ReadOnlySpan<char> input)
        {
            var response = new List<BalanceEntry>();
            ReadOnlySpan<char> remaining = input;

            while (!remaining.IsEmpty)
            {
                int itemSeparatorIndex = remaining.IndexOf(itemSeparator);

                ReadOnlySpan<char> currentEntry;

                if (itemSeparatorIndex == -1)
                {
                    currentEntry = remaining;
                    remaining = ReadOnlySpan<char>.Empty;
                }
                else
                {
                    currentEntry = remaining.Slice(0, itemSeparatorIndex);
                    remaining = remaining.Slice(itemSeparatorIndex + 1);
                }

                int valueSeparatorIndex = currentEntry.IndexOf(valueSeparator);
                if (valueSeparatorIndex == -1) continue;

                ReadOnlySpan<char> currencySpan = currentEntry.Slice(0, valueSeparatorIndex);
                ReadOnlySpan<char> amountSpan = currentEntry.Slice(valueSeparatorIndex + 1);

                if (!int.TryParse(amountSpan, out var amount)) continue;

                response.Add(new BalanceEntry(currencySpan.ToString(), amount));
            }

            return response;
        }

        private static List<StreamerRequest> ParseStreamerRequests(ReadOnlySpan<char> input)
        {
            var response = new List<StreamerRequest>();

            if (input.IsEmpty) return response;

            ReadOnlySpan<char> remaining = input;

            while (!remaining.IsEmpty)
            {
                int itemSeparatorIndex = remaining.IndexOf(itemSeparator);
                ReadOnlySpan<char> currentEntry;

                if (itemSeparatorIndex == -1)
                {
                    currentEntry = remaining;
                    remaining = ReadOnlySpan<char>.Empty;
                }
                else
                {
                    currentEntry = remaining.Slice(0, itemSeparatorIndex);
                    remaining = remaining.Slice(itemSeparatorIndex + 1);
                }

                if (currentEntry.IsEmpty) continue;

                int firstSeparatorIndex = currentEntry.IndexOf(valueSeparator);
                if (firstSeparatorIndex == -1) continue;

                ReadOnlySpan<char> part0 = currentEntry.Slice(0, firstSeparatorIndex);

                ReadOnlySpan<char> remainingEntry = currentEntry.Slice(firstSeparatorIndex + 1);

                int secondSeparatorIndex = remainingEntry.IndexOf(valueSeparator);
                if (secondSeparatorIndex == -1) continue;

                ReadOnlySpan<char> part1 = remainingEntry.Slice(0, secondSeparatorIndex);
                ReadOnlySpan<char> part2 = remainingEntry.Slice(secondSeparatorIndex + 1);

                if (part2.IndexOf(valueSeparator) != -1) continue;

                if (!int.TryParse(part2, out var amount) || amount <= 0) continue;

                response.Add(new StreamerRequest(part0.ToString(), part1.ToString(), amount));
            }

            return response;
        }
    }

    public class PaymentProcessor
    {
        private readonly ICurrencyService _currencyService;
        public PaymentProcessor(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public (List<AccountBalance> accountBalances, List<Payment> payments) Process(IEnumerable<BalanceEntry> balanceEntries, IEnumerable<StreamerRequest> streamerRequests)
        {
            var accountBalances = new Dictionary<string, AccountBalance>(StringComparer.Ordinal);
            foreach (var entry in balanceEntries)
            {
                if (!_currencyService.IsSupported(entry.CurrencyCode)) continue;

                if (accountBalances.TryGetValue(entry.CurrencyCode, out var accountBalance))
                    accountBalance.Increase(entry.Amount);
                else
                    accountBalances[entry.CurrencyCode] = new AccountBalance(entry.CurrencyCode, entry.Amount);
            }

            var payments = new List<Payment>();
            foreach (var request in streamerRequests.OrderBy(q=> q.RequestedAmount))
            {
                if (!_currencyService.IsSupported(request.CurrencyCode)) continue;

                if (!accountBalances.TryGetValue(request.CurrencyCode, out var accountBalance)) continue;

                var fee = _currencyService.CalculateFee(request.CurrencyCode, request.RequestedAmount);

                var actualAmount = request.RequestedAmount - fee;
                if (actualAmount <= 0) continue;

                if (!accountBalance.Decrease(actualAmount)) continue;

                payments.Add(new Payment(request.StreamerId, request.CurrencyCode, actualAmount));
            }

            return (accountBalances.Values.ToList(), payments);
        }
    }

    public static class OutputFormatter
    {
        public static string Format(List<AccountBalance> accountBalances, List<Payment> payments)
        {
            var balanceParts = accountBalances
            .OrderBy(c => c.CurrencyCode, StringComparer.Ordinal)
            .Select(c => $"{c.CurrencyCode}:{c.Amount}");

            var balancePart = string.Join('|', balanceParts);

            var paymentParts = payments
            .OrderBy(c => c.CurrencyCode, StringComparer.Ordinal)
            .ThenBy(q => q.AmountToPay)
            .Select(c => $"{c.StreamerId}:{c.CurrencyCode}:{c.AmountToPay}");

            var paymentPart = string.Join('|', paymentParts);

            var sb = new StringBuilder();
            sb.Append(balancePart).Append("&").Append(paymentPart);
            return sb.ToString();
        }
    }


    public interface ICurrencyService
    {
        public bool IsSupported(string currencyCode);
        public int CalculateFee(string currencyCode, int requestedAmount);
    }

    public class CurrencyService : ICurrencyService
    {
        private readonly Dictionary<string, Currency> _currencies;

        public CurrencyService(List<Currency> currencies)
        {
            _currencies = currencies.ToDictionary(k => k.Code, v => new Currency(v.Code, v.ProcessingFee), StringComparer.Ordinal);
        }
        public bool IsSupported(string currencyCode)
        {
            return _currencies.ContainsKey(currencyCode);
        }
        public int CalculateFee(string currencyCode, int requestedAmount)
        {
            if (!_currencies.TryGetValue(currencyCode, out var fee)) return 0;

            return fee.ProcessingFee;
        }
    }

    public class AccountBalance
    {
        public string CurrencyCode { get; }
        public int Amount { get; private set; }
        public AccountBalance(string currencyCode, int amount)
        {
            CurrencyCode = currencyCode;
            Amount = amount;
        }
        public bool Decrease(int amount)
        {
            if (Amount < amount) return false;

            Amount -= amount;
            return true;
        }
        public void Increase(int amount)
        {
            Amount += amount;
        }
    }

    public record Currency(string Code, int ProcessingFee);
    public record BalanceEntry(string CurrencyCode, int Amount);
    public record StreamerRequest(string StreamerId, string CurrencyCode, int RequestedAmount);
    public record Payment(string StreamerId, string CurrencyCode, int AmountToPay);
}