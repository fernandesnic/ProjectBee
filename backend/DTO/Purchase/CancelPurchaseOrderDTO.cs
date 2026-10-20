namespace ProjectBee.DTO.Purchase;

// RowVersion: a versão do pedido lida antes de cancelar, para não cancelar com base numa tela velha.
public record CancelPurchaseOrderDTO(string Reason, byte[]? RowVersion);
