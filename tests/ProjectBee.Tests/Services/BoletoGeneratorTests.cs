using ProjectBee.Services;

namespace ProjectBee.Tests.Services;

public class BoletoGeneratorTests
{
    [Fact]
    public void Digitable_line_has_47_digits_with_amount_and_due_factor_at_the_end()
    {
        var line = BoletoGenerator.DigitableLine(BoletoGenerator.OurNumber(42), 349.00m, new DateOnly(2025, 2, 22));

        Assert.Equal(47, line.Length);
        Assert.All(line, c => Assert.True(char.IsDigit(c)));
        Assert.StartsWith("0019", line);              // banco 001, moeda 9 (real)
        Assert.EndsWith("1000" + "0000034900", line); // fator 1000 na data-base, valor em centavos
    }

    [Theory]
    [InlineData(1, 10.00, 2026, 10, 24)]
    [InlineData(123456, 98765.43, 2027, 1, 1)]
    [InlineData(7, 0.01, 2025, 3, 1)]
    public void Check_digits_match_the_FEBRABAN_rules(int orderNumber, double amount, int year, int month, int day)
    {
        var line = BoletoGenerator.DigitableLine(BoletoGenerator.OurNumber(orderNumber), (decimal)amount, new DateOnly(year, month, day));

        // Cada um dos três primeiros campos termina num dígito módulo 10 do próprio campo.
        Assert.Equal(Modulo10(line[..9]), line[9] - '0');
        Assert.Equal(Modulo10(line[10..20]), line[20] - '0');
        Assert.Equal(Modulo10(line[21..31]), line[31] - '0');

        // O dígito geral (posição 33) é o módulo 11 do código de barras montado de volta.
        var barcodeWithoutDv = line[..4] + line[33..] + line[4..9] + line[10..20] + line[21..31];
        Assert.Equal(Modulo11(barcodeWithoutDv), line[32] - '0');
    }

    [Fact]
    public void Format_groups_the_line_as_printed_on_the_boleto()
    {
        var line = BoletoGenerator.DigitableLine(BoletoGenerator.OurNumber(1), 10m, new DateOnly(2026, 1, 1));

        var formatted = BoletoGenerator.Format(line);

        Assert.Matches(@"^\d{5}\.\d{5} \d{5}\.\d{6} \d{5}\.\d{6} \d \d{14}$", formatted);
        Assert.Equal(line, formatted.Replace(".", "").Replace(" ", ""));
    }

    [Fact]
    public void Barcode_rebuilt_from_the_line_has_44_digits_and_a_valid_general_check_digit()
    {
        var line = BoletoGenerator.DigitableLine(BoletoGenerator.OurNumber(321), 4537m, new DateOnly(2026, 10, 24));

        var barcode = BoletoGenerator.BarcodeFromDigitableLine(line);

        Assert.Equal(44, barcode.Length);
        Assert.Equal(Modulo11(barcode[..4] + barcode[5..]), barcode[4] - '0');
        Assert.EndsWith(line[33..] + line[4..9] + line[10..20] + line[21..31], barcode);
    }

    [Fact]
    public void Itf_bars_decode_back_to_the_same_digits()
    {
        var line = BoletoGenerator.DigitableLine(BoletoGenerator.OurNumber(321), 4537m, new DateOnly(2026, 10, 24));
        var barcode = BoletoGenerator.BarcodeFromDigitableLine(line);

        var widths = Itf25Barcode.Widths(barcode);

        // Início (4) + 22 pares de 10 elementos + fim (3).
        Assert.Equal(4 + 22 * 10 + 3, widths.Count);
        Assert.Equal([1, 1, 1, 1], widths.Take(4));
        Assert.Equal([3, 1, 1], widths.TakeLast(3));
        Assert.Equal(barcode, DecodeItf(widths));
    }

    [Fact]
    public void Itf_rejects_an_odd_number_of_digits()
    {
        Assert.Throws<ArgumentException>(() => Itf25Barcode.Widths("123"));
    }

    // Leitor independente: cada dígito é o padrão de larguras (2 largas, 3 estreitas) com pesos 1,2,4,7,0.
    private static string DecodeItf(IReadOnlyList<int> widths)
    {
        int[] weights = [1, 2, 4, 7, 0];
        int Digit(IEnumerable<int> elements)
        {
            var value = elements.Select((w, i) => w > 1 ? weights[i] : 0).Sum();
            return value == 11 ? 0 : value;
        }

        var body = widths.Skip(4).Take(widths.Count - 7).ToList();
        var digits = new System.Text.StringBuilder();
        for (var i = 0; i < body.Count; i += 10)
        {
            var pair = body.Skip(i).Take(10).ToList();
            digits.Append(Digit(pair.Where((_, j) => j % 2 == 0)));
            digits.Append(Digit(pair.Where((_, j) => j % 2 == 1)));
        }
        return digits.ToString();
    }

    private static int Modulo10(string digits)
    {
        var sum = digits.Reverse()
            .Select((c, i) => (c - '0') * (i % 2 == 0 ? 2 : 1))
            .Sum(p => p / 10 + p % 10);
        return (10 - sum % 10) % 10;
    }

    private static int Modulo11(string digits)
    {
        var sum = digits.Reverse().Select((c, i) => (c - '0') * (2 + i % 8)).Sum();
        var dv = 11 - sum % 11;
        return dv is 0 or 10 or 11 ? 1 : dv;
    }
}
