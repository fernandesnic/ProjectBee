import { Link } from 'react-router-dom'
import { purchaseOrderCode, type PurchaseOrderSummary } from '../services/api'
import { formatDateTime } from '../services/date'
import { formatCurrency } from '../services/price'
import { PurchaseStatusBadge } from './StatusBadge'

interface PurchaseOrderTableProps {
  orders: PurchaseOrderSummary[]
}

function Progress({ order }: { order: PurchaseOrderSummary }) {
  return (
    <span className="tabular-nums">
      {order.receivedUnits.toLocaleString('pt-BR')} / {order.orderedUnits.toLocaleString('pt-BR')}
    </span>
  )
}

export function PurchaseOrderTable({ orders }: PurchaseOrderTableProps) {
  if (orders.length === 0) {
    return <p className="text-sm text-ink-muted">Nenhum pedido de compra neste filtro.</p>
  }

  return (
    <>
      {/* Mobile: um card por pedido, o card inteiro abre o detalhe */}
      <div className="flex flex-col gap-3 sm:hidden">
        {orders.map((order) => (
          <Link
            key={order.id}
            to={`/app/purchases/${order.id}`}
            className="block rounded-xl border border-line bg-cream p-4 transition hover:border-honey-dark"
          >
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="font-mono text-sm font-medium text-ink">{purchaseOrderCode(order.number)}</p>
                <p className="truncate text-sm text-ink">{order.supplierName}</p>
              </div>
              <PurchaseStatusBadge status={order.status} />
            </div>
            <dl className="mt-3 grid grid-cols-2 gap-2 text-sm">
              <div>
                <dt className="text-xs text-ink-muted">Total</dt>
                <dd className="tabular-nums font-medium text-ink">{formatCurrency(order.total)}</dd>
              </div>
              <div>
                <dt className="text-xs text-ink-muted">Recebido</dt>
                <dd className="text-ink">
                  <Progress order={order} />
                </dd>
              </div>
            </dl>
            <p className="mt-2 text-xs text-ink-muted">
              {order.storageName} · {formatDateTime(order.createdAt)}
            </p>
          </Link>
        ))}
      </div>

      {/* Desktop/tablet: dense table */}
      <div className="hidden overflow-x-auto sm:block">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-line text-ink-muted">
              <th className="py-2 font-medium">Pedido</th>
              <th className="py-2 font-medium">Fornecedor</th>
              <th className="py-2 font-medium">Armazém</th>
              <th className="py-2 font-medium">Criado em</th>
              <th className="py-2 pr-6 text-right font-medium">Recebido</th>
              <th className="py-2 pr-6 text-right font-medium">Total</th>
              <th className="py-2 font-medium">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {orders.map((order) => (
              <tr key={order.id}>
                <td className="py-2">
                  <Link
                    to={`/app/purchases/${order.id}`}
                    className="font-mono font-medium text-ink underline-offset-2 hover:text-honey-dark hover:underline"
                  >
                    {purchaseOrderCode(order.number)}
                  </Link>
                </td>
                <td className="py-2 text-ink">{order.supplierName}</td>
                <td className="py-2 text-ink-muted">{order.storageName}</td>
                <td className="py-2 text-ink-muted">{formatDateTime(order.createdAt)}</td>
                <td className="py-2 pr-6 text-right text-ink">
                  <Progress order={order} />
                </td>
                <td className="py-2 pr-6 text-right tabular-nums text-ink">{formatCurrency(order.total)}</td>
                <td className="py-2">
                  <PurchaseStatusBadge status={order.status} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}
