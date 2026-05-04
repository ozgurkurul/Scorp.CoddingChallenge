using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        var examples = new[]
        {
            "TRY:5000|EUR:300|AZN:150&streamer1:USD:150|streamer2:EUR:100|streamer3:USD:200|streamer4:TRY:1400|streamer4:TRY:110|streamer6:AZN:10|streamer7:RUB:20|streamer16:TRY:8",
            "USD:276|EUR:300|TRY:1100&streamer7:USD:120|streamer2:EUR:112|streamer55:USD:200|streamer4:TRY:1000|streamer5:TRY:375",
        };

        foreach (var input in examples)
        {
            Console.WriteLine($"Input : {input}"); 
            Console.WriteLine($"Output: {CodingChallenge(input)}");
            Console.WriteLine();
        }
    }
    public static string CodingChallenge(string str)
    {
        var (balanceEntries, paymentRequests) = PaymentParser.Parse(str);
        var (balances, paidByCurrency) = PaymentProcessor.Process(balanceEntries, paymentRequests);
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

    public static class PaymentProcessor
    {
        public static (Dictionary<string, int> balances, Dictionary<string, List<Payment>> paidByCurrency)
            Process(IEnumerable<BalanceEntry> balanceEntries, IEnumerable<PaymentRequest> paymentRequests)
        {
            var balances = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var entry in balanceEntries)
            {
                if (!Currencies.IsSupported(entry.Currency)) continue;
                balances[entry.Currency] = entry.Amount;
            }

            var paidByCurrency = new Dictionary<string, List<Payment>>(StringComparer.Ordinal);

            var grouped = paymentRequests
                .Where(p => Currencies.IsSupported(p.Currency))
                .GroupBy(p => p.Currency);

            foreach (var group in grouped)
            {
                var currency = group.Key;
                if (!balances.ContainsKey(currency)) continue;

                var fee = Currencies.Get(currency).ProcessingFee;

                var eligible = group
                    .Select(p => new Payment(p.StreamerId, p.Currency, p.RequestedAmount - fee))
                    .Where(p => p.ActualAmount > 0)
                    .OrderBy(p => p.ActualAmount)
                    .ToList();

                var paid = new List<Payment>();
                foreach (var payment in eligible)
                {
                    if (balances[currency] < payment.ActualAmount) continue;
                    balances[currency] -= payment.ActualAmount;
                    paid.Add(payment);
                }

                if (paid.Count > 0) paidByCurrency[currency] = paid;
            }

            return (balances, paidByCurrency);
        }
    }


    public static class PaymentFormatter
    {
        public static string Format(Dictionary<string, int> balances, Dictionary<string, List<Payment>> paidByCurrency)
        {
            var sortedCurrencies = balances.Keys.OrderBy(c => c, StringComparer.Ordinal).ToList();

            var balancePart = string.Join("|", sortedCurrencies.Select(c => $"{c}:{balances[c]}"));

            var paymentItems = new List<string>();
            foreach (var currency in sortedCurrencies)
            {
                if (!paidByCurrency.TryGetValue(currency, out var paid)) continue;
                foreach (var p in paid)
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



    public static class Currencies
    {
        private static readonly Dictionary<string, Currency> _supported =
            new(StringComparer.Ordinal)
            {
                ["TRY"] = new Currency("TRY", 1),
                ["EUR"] = new Currency("EUR", 2),
                ["USD"] = new Currency("USD", 2),
            };

        public static bool IsSupported(string code) => _supported.ContainsKey(code);

        public static Currency Get(string code) => _supported[code];
    }
    public sealed record Currency(string Code, int ProcessingFee);
    public sealed record BalanceEntry(string Currency, int Amount);

    public sealed record PaymentRequest(string StreamerId, string Currency, int RequestedAmount);

    public sealed record Payment(string StreamerId, string Currency, int ActualAmount);
}