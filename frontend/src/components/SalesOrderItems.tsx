import type { SalesOrderItem } from '../services/api'
import { formatCurrency } from '../services/price'

export function SalesOrderItems({ items }: { items: SalesOrderItem[] }) {
  const total = items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0)

  return (
    <>
      {/* Celular e tablet: cartões, um por item */}
      <div className="grid gap-3 sm:grid-cols-2 xl:hidden">
        {items.map((item) => (
          <div key={item.id} className="rounded-xl border border-line bg-cream p-4">
            <p className="font-medium text-ink">{item.productName}</p>
            <p className="font-mono text-xs text-ink-muted">{item.productSku}</p>

            <dl className="mt-3 grid grid-cols-3 gap-x-3 gap-y-2 text-sm">
              <div>
                <dt className="text-xs text-ink-muted">Qtd.</dt>
                <dd className="tabular-nums text-ink">{item.quantity.toLocaleString('pt-BR')}</dd>
              </div>
              <div>
                <dt className="text-xs text-ink-muted">Preço</dt>
                <dd className="tabular-nums text-ink">{formatCurrency(item.unitPrice)}</dd>
              </div>
              <div>
                <dt className="text-xs text-ink-muted">Total</dt>
                <dd className="tabular-nums font-medium text-ink">
                  {formatCurrency(item.quantity * item.unitPrice)}
                </dd>
              </div>
            </dl>
          </div>
        ))}
        <p className="flex items-baseline justify-between border-t border-line pt-3 text-sm text-ink-muted sm:col-span-2">
          Total do pedido
          <span className="font-semibold tabular-nums text-ink">{formatCurrency(total)}</span>
        </p>
      </div>

      {/* Desktop: tabela densa */}
      <div className="hidden overflow-x-auto xl:block">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-line text-ink-muted">
              <th className="py-2 font-medium">Produto</th>
              <th className="py-2 pr-4 text-right font-medium">Quantidade</th>
              <th className="py-2 pr-4 text-right font-medium">Preço</th>
              <th className="py-2 text-right font-medium">Total</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {items.map((item) => (
              <tr key={item.id}>
                <td className="py-2">
                  <p className="text-ink">{item.productName}</p>
                  <p className="font-mono text-xs text-ink-muted">{item.productSku}</p>
                </td>
                <td className="py-2 pr-4 text-right tabular-nums text-ink">
                  {item.quantity.toLocaleString('pt-BR')}
                </td>
                <td className="py-2 pr-4 text-right tabular-nums text-ink">{formatCurrency(item.unitPrice)}</td>
                <td className="py-2 text-right tabular-nums text-ink">
                  {formatCurrency(item.quantity * item.unitPrice)}
                </td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr className="border-t border-line">
              <td colSpan={3} className="pt-3 text-right text-ink-muted">
                Total do pedido
              </td>
              <td className="pt-3 text-right font-semibold tabular-nums text-ink">{formatCurrency(total)}</td>
            </tr>
          </tfoot>
        </table>
      </div>
    </>
  )
}
