import {
  PURCHASE_STATUS_LABEL,
  SALES_STATUS_LABEL,
  type PurchaseOrderStatus,
  type SalesOrderStatus,
} from '../services/api'

const SALES_STATUS_CLASS: Record<SalesOrderStatus, string> = {
  AwaitingPayment: 'bg-honey-light/60 text-ink',
  Paid: 'bg-honey/15 text-honey-text',
  Invoiced: 'bg-ink/10 text-ink',
}

export function SalesStatusBadge({ status }: { status: SalesOrderStatus }) {
  return (
    <span className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-medium ${SALES_STATUS_CLASS[status]}`}>
      {SALES_STATUS_LABEL[status]}
    </span>
  )
}

const PURCHASE_STATUS_CLASS: Record<PurchaseOrderStatus, string> = {
  Open: 'bg-honey/15 text-honey-text',
  Partial: 'bg-honey-light/60 text-ink',
  Received: 'bg-ink/10 text-ink',
  Cancelled: 'bg-line/60 text-ink-muted',
}

export function PurchaseStatusBadge({ status }: { status: PurchaseOrderStatus }) {
  const label = PURCHASE_STATUS_LABEL[status]
  const className = PURCHASE_STATUS_CLASS[status]
  return (
    <span className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-medium ${className}`}>
      {label}
    </span>
  )
}

export function StatusBadge({ isActive }: { isActive: boolean }) {
  return (
    <span
      className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-medium ${
        isActive ? 'bg-honey/15 text-honey-text' : 'bg-line/60 text-ink-muted'
      }`}
    >
      {isActive ? 'Ativo' : 'Inativo'}
    </span>
  )
}
