import type { StockBalance } from '../services/api'
import type { MovementKind } from './MovementForm'

interface StockTableProps {
  stock: StockBalance[]
  canAdjust: boolean
  onMovement: (kind: MovementKind, item: StockBalance) => void
  onHistory: (item: StockBalance) => void
}

const ACTIONS: { kind: MovementKind; label: string; managerOnly?: boolean }[] = [
  { kind: 'entry', label: 'Entrada' },
  { kind: 'exit', label: 'Saída' },
  { kind: 'transfer', label: 'Transferir' },
  { kind: 'adjustment', label: 'Ajustar', managerOnly: true },
]

function keyOf(item: StockBalance) {
  return `${item.productId}-${item.storageId}-${item.batch}`
}

function formatValue(item: StockBalance) {
  return (item.balance * item.productPrice).toLocaleString('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  })
}

export function StockTable({ stock, canAdjust, onMovement, onHistory }: StockTableProps) {
  if (stock.length === 0) {
    return <p className="text-sm text-ink-muted">Nenhum saldo registrado.</p>
  }

  const actions = ACTIONS.filter((action) => !action.managerOnly || canAdjust)

  return (
    <>
      {/* Celular e tablet: cartões, um por saldo, com áreas de toque de 44px */}
      <div className="grid gap-3 sm:grid-cols-2 xl:hidden">
        {stock.map((item) => (
          <div key={keyOf(item)} className="flex flex-col rounded-xl border border-line bg-cream p-4">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="font-medium text-ink">{item.productName}</p>
                <p className="text-sm text-ink-muted">
                  {item.storageName}
                  <span className="block text-xs text-ink-muted/70">{item.storageAddress}</span>
                </p>
              </div>
              <button
                onClick={() => onHistory(item)}
                className="-mr-2 -mt-2 flex h-11 shrink-0 items-center rounded-lg px-2 text-sm font-medium text-honey-text transition hover:bg-cream-soft"
              >
                Histórico
              </button>
            </div>

            <dl className="mb-3 mt-3 grid grid-cols-3 gap-x-3 gap-y-2 text-sm">
              <div className="min-w-0">
                <dt className="text-xs text-ink-muted">Lote</dt>
                <dd className="truncate font-mono text-ink-muted">{item.batch}</dd>
              </div>
              <div>
                <dt className="text-xs text-ink-muted">Saldo</dt>
                <dd className="tabular-nums text-ink">{item.balance.toLocaleString('pt-BR')}</dd>
              </div>
              <div>
                <dt className="text-xs text-ink-muted">Valor</dt>
                <dd className="tabular-nums font-medium text-ink">{formatValue(item)}</dd>
              </div>
            </dl>

            <div
              className={`mt-auto grid gap-1 border-t border-line pt-3 ${
                actions.length === 4 ? 'grid-cols-2 min-[400px]:grid-cols-4' : 'grid-cols-3'
              }`}
            >
              {actions.map((action) => (
                <button
                  key={action.kind}
                  onClick={() => onMovement(action.kind, item)}
                  className="flex h-11 items-center justify-center rounded-lg text-sm font-medium text-ink-muted transition hover:bg-cream-soft hover:text-honey-dark"
                >
                  {action.label}
                </button>
              ))}
            </div>
          </div>
        ))}
      </div>

      {/* Desktop: tabela densa */}
      <div className="hidden overflow-x-auto xl:block">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-line text-ink-muted">
              <th className="py-2 font-medium">Produto</th>
              <th className="py-2 font-medium">Armazém</th>
              <th className="py-2 font-medium">Lote</th>
              <th className="py-2 text-right font-medium">Saldo</th>
              <th className="py-2 text-right font-medium">Valor</th>
              <th className="py-2">
                <span className="sr-only">Ações</span>
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {stock.map((item) => (
              <tr key={keyOf(item)}>
                <td className="py-2 pr-3 text-ink">{item.productName}</td>
                <td className="py-2 pr-3 text-ink-muted">
                  {item.storageName}
                  <span className="block text-xs text-ink-muted/70">{item.storageAddress}</span>
                </td>
                <td className="py-2 pr-3 font-mono text-ink-muted">{item.batch}</td>
                <td className="py-2 pl-3 text-right tabular-nums text-ink">
                  {item.balance.toLocaleString('pt-BR')}
                </td>
                <td className="py-2 pl-3 text-right tabular-nums font-medium text-ink">
                  {formatValue(item)}
                </td>
                <td className="py-2 pl-3 text-right">
                  <div className="flex justify-end gap-1 whitespace-nowrap">
                    {actions.map((action) => (
                      <button
                        key={action.kind}
                        onClick={() => onMovement(action.kind, item)}
                        aria-label={`${action.label}: ${item.productName}, lote ${item.batch}, ${item.storageName}`}
                        className="rounded-md px-2 py-1.5 text-ink-muted transition hover:bg-cream-soft hover:text-honey-dark"
                      >
                        {action.label}
                      </button>
                    ))}
                    <span className="mx-1 w-px self-stretch bg-line" aria-hidden="true" />
                    <button
                      onClick={() => onHistory(item)}
                      aria-label={`Histórico: ${item.productName}, lote ${item.batch}, ${item.storageName}`}
                      className="rounded-md px-2 py-1.5 font-medium text-honey-text transition hover:bg-cream-soft"
                    >
                      Histórico
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}
