using System.Globalization;
using System.Text;

namespace ProjectBee.Services;

// Código de barras "Intercalado 2 de 5" (ITF), o padrão FEBRABAN para boletos. Os dígitos vão
// em pares: o primeiro do par define as larguras das barras, o segundo as dos espaços entre elas.
public static class Itf25Barcode
{
    // Cada dígito tem 5 elementos, dois largos (W) e três estreitos (N).
    private static readonly string[] Patterns =
    [
        "NNWWN", "WNNNW", "NWNNW", "WWNNN", "NNWNW",
        "WNWNN", "NWWNN", "NNNWW", "WNNWN", "NWNWN"
    ];

    private const int Narrow = 1;
    private const int Wide = 3; // a FEBRABAN aceita de 2,25 a 3 vezes a barra estreita

    // Larguras alternadas, começando por barra: início (N N N N), os pares e fim (W N N).
    public static IReadOnlyList<int> Widths(string digits)
    {
        if (digits.Length % 2 != 0 || !digits.All(char.IsAsciiDigit))
            throw new ArgumentException("O ITF codifica uma quantidade par de dígitos.", nameof(digits));

        var widths = new List<int> { Narrow, Narrow, Narrow, Narrow };

        for (var i = 0; i < digits.Length; i += 2)
        {
            var bars = Patterns[digits[i] - '0'];
            var spaces = Patterns[digits[i + 1] - '0'];
            for (var j = 0; j < 5; j++)
            {
                widths.Add(bars[j] == 'W' ? Wide : Narrow);
                widths.Add(spaces[j] == 'W' ? Wide : Narrow);
            }
        }

        widths.AddRange([Wide, Narrow, Narrow]);
        return widths;
    }

    // SVG com uma unidade de largura por módulo: quem desenha estica para o tamanho impresso.
    public static string ToSvg(string digits, int height = 50)
    {
        var widths = Widths(digits);
        var total = widths.Sum();
        var svg = new StringBuilder()
            .Append(CultureInfo.InvariantCulture,
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {total} {height}\" preserveAspectRatio=\"none\">");

        var x = 0;
        for (var i = 0; i < widths.Count; i++)
        {
            if (i % 2 == 0)
                svg.Append(CultureInfo.InvariantCulture,
                    $"<rect x=\"{x}\" y=\"0\" width=\"{widths[i]}\" height=\"{height}\" fill=\"#000\"/>");
            x += widths[i];
        }

        return svg.Append("</svg>").ToString();
    }
}
