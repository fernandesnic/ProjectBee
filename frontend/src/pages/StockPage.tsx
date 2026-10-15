import { useCallback, useEffect, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { getStock, type MovementResult, type StockBalance } from '../services/api'
import { StockTable } from '../components/StockTable'
import { MovementForm, type MovementKind } from '../components/MovementForm'
import { MovementHistory } from '../components/MovementHistory'
import { Modal } from '../components/Modal'
import { Toast } from '../components/Toast'
import type { AppLayoutContext } from '../components/AppLayout'

type Dialog =
  | { type: 'closed' }
  | { type: 'movement'; kind: MovementKind; item?: StockBalance }
  | { type: 'history'; item: StockBalance }

const TITLE: Record<MovementKind, string> = {
  entry: 'Entrada',
  exit: 'Saída',
  transfer: 'Transferência',
  adjustment: 'Ajuste de inventário',
}

function StockPage() {
  const { roles } = useOutletContext<AppLayoutContext>()
  const canAdjust = roles.includes('Manager')

  const [stock, setStock] = useState<StockBalance[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [dialog, setDialog] = useState<Dialog>({ type: 'closed' })
  const [busy, setBusy] = useState(false)
  const [toast, setToast] = useState<string | null>(null)

  const loadStock = useCallback(() => {
    return getStock()
      .then(setStock)
      .catch(() => setError('Não foi possível carregar os saldos.'))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    loadStock()
  }, [loadStock])

  const closeDialog = useCallback(() => setDialog({ type: 'closed' }), [])
  const dismissToast = useCallback(() => setToast(null), [])

  function handleMovementDone(result: MovementResult) {
    setDialog({ type: 'closed' })
    setError(null)
    setToast(`${result.mensagem} Saldo atual: ${result.saldoAtual.toLocaleString('pt-BR')}.`)
    loadStock()
  }

  const totalUnits = stock.reduce((sum, item) => sum + item.balance, 0)
  const totalValue = stock.reduce((sum, item) => sum + item.balance * item.productPrice, 0)

  const sortedStock = [...stock].sort(
    (a, b) => b.balance * b.productPrice - a.balance * a.productPrice,
  )

  const dialogItem = dialog.type === 'closed' ? undefined : dialog.item

  return (
    <div>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold tracking-tight text-ink">Estoque</h1>
        <button
          onClick={() => setDialog({ type: 'movement', kind: 'entry' })}
          className="h-11 rounded-full bg-honey px-5 text-sm font-semibold text-hive transition hover:bg-honey-light sm:h-auto sm:py-2"
        >
          Entrada em lote novo
        </button>
      </div>

      <div className="mt-6 grid grid-cols-2 gap-3 sm:gap-4">
        <div className="rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
          <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
            Unidades em estoque
          </p>
          <p className="mt-2 text-xl font-bold tabular-nums text-ink sm:text-2xl">
            {loading ? '—' : totalUnits.toLocaleString('pt-BR')}
          </p>
        </div>
        <div className="rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
          <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
            Valor total em estoque
          </p>
          <p className="mt-2 break-words text-xl font-bold tabular-nums text-ink sm:text-2xl">
            {loading
              ? '—'
              : totalValue.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
          </p>
        </div>
      </div>

      {error && (
        <p className="mt-4 text-sm text-danger" role="alert">
          {error}
        </p>
      )}

      <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-3 sm:p-6">
        {loading ? (
          <p className="text-sm text-ink-muted">Carregando saldos...</p>
        ) : (
          <StockTable
            stock={sortedStock}
            canAdjust={canAdjust}
            onMovement={(kind, item) => setDialog({ type: 'movement', kind, item })}
            onHistory={(item) => setDialog({ type: 'history', item })}
          />
        )}
      </div>

      <Modal
        open={dialog.type !== 'closed'}
        title={
          dialog.type === 'movement'
            ? dialog.item
              ? TITLE[dialog.kind]
              : 'Entrada em lote novo'
            : 'Histórico do lote'
        }
        subtitle={
          dialogItem && (
            <>
              <span className="font-medium text-ink">{dialogItem.productName}</span> em{' '}
              {dialogItem.storageName}
              <span className="mt-0.5 block text-xs">
                Lote <span className="font-mono">{dialogItem.batch}</span> · saldo{' '}
                <span className="tabular-nums">{dialogItem.balance.toLocaleString('pt-BR')}</span>
              </span>
            </>
          )
        }
        busy={busy}
        onClose={closeDialog}
      >
        {dialog.type === 'movement' && (
          <MovementForm
            key={`${dialog.kind}-${dialogItem ? `${dialogItem.productId}-${dialogItem.storageId}-${dialogItem.batch}` : 'new'}`}
            kind={dialog.kind}
            item={dialog.item}
            onDone={handleMovementDone}
            onCancel={closeDialog}
            onBusyChange={setBusy}
          />
        )}
        {dialog.type === 'history' && <MovementHistory item={dialog.item} />}
      </Modal>

      <Toast message={toast} onDismiss={dismissToast} />
    </div>
  )
}

export default StockPage
