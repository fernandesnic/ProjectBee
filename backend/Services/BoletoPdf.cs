using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProjectBee.Services;

public record BoletoPdfData(
    int SalesOrderNumber,
    string CustomerName,
    string CustomerDocument,
    string OurNumber,
    string DigitableLine,
    decimal Amount,
    DateOnly DueDate,
    DateTime CreatedAt,
    DateTime? PaidAt);

// Boleto em PDF no layout da ficha de compensação: recibo do pagador em cima, linha de corte e a
// ficha com o código de barras embaixo. A marca "simulação" atravessa a página: o título não
// está registrado em banco nenhum, e o PDF não pode ser confundido com um boleto pagável.
public static class BoletoPdf
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private const string Beneficiary = "ProjectBee (empresa de demonstração)";
    private const string PaymentPlace = "Documento de demonstração. Não pague este boleto: ele não está registrado em banco.";

    private const string Line = "#000000";
    private const string Muted = "#555555";

    public static byte[] Generate(BoletoPdfData data) =>
        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(text => text.FontSize(8).FontColor(Line));

                // No fundo, não na frente: por cima, a marca apagaria os rótulos dos campos.
                page.Background()
                    .AlignCenter().AlignMiddle()
                    .Rotate(-30)
                    .Text("SIMULAÇÃO — SEM VALOR")
                    .FontSize(40).Bold().FontColor("#E5E5E5");

                page.Content().Column(column =>
                {
                    column.Spacing(6);
                    column.Item().Element(c => Receipt(c, data));
                    column.Item().PaddingVertical(10).Element(CutLine);
                    column.Item().Element(c => CompensationSlip(c, data));
                });

                page.Footer().AlignCenter()
                    .Text($"Gerado pelo ProjectBee em {BrazilTime.FromUtc(DateTime.UtcNow).ToString("dd/MM/yyyy HH:mm", PtBr)}")
                    .FontSize(7).FontColor(Muted);
            });
        }).GeneratePdf();

    private static void Receipt(IContainer container, BoletoPdfData data) =>
        container.Column(column =>
        {
            column.Item().Element(c => Header(c, data, digitableLineSize: 10));
            column.Item().Text("Recibo do pagador").FontSize(7).FontColor(Muted);

            column.Item().Row(row =>
            {
                Field(row.RelativeItem(4), "Beneficiário", Beneficiary);
                Field(row.RelativeItem(2), "Vencimento", Date(data.DueDate), bold: true);
            });
            column.Item().Row(row =>
            {
                Field(row.RelativeItem(4), "Pagador", Payer(data));
                Field(row.RelativeItem(2), "Nosso número", data.OurNumber);
            });
            column.Item().Row(row =>
            {
                Field(row.RelativeItem(2), "Nº do documento", SalesService.Code(data.SalesOrderNumber));
                Field(row.RelativeItem(2), "Data do documento", Date(BrazilTime.DateOf(data.CreatedAt)));
                Field(row.RelativeItem(2), "(=) Valor do documento", Money(data.Amount), bold: true);
            });

            if (data.PaidAt is { } paidAt)
                column.Item().PaddingTop(4)
                    .Text($"PAGO em {BrazilTime.FromUtc(paidAt).ToString("dd/MM/yyyy 'às' HH:mm", PtBr)} (pagamento simulado)")
                    .FontSize(9).Bold();
        });

    private static void CutLine(IContainer container) =>
        container.Column(column =>
        {
            column.Item().LineHorizontal(0.5f).LineColor(Muted).LineDashPattern([4f, 3f]);
            column.Item().AlignRight().Text("Corte na linha pontilhada").FontSize(6).FontColor(Muted);
        });

    private static void CompensationSlip(IContainer container, BoletoPdfData data) =>
        container.Column(column =>
        {
            column.Item().Element(c => Header(c, data, digitableLineSize: 11));

            column.Item().Row(row =>
            {
                Field(row.RelativeItem(4), "Local de pagamento", PaymentPlace);
                Field(row.RelativeItem(2), "Vencimento", Date(data.DueDate), bold: true, alignRight: true);
            });
            column.Item().Row(row =>
            {
                Field(row.RelativeItem(4), "Beneficiário", Beneficiary);
                Field(row.RelativeItem(2), "Agência / Código do beneficiário", $"0000 / {BoletoGenerator.Agreement}", alignRight: true);
            });
            column.Item().Row(row =>
            {
                Field(row.RelativeItem(1), "Data do documento", Date(BrazilTime.DateOf(data.CreatedAt)));
                Field(row.RelativeItem(1), "Nº do documento", SalesService.Code(data.SalesOrderNumber));
                Field(row.RelativeItem(0.6f), "Espécie doc.", "DM");
                Field(row.RelativeItem(0.4f), "Aceite", "N");
                Field(row.RelativeItem(1), "Data processamento", Date(BrazilTime.DateOf(data.CreatedAt)));
                Field(row.RelativeItem(2), "Nosso número", data.OurNumber, alignRight: true);
            });
            column.Item().Row(row =>
            {
                Field(row.RelativeItem(1), "Uso do banco", "");
                Field(row.RelativeItem(1), "Carteira", "17");
                Field(row.RelativeItem(0.6f), "Espécie", "R$");
                Field(row.RelativeItem(0.4f), "Qtde.", "");
                Field(row.RelativeItem(1), "Valor", "");
                Field(row.RelativeItem(2), "(=) Valor do documento", Money(data.Amount), bold: true, alignRight: true);
            });
            column.Item().Row(row =>
            {
                row.RelativeItem(4).Border(0.5f).BorderColor(Line).Padding(3).Column(instructions =>
                {
                    instructions.Item().Text("Instruções (texto de responsabilidade do beneficiário)").FontSize(6).FontColor(Muted);
                    instructions.Item().Text($"Referente ao pedido de venda {SalesService.Code(data.SalesOrderNumber)}.");
                    instructions.Item().Text("Boleto gerado por simulação no ProjectBee, sem registro bancário.");
                    instructions.Item().Text("O pagamento é confirmado pela opção Simular pagamento do pedido.");
                });
                row.RelativeItem(2).Column(amounts =>
                {
                    Field(amounts.Item(), "(−) Desconto / Abatimento", "");
                    Field(amounts.Item(), "(+) Mora / Multa", "");
                    Field(amounts.Item(), "(=) Valor cobrado", "");
                });
            });
            column.Item().Element(c => Field(c, "Pagador", Payer(data)));

            column.Item().PaddingTop(8).Row(row =>
            {
                // 103 x 13 mm: a medida do código de barras do boleto na especificação FEBRABAN.
                row.ConstantItem(103, Unit.Millimetre).Height(13, Unit.Millimetre)
                    .Svg(Itf25Barcode.ToSvg(BoletoGenerator.BarcodeFromDigitableLine(data.DigitableLine), 103.0 / 13.0));
                row.RelativeItem().AlignRight().Column(auth =>
                {
                    auth.Item().AlignRight().Text("Autenticação mecânica").FontSize(6).FontColor(Muted);
                    auth.Item().AlignRight().Text("Ficha de Compensação").Bold();
                });
            });
        });

    private static void Header(IContainer container, BoletoPdfData data, float digitableLineSize) =>
        container.BorderBottom(1.5f).BorderColor(Line).PaddingBottom(3).Row(row =>
        {
            row.ConstantItem(110).AlignBottom().Text("ProjectBee").FontSize(13).Bold();
            row.ConstantItem(52).BorderLeft(1.5f).BorderRight(1.5f).BorderColor(Line)
                .AlignCenter().AlignBottom().Text(BoletoGenerator.BankCode).FontSize(13).Bold();
            row.RelativeItem().AlignRight().AlignBottom()
                .Text(BoletoGenerator.Format(data.DigitableLine)).FontSize(digitableLineSize).Bold();
        });

    private static void Field(IContainer container, string label, string value, bool bold = false, bool alignRight = false) =>
        container.Border(0.5f).BorderColor(Line).PaddingHorizontal(3).PaddingVertical(2).Column(column =>
        {
            column.Item().Text(label).FontSize(6).FontColor(Muted);
            var text = alignRight ? column.Item().AlignRight().Text(value) : column.Item().Text(value);
            text.FontSize(bold ? 9 : 8);
            if (bold)
                text.Bold();
        });

    private static string Payer(BoletoPdfData data) => $"{data.CustomerName} — CPF/CNPJ {FormatDocument(data.CustomerDocument)}";

    private static string FormatDocument(string document) => document.Length switch
    {
        11 => $"{document[..3]}.{document[3..6]}.{document[6..9]}-{document[9..]}",
        14 => $"{document[..2]}.{document[2..5]}.{document[5..8]}/{document[8..12]}-{document[12..]}",
        _ => document
    };

    private static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", PtBr);

    // Sem "C", pelo mesmo motivo do SalesService: o espaço depois de "R$" muda entre Windows e Linux.
    private static string Money(decimal value) => $"R$ {value.ToString("N2", PtBr)}";
}
