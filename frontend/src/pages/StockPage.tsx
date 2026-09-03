import { useCallback, useEffect, useState } from 'react'
import axios from 'axios'
import { useOutletContext } from 'react-router-dom'
import { deleteStock, getStock, type StockBalance } from '../services/api'
import { StockTable } from '../components/StockTable'
import { StockForm } from '../components/StockForm'
import type { AppLayoutContext } from '../components/AppLayout'

type FormMode = { type: 'closed' } | { type: 'create' } | { type: 'edit'; item: StockBalance }

function StockPage() {
  const { roles } = useOutletContext<AppLayoutContext>()
  const canManage = roles.includes('Manager')

  const [stock, setStock] = useState<StockBalance[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [formMode, setFormMode] = useState<FormMode>({ type: 'closed' })

  const loadStock = useCallback(() => {
    setLoading(true)
    getStock()
      .then(setStock)
      .catch(() => setError('Não foi possível carregar os saldos.'))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    loadStock()
  }, [loadStock])

  async function handleDelete(item: StockBalance) {
    setError(null)
    try {
      await deleteStock(item.productId, item.storageId, item.batch)
      setStock((current) =>
        current.filter(
          (s) =>
            !(
              s.productId === item.productId &&
              s.storageId === item.storageId &&
              s.batch === item.batch
            ),
        ),
      )
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 403) {
        setError('Seu perfil não tem permissão para excluir saldos.')
      } else {
        setError('Não foi possível excluir o saldo.')
      }
    }
  }

  function handleSaved() {
    setFormMode({ type: 'closed' })
    loadStock()
  }

  const totalUnits = stock.reduce((sum, item) => sum + item.balance, 0)
  const batchCount = stock.length

  return (
    <div>
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight text-ink">Estoque</h1>
        {canManage && formMode.type === 'closed' && (
          <button
            onClick={() => setFormMode({ type: 'create' })}
            className="rounded-full bg-honey px-5 py-2 text-sm font-semibold text-hive transition hover:bg-honey-light"
          >
            Registrar saldo
          </button>
        )}
      </div>

      {canManage && formMode.type !== 'closed' && (
        <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-6">
          <StockForm
            key={
              formMode.type === 'edit'
                ? `${formMode.item.productId}-${formMode.item.storageId}-${formMode.item.batch}`
                : 'create'
            }
            item={formMode.type === 'edit' ? formMode.item : undefined}
            onSaved={handleSaved}
            onCancel={() => setFormMode({ type: 'closed' })}
          />
        </div>
      )}

      <div className="mt-6 grid gap-4 sm:grid-cols-2">
        <div className="rounded-2xl border border-line bg-cream-soft p-6">
          <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
            Unidades em estoque
          </p>
          <p className="mt-2 text-2xl font-bold tabular-nums text-ink">
            {loading ? '—' : totalUnits.toLocaleString('pt-BR')}
          </p>
        </div>
        <div className="rounded-2xl border border-line bg-cream-soft p-6">
          <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
            Lotes rastreados
          </p>
          <p className="mt-2 text-2xl font-bold tabular-nums text-ink">
            {loading ? '—' : batchCount}
          </p>
        </div>
      </div>

      {error && <p className="mt-4 text-sm text-red-600">{error}</p>}

      <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-6">
        {loading ? (
          <p className="text-sm text-ink-muted">Carregando saldos...</p>
        ) : (
          <StockTable
            stock={stock}
            onEdit={canManage ? (item) => setFormMode({ type: 'edit', item }) : undefined}
            onDelete={canManage ? handleDelete : undefined}
          />
        )}
      </div>
    </div>
  )
}

export default StockPage