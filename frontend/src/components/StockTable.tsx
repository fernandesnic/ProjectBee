import type { StockBalance } from '../services/api'

interface StockTableProps {
  stock: StockBalance[]
  onEdit?: (item: StockBalance) => void
  onDelete?: (item: StockBalance) => void
}

export function StockTable({ stock, onEdit, onDelete }: StockTableProps) {
  if (stock.length === 0) {
    return <p className="text-sm text-ink-muted">Nenhum saldo registrado.</p>
  }

  return (
    <>
      {/* Mobile: stacked cards, one per balance line, with full-size touch targets */}
      <div className="flex flex-col gap-3 sm:hidden">
        {stock.map((item) => (
          <div
            key={`${item.productId}-${item.storageId}-${item.batch}`}
            className="rounded-xl border border-line bg-cream p-4"
          >
            <p className="font-medium text-ink">{item.productName}</p>
            <p className="text-sm text-ink-muted">
              {item.storageName}
              <span className="block text-xs text-ink-muted/70">{item.storageAddress}</span>
            </p>

            <dl className="mt-3 grid grid-cols-3 gap-x-3 gap-y-2 text-sm">
              <div>
                <dt className="text-xs text-ink-muted">Lote</dt>
                <dd className="font-mono text-ink-muted">{item.batch}</dd>
              </div>
              <div>
                <dt className="text-xs text-ink-muted">Saldo</dt>
                <dd className="tabular-nums text-ink">{item.balance.toLocaleString('pt-BR')}</dd>
              </div>
              <div>
                <dt className="text-xs text-ink-muted">Valor</dt>
                <dd className="tabular-nums font-medium text-ink">
                  {(item.balance * item.productPrice).toLocaleString('pt-BR', {
                    style: 'currency',
                    currency: 'BRL',
                  })}
                </dd>
              </div>
            </dl>

            {(onEdit || onDelete) && (
              <div className="mt-3 flex gap-2 border-t border-line pt-3">
                {onEdit && (
                  <button
                    onClick={() => onEdit(item)}
                    className="flex h-11 flex-1 items-center justify-center rounded-lg text-sm font-medium text-ink-muted transition hover:bg-cream-soft hover:text-honey-dark"
                  >
                    Editar
                  </button>
                )}
                {onDelete && (
                  <button
                    onClick={() => onDelete(item)}
                    className="flex h-11 flex-1 items-center justify-center rounded-lg text-sm font-medium text-danger transition hover:bg-danger/10"
                  >
                    Remover
                  </button>
                )}
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Desktop/tablet: dense table */}
      <div className="hidden overflow-x-auto sm:block">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-line text-ink-muted">
              <th className="py-2 font-medium">Produto</th>
              <th className="py-2 font-medium">Armazém</th>
              <th className="py-2 font-medium">Lote</th>
              <th className="py-2 text-right font-medium">Saldo</th>
              <th className="py-2 text-right font-medium">Valor</th>
              {(onEdit || onDelete) && <th className="py-2" />}
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {stock.map((item) => (
              <tr key={`${item.productId}-${item.storageId}-${item.batch}`}>
                <td className="py-2 text-ink">{item.productName}</td>
                <td className="py-2 text-ink-muted">
                  {item.storageName}
                  <span className="block text-xs text-ink-muted/70">{item.storageAddress}</span>
                </td>
                <td className="py-2 font-mono text-ink-muted">{item.batch}</td>
                <td className="py-2 text-right tabular-nums text-ink">
                  {item.balance.toLocaleString('pt-BR')}
                </td>
                <td className="py-2 text-right tabular-nums font-medium text-ink">
                  {(item.balance * item.productPrice).toLocaleString('pt-BR', {
                    style: 'currency',
                    currency: 'BRL',
                  })}
                </td>
                {(onEdit || onDelete) && (
                  <td className="py-2 text-right">
                    <div className="flex justify-end gap-2">
                      {onEdit && (
                        <button
                          onClick={() => onEdit(item)}
                          className="rounded-md px-2 py-1.5 text-ink-muted transition hover:bg-cream-soft hover:text-honey-dark"
                        >
                          Editar
                        </button>
                      )}
                      {onDelete && (
                        <button
                          onClick={() => onDelete(item)}
                          className="rounded-md px-2 py-1.5 text-danger transition hover:bg-danger/10"
                        >
                          Remover
                        </button>
                      )}
                    </div>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}
