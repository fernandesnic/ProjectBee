import { Link } from 'react-router-dom'
import { formatDocument, invoiceCode, salesOrderCode, type Invoice } from '../services/api'
import { formatDateTime } from '../services/date'
import { formatCurrency } from '../services/price'

export function InvoiceList({ invoices }: { invoices: Invoice[] }) {
  if (invoices.length === 0) {
    return <p className="text-sm text-ink-muted">Nenhuma nota emitida ainda.</p>
  }

  return (
    <ol className="divide-y divide-line">
      {invoices.map((invoice) => (
        <li key={invoice.id} className="py-4 first:pt-0 last:pb-0">
          <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
            <p className="text-sm font-medium text-ink">
              Nota <span className="font-mono">{invoiceCode(invoice.number)}</span>
              <span className="font-normal text-ink-muted"> · {invoice.customerName}</span>
            </p>
            <p className="text-sm font-semibold tabular-nums text-ink">{formatCurrency(invoice.total)}</p>
          </div>
          <p className="text-xs text-ink-muted">
            <span className="tabular-nums">{formatDocument(invoice.customerDocument)}</span>
            <span aria-hidden="true"> · </span>
            <Link
              to={`/app/sales/${invoice.salesOrderId}`}
              className="font-mono underline-offset-2 hover:text-honey-dark hover:underline"
            >
              {salesOrderCode(invoice.salesOrderNumber)}
            </Link>
            <span aria-hidden="true"> · </span>
            {formatDateTime(invoice.createdAt)}
            <span aria-hidden="true"> · </span>
            <span className="break-all">{invoice.createdBy}</span>
          </p>

          <ul className="mt-2 grid gap-1 text-sm">
            {invoice.items.map((item) => (
              <li
                key={`${item.productId}-${item.batch}`}
                className="flex flex-wrap justify-between gap-x-4 text-ink-muted"
              >
                <span>
                  <span className="tabular-nums text-ink">{item.quantity.toLocaleString('pt-BR')}</span> ×{' '}
                  {item.productName} · lote <span className="font-mono">{item.batch}</span>
                </span>
                <span className="tabular-nums">{formatCurrency(item.quantity * item.unitPrice)}</span>
              </li>
            ))}
          </ul>
          <p className="mt-1 text-xs text-ink-muted">Saída de {invoice.storageName}</p>
        </li>
      ))}
    </ol>
  )
}
