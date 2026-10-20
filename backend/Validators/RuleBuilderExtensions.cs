using FluentValidation;

public static class RuleBuilderExtensions
{
    // Mesmo formato em movimentos e recebimentos de compra: o lote é parte da chave do saldo.
    public static IRuleBuilderOptions<T, string> MustBeValidBatch<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("O lote é obrigatório.")
            .Matches("^[A-Z0-9-]{1,30}$")
            .WithMessage("O lote deve conter apenas letras maiúsculas, números e hífen (até 30 caracteres).");
}
