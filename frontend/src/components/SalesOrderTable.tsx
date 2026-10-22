import { Link } from 'react-router-dom'
import { salesOrderCode, type SalesOrderSummary } from '../services/api'
import { formatDateTime } from '../services/date'
import { formatCurrency } from '../services/price'
import { SalesStatusBadge } from './StatusBadge'

export function SalesOrderTable({ orders }: { orders: SalesOrderSummary[] }) {
  if (orders.length === 0) {
    return <p className="text-sm text-ink-muted">Nenhum pedido de venda neste filtro.</p>
  }

  return (
    <>
      {/* Celular e tablet: cartões, um por pedido; o cartão inteiro abre o detalhe */}
      <div className="grid gap-3 sm:grid-cols-2 xl:hidden">
        {orders.map((order) => (
          <Link
            key={order.id}
            to={`/app/sales/${order.id}`}
            className="group block rounded-xl border border-line bg-cream p-4 shadow-sm transition hover:border-honey-dark hover:shadow-md hover:shadow-honey/10 focus-visible:border-honey-dark focus-visible:outline-none active:scale-[0.99]"
          >
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="font-mono text-sm font-medium text-ink">{salesOrderCode(order.number)}</p>
                <p className="truncate text-sm text-ink">{order.customerName}</p>
              </div>
              <SalesStatusBadge status={order.status} />
            </div>
            <dl className="mt-3 grid grid-cols-2 gap-2 text-sm">
              <div>
                <dt className="text-xs text-ink-muted">Total</dt>
                <dd className="tabular-nums font-medium text-ink">{formatCurrency(order.total)}</dd>
              </div>
              <div>
                <dt className="text-xs text-ink-muted">Unidades</dt>
                <dd className="tabular-nums text-ink">{order.units.toLocaleString('pt-BR')}</dd>
              </div>
            </dl>
            {/* Rodapé com chamada explícita: sem ela, o cartão parece só informativo */}
            <div className="mt-3 flex items-center justify-between gap-3 border-t border-line pt-3">
              <p className="min-w-0 truncate text-xs text-ink-muted">
                {order.storageName} · {formatDateTime(order.createdAt)}
              </p>
              <span className="flex shrink-0 items-center gap-1 rounded-full border border-honey-dark/40 bg-honey/15 px-3 py-1.5 text-xs font-semibold text-honey-text transition group-hover:bg-honey group-hover:text-hive">
                Ver pedido
                <span aria-hidden="true" className="transition group-hover:translate-x-0.5">
                  →
                </span>
              </span>
            </div>
          </Link>
        ))}
      </div>

      {/* Desktop: tabela densa */}
      <div className="hidden overflow-x-auto xl:block">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-line text-ink-muted">
              <th className="py-2 font-medium">Pedido</th>
              <th className="py-2 font-medium">Cliente</th>
              <th className="py-2 font-medium">Armazém</th>
              <th className="py-2 font-medium">Criado em</th>
              <th className="py-2 pr-6 text-right font-medium">Unidades</th>
              <th className="py-2 pr-6 text-right font-medium">Total</th>
              <th className="py-2 font-medium">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {orders.map((order) => (
              <tr key={order.id} className="group relative transition hover:bg-cream">
                <td className="py-2">
                  {/* O ::after cobre a linha inteira: clicar em qualquer coluna abre o pedido */}
                  <Link
                    to={`/app/sales/${order.id}`}
                    className="font-mono font-medium text-ink underline-offset-2 outline-none after:absolute after:inset-0 group-hover:text-honey-dark group-hover:underline focus-visible:underline focus-visible:after:ring-2 focus-visible:after:ring-honey-dark"
                  >
                    {salesOrderCode(order.number)}
                  </Link>
                </td>
                <td className="py-2 text-ink">{order.customerName}</td>
                <td className="py-2 text-ink-muted">{order.storageName}</td>
                <td className="py-2 text-ink-muted">{formatDateTime(order.createdAt)}</td>
                <td className="py-2 pr-6 text-right tabular-nums text-ink">{order.units.toLocaleString('pt-BR')}</td>
                <td className="py-2 pr-6 text-right tabular-nums text-ink">{formatCurrency(order.total)}</td>
                <td className="py-2">
                  <SalesStatusBadge status={order.status} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}
