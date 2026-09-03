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
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-line text-ink-muted">
          <th className="py-2 font-medium">Produto</th>
          <th className="py-2 font-medium">Armazém</th>
          <th className="py-2 font-medium">Lote</th>
          <th className="py-2 text-right font-medium">Saldo</th>
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
            {(onEdit || onDelete) && (
              <td className="py-2 text-right">
                <div className="flex justify-end gap-4">
                  {onEdit && (
                    <button
                      onClick={() => onEdit(item)}
                      className="text-ink-muted transition hover:text-honey-dark hover:underline"
                    >
                      Editar
                    </button>
                  )}
                  {onDelete && (
                    <button
                      onClick={() => onDelete(item)}
                      className="text-red-600 transition hover:underline"
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
  )
}