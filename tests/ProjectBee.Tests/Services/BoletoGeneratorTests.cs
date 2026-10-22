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
