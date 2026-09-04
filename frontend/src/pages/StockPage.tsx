import { useCallback, useEffect, useState } from 'react'
import axios from 'axios'
import { useOutletContext } from 'react-router-dom'
import { deleteStock, getStock, type StockBalance } from '../services/api'
import { StockTable } from '../components/StockTable'
import { StockForm } from '../components/StockForm'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Toast } from '../components/Toast'
import type { AppLayoutContext } from '../components/AppLayout'

type FormMode = { type: 'closed' } | { type: 'create' } | { type: 'edit'; item: StockBalance }

function StockPage() {
  const { roles } = useOutletContext<AppLayoutContext>()
  const canManage = roles.includes('Manager')

  const [stock, setStock] = useState<StockBalance[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [formMode, setFormMode] = useState<FormMode>({ type: 'closed' })
  const [pendingDelete, setPendingDelete] = useState<StockBalance | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [toast, setToast] = useState<string | null>(null)

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

  async function confirmDelete() {
    if (!pendingDelete) return
    setError(null)
    setDeleting(true)
    try {
      await deleteStock(pendingDelete.productId, pendingDelete.storageId, pendingDelete.batch)
      setStock((current) =>
        current.filter(
          (s) =>
            !(
              s.productId === pendingDelete.productId &&
              s.storageId === pendingDelete.storageId &&
              s.batch === pendingDelete.batch
            ),
        ),
      )
      setPendingDelete(null)
      setToast('Saldo removido com sucesso.')
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 403) {
        setError('Seu perfil não tem permissão para excluir saldos.')
      } else {
        setError('Não foi possível excluir o saldo.')
      }
    } finally {
      setDeleting(false)
    }
  }

  function handleSaved() {
    const isEditing = formMode.type === 'edit'
    setFormMode({ type: 'closed' })
    loadStock()
    setToast(isEditing ? 'Saldo atualizado com sucesso.' : 'Saldo registrado com sucesso.')
  }

  const totalUnits = stock.reduce((sum, item) => sum + item.balance, 0)
  const totalValue = stock.reduce((sum, item) => sum + item.balance * item.productPrice, 0)

  const sortedStock = [...stock].sort(
    (a, b) => b.balance * b.productPrice - a.balance * a.productPrice,
  )
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
            Valor total em estoque
          </p>
          <p className="mt-2 text-2xl font-bold tabular-nums text-ink">
            {loading
              ? '—'
              : totalValue.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
          </p>
        </div>
      </div>

      {error && <p className="mt-4 text-sm text-red-600">{error}</p>}

      <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-6">
        {loading ? (
          <p className="text-sm text-ink-muted">Carregando saldos...</p>
        ) : (
          <StockTable
            stock={sortedStock}
            onEdit={canManage ? (item) => setFormMode({ type: 'edit', item }) : undefined}
            onDelete={canManage ? (item) => setPendingDelete(item) : undefined}
          />
        )}
      </div>

      <ConfirmDialog
        open={pendingDelete !== null}
        title="Remover saldo"
        description={
          pendingDelete
            ? `Tem certeza que deseja remover o saldo do lote "${pendingDelete.batch}" de "${pendingDelete.productName}" em "${pendingDelete.storageName}"?`
            : undefined
        }
        busy={deleting}
        onConfirm={confirmDelete}
        onCancel={() => setPendingDelete(null)}
      />

      <Toast message={toast} onDismiss={() => setToast(null)} />
    </div>
  )
}

export default StockPage