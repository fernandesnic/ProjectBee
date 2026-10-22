using System.Text;

namespace ProjectBee.Services;

// Gera a linha digitável de um boleto simulado no layout FEBRABAN (banco 001, carteira 17,
// convênio de 7 dígitos). Os dígitos verificadores são calculados de verdade, então a linha
// passa na validação de um leitor de boleto, mas o título não está registrado em banco nenhum.
public static class BoletoGenerator
{
    private const string Bank = "001";
    private const string Currency = "9";
    private const string Agreement = "1234567";
    private const string Wallet = "17";

    // Desde 22/02/2025 o fator de vencimento recomeça em 1000 (o de 1997 estourou os 4 dígitos).
    private static readonly DateOnly DueFactorBase = new(2025, 2, 22);

    // Valor vai em centavos em 10 dígitos: acima disso o boleto não comporta.
    public const decimal MaxAmount = 99_999_999.99m;

    public static string OurNumber(int salesOrderNumber) => $"{Agreement}{salesOrderNumber:D10}";

    public static string DigitableLine(string ourNumber, decimal amount, DateOnly dueDate)
    {
        var factor = (1000 + (dueDate.DayNumber - DueFactorBase.DayNumber)).ToString("D4");
        var cents = ((long)Math.Round(amount * 100)).ToString("D10");
        var freeField = $"000000{ourNumber}{Wallet}"; // 25 posições

        // Código de barras (44): banco, moeda, DV geral, fator, valor e campo livre.
        var withoutDv = $"{Bank}{Currency}{factor}{cents}{freeField}";
        var generalDv = Modulo11(withoutDv);

        var field1 = $"{Bank}{Currency}{freeField[..5]}";
        var field2 = freeField[5..15];
        var field3 = freeField[15..];

        return new StringBuilder()
            .Append(field1).Append(Modulo10(field1))
            .Append(field2).Append(Modulo10(field2))
            .Append(field3).Append(Modulo10(field3))
            .Append(generalDv)
            .Append(factor).Append(cents)
            .ToString();
    }

    // 47 dígitos agrupados como o banco imprime: AAAAA.AAAAA BBBBB.BBBBBB CCCCC.CCCCCC D FFFFVVVVVVVVVV
    public static string Format(string digitableLine) =>
        $"{digitableLine[..5]}.{digitableLine[5..10]} " +
        $"{digitableLine[10..15]}.{digitableLine[15..21]} " +
        $"{digitableLine[21..26]}.{digitableLine[26..32]} " +
        $"{digitableLine[32]} {digitableLine[33..]}";

    // Pesos 2,1,2,1... da direita para a esquerda; produto com dois dígitos soma os dígitos.
    private static int Modulo10(string digits)
    {
        var sum = 0;
        var weight = 2;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var product = (digits[i] - '0') * weight;
            sum += product > 9 ? product - 9 : product;
            weight = weight == 2 ? 1 : 2;
        }
        return (10 - sum % 10) % 10;
    }

    // Pesos 2 a 9 da direita para a esquerda; resultado 0, 10 ou 11 vira 1.
    private static int Modulo11(string digits)
    {
        var sum = 0;
        var weight = 2;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            sum += (digits[i] - '0') * weight;
            weight = weight == 9 ? 2 : weight + 1;
        }
        var dv = 11 - sum % 11;
        return dv is 0 or 10 or 11 ? 1 : dv;
    }
}
