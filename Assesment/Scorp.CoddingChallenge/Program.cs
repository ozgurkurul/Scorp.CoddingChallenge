using System.Collections.Concurrent;
using System.Text;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var examples = new[]
        {
            "TRY:5000|EUR:300|AZN:150&streamer1:USD:150|streamer2:EUR:100|streamer3:USD:200|streamer4:TRY:1400|streamer4:TRY:110|streamer6:AZN:10|streamer7:RUB:20|streamer16:TRY:8",
            "USD:276|EUR:300|TRY:1100&streamer7:USD:120|streamer2:EUR:112|streamer55:USD:200|streamer4:TRY:1000|streamer5:TRY:375",
        };

        foreach (var input in examples)
        {
            Console.WriteLine($"Input : {input}");
            var output = await CodingChallenge(input);
            Console.WriteLine($"Output: {output}");
            Console.WriteLine();
        }
    }

    public static async Task<string> CodingChallenge(string str)
    {
        var (balanceEntries, paymentRequests) = PaymentParser.Parse(str);

        var paymentProcessor = new PaymentProcessor(new DefaultCurrencyService());
        var (balances, paidByCurrency) = await paymentProcessor.Process(balanceEntries, paymentRequests);

        return PaymentFormatter.Format(balances, paidByCurrency);
    }

    public static class PaymentParser
    {
        private const char SectionSeparator = '&';
        private const char EntrySeparator = '|';
        private const char FieldSeparator = ':';

        public static (List<BalanceEntry> balances, List<PaymentRequest> payments) Parse(string input)
        {
            var sections = input.Split(SectionSeparator);
            var balances = ParseBalances(sections[0]);
            var payments = sections.Length > 1 ? ParsePayments(sections[1]) : new List<PaymentRequest>();
            return (balances, payments);
        }

        private static List<BalanceEntry> ParseBalances(string section)
        {
            var result = new List<BalanceEntry>();
            if (string.IsNullOrEmpty(section)) return result;

            foreach (var entry in section.Split(EntrySeparator))
            {
                var parts = entry.Split(FieldSeparator);
                result.Add(new BalanceEntry(parts[0], int.Parse(parts[1])));
            }
            return result;
        }

        private static List<PaymentRequest> ParsePayments(string section)
        {
            var result = new List<PaymentRequest>();
            if (string.IsNullOrEmpty(section)) return result;

            foreach (var entry in section.Split(EntrySeparator))
            {
                var parts = entry.Split(FieldSeparator);
                result.Add(new PaymentRequest(parts[0], parts[1], int.Parse(parts[2])));
            }
            return result;
        }
    }

    public class PaymentProcessor
    {

        private readonly ICurrencyService _currencyService;

        public PaymentProcessor(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public async Task<(List<AccountBalance> balances, ConcurrentBag<Payment> paidByCurrency)> Process(IEnumerable<BalanceEntry> balanceEntries, IEnumerable<PaymentRequest> paymentRequests)
        {
            var accountBalances = new Dictionary<string, AccountBalance>(StringComparer.Ordinal);
            foreach (var entry in balanceEntries)
            {
                if (!_currencyService.IsSupported(entry.Currency)) continue;

                accountBalances.TryAdd(entry.Currency, new AccountBalance(entry.Currency, entry.Amount));
            }

            var grouped = paymentRequests
                .Where(p => _currencyService.IsSupported(p.Currency))
                .GroupBy(p => p.Currency);

            var paidPayments = new ConcurrentBag<Payment>();
            Parallel.ForEach(grouped, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (group) =>
            {
                var currency = group.Key;
                if (!accountBalances.ContainsKey(currency)) return;

                if (!accountBalances.TryGetValue(currency, out var balances)) return;

                var eligible = group
                    .Select(p => new Payment(p.StreamerId, p.Currency, p.RequestedAmount - _currencyService.CalculateFee(p.Currency, p.RequestedAmount)))
                    .Where(p => p.ActualAmount > 0)
                    .OrderBy(p => p.ActualAmount)
                    .ToList();

                foreach (var payment in eligible)
                {
                    if (balances.DecreaseAmount(payment.ActualAmount))
                    {
                        paidPayments.Add(payment);
                    }
                }
            });

            return (accountBalances.Values.ToList(), paidPayments);
        }
    }

    public static class PaymentFormatter
    {
        public static string Format(List<AccountBalance> balances, ConcurrentBag<Payment> paidByCurrency)
        {
            var sortedAsCurrencies = balances.OrderBy(c => c.Currency, StringComparer.Ordinal).ToList();

            var balancePart = string.Join("|", sortedAsCurrencies.Select(c => $"{c.Currency}:{c.CurrentAmount}"));

            var paymentItems = new List<string>();
            foreach (var balance in sortedAsCurrencies)
            {
                foreach (var p in paidByCurrency.OrderBy(q => q.ActualAmount).Where(c => c.Currency == balance.Currency))
                {
                    paymentItems.Add($"{p.StreamerId}:{p.Currency}:{p.ActualAmount}");
                }
            }

            var paymentsPart = string.Join("|", paymentItems);

            var sb = new StringBuilder();
            sb.Append(balancePart).Append('&').Append(paymentsPart);
            return sb.ToString();
        }

    }


    public interface ICurrencyService
    {
        public bool IsSupported(string code);
        int CalculateFee(string code, int requestedAmount);
    }

    public class DefaultCurrencyService : ICurrencyService
    {
        private static readonly Dictionary<string, Currency> _supported = new(StringComparer.Ordinal)
        {
            ["TRY"] = new Currency("TRY", 1),
            ["EUR"] = new Currency("EUR", 2),
            ["USD"] = new Currency("USD", 2),
        };

        public bool IsSupported(string code) => _supported.ContainsKey(code);
        public int CalculateFee(string code, int requestedAmount) => _supported[code].ProcessingFee;
    }

    public class AccountBalance
    {
        public string Currency { get; }
        public int CurrentAmount { get; private set; }

        public AccountBalance(string currency, int initialAmount)
        {
            Currency = currency;
            CurrentAmount = initialAmount;
        }

        public bool DecreaseAmount(int amount)
        {
            if (CurrentAmount >= amount)
            {
                CurrentAmount -= amount;
                return true;
            }
            return false;
        }
    }

    public sealed record Currency(string Code, int ProcessingFee);

    public sealed record BalanceEntry(string Currency, int Amount);

    public sealed record PaymentRequest(string StreamerId, string Currency, int RequestedAmount);

    public sealed record Payment(string StreamerId, string Currency, int ActualAmount);
}