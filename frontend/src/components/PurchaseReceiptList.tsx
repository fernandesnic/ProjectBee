import type { PurchaseReceipt } from '../services/api'
import { formatDateTime } from '../services/date'
import { formatCurrency } from '../services/price'

export function PurchaseReceiptList({ receipts }: { receipts: PurchaseReceipt[] }) {
  if (receipts.length === 0) {
    return <p className="text-sm text-ink-muted">Nenhuma nota recebida ainda.</p>
  }

  return (
    <ol className="divide-y divide-line">
      {receipts.map((receipt) => {
        const total = receipt.items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0)
        return (
          <li key={receipt.id} className="py-4 first:pt-0 last:pb-0">
            <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
              <p className="text-sm font-medium text-ink">
                Nota <span className="font-mono">{receipt.invoiceNumber}</span>
              </p>
              <p className="text-sm font-semibold tabular-nums text-ink">{formatCurrency(total)}</p>
            </div>
            <p className="text-xs text-ink-muted">
              {formatDateTime(receipt.createdAt)}
              <span aria-hidden="true"> · </span>
              <span className="break-all">{receipt.createdBy}</span>
            </p>

            <ul className="mt-2 grid gap-1 text-sm">
              {receipt.items.map((item) => {
                const diverges = item.unitPrice !== item.orderedUnitPrice
                return (
                  <li
                    key={`${item.purchaseOrderItemId}-${item.batch}`}
                    className="flex flex-wrap justify-between gap-x-4 text-ink-muted"
                  >
                    <span>
                      <span className="tabular-nums text-ink">{item.quantity.toLocaleString('pt-BR')}</span> ×{' '}
                      {item.productName} · lote <span className="font-mono">{item.batch}</span>
                    </span>
                    <span className="tabular-nums">
                      {formatCurrency(item.unitPrice)}
                      {diverges && (
                        <span className="ml-1 text-xs text-honey-text">
                          (pedido {formatCurrency(item.orderedUnitPrice)})
                        </span>
                      )}
                    </span>
                  </li>
                )
              })}
            </ul>

            {receipt.priceDivergenceReason && (
              <p className="mt-2 text-xs text-ink-muted">
                Divergência de preço: <span className="italic">“{receipt.priceDivergenceReason}”</span>
              </p>
            )}
          </li>
        )
      })}
    </ol>
  )
}
