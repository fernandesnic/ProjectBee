namespace ProjectBee.DTO.Sales;

// RowVersion: a versão do pedido lida antes de pagar, para não pagar com base numa tela velha.
public record PayBoletoDTO(byte[]? RowVersion);
