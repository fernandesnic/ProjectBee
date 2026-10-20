import { useEffect, useState } from 'react'
import { getMovements, type Movement, type MovementType, type StockBalance } from '../services/api'
import { formatDateTime } from '../services/date'

const TYPE_LABEL: Record<MovementType, string> = {
  Entry: 'Entrada',
  Exit: 'Saída',
  TransferOut: 'Transferência enviada',
  TransferIn: 'Transferência recebida',
  Adjustment: 'Ajuste',
  OpeningBalance: 'Saldo inicial',
}

interface MovementHistoryProps {
  item: StockBalance
}

export function MovementHistory({ item }: MovementHistoryProps) {
  const [movements, setMovements] = useState<Movement[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    getMovements({ productId: item.productId, storageId: item.storageId, batch: item.batch })
      .then(setMovements)
      .catch(() => setError('Não foi possível carregar o histórico.'))
      .finally(() => setLoading(false))
  }, [item.productId, item.storageId, item.batch])

  if (loading) return <p className="text-sm text-ink-muted">Carregando histórico...</p>
  if (error)
    return (
      <p className="text-sm text-danger" role="alert">
        {error}
      </p>
    )
  if (movements.length === 0)
    return <p className="text-sm text-ink-muted">Nenhum movimento registrado para este lote.</p>

  return (
    <ol className="divide-y divide-line">
      {movements.map((movement) => (
        <li key={movement.id} className="flex items-start justify-between gap-4 py-3 first:pt-0 last:pb-0">
          <div className="min-w-0">
            <p className="text-sm font-medium text-ink">{TYPE_LABEL[movement.type] ?? movement.type}</p>
            <p className="text-xs text-ink-muted">
              {formatDateTime(movement.createdAt)}
              <span aria-hidden="true"> · </span>
              <span className="break-all">{movement.createdBy}</span>
            </p>
            {movement.reason && (
              <p className="mt-1 text-xs text-ink-muted italic">“{movement.reason}”</p>
            )}
          </div>
          <p
            className={`shrink-0 text-sm font-semibold tabular-nums ${
              movement.quantity < 0 ? 'text-danger' : 'text-honey-text'
            }`}
          >
            {movement.quantity > 0 ? '+' : ''}
            {movement.quantity.toLocaleString('pt-BR')}
          </p>
        </li>
      ))}
    </ol>
  )
}
