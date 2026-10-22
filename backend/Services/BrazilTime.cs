namespace ProjectBee.Services;

// O banco grava em UTC, mas datas de documento (vencimento, emissão do boleto) são do dia em Brasília:
// em UTC, um pedido das 21h já cairia no dia seguinte.
public static class BrazilTime
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateTime FromUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public static DateOnly DateOf(DateTime utc) => DateOnly.FromDateTime(FromUtc(utc));
}
