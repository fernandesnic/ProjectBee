using ProjectBee.Services;

namespace ProjectBee.Tests.Services;

public class BoletoPdfTests
{
    static BoletoPdfTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    private static BoletoPdfData Sample(DateTime? paidAt = null)
    {
        var ourNumber = BoletoGenerator.OurNumber(1);
        var due = new DateOnly(2026, 10, 24);
        return new BoletoPdfData(1, "Empório Natural Ltda.", "22333444000150", ourNumber,
            BoletoGenerator.DigitableLine(ourNumber, 4537m, due), 4537m, due,
            new DateTime(2026, 10, 21, 15, 0, 0, DateTimeKind.Utc), paidAt);
    }

    [Fact]
    public void Generates_a_pdf_for_pending_and_paid_boletos()
    {
        var pending = BoletoPdf.Generate(Sample());
        var paid = BoletoPdf.Generate(Sample(new DateTime(2026, 10, 22, 13, 30, 0, DateTimeKind.Utc)));

        Assert.Equal("%PDF"u8.ToArray(), pending[..4]);
        Assert.Equal("%PDF"u8.ToArray(), paid[..4]);

        var output = Environment.GetEnvironmentVariable("PROJECTBEE_BOLETO_PDF_OUT");
        if (!string.IsNullOrEmpty(output))
            File.WriteAllBytes(output, pending);
    }
}
