import { useEffect, useRef, useState } from 'react'
import axios from 'axios'
import { useOutletContext } from 'react-router-dom'
import { deleteSupplier, getSuppliers, type Supplier } from '../services/api'
import { SupplierTable } from '../components/SupplierTable'
import { SupplierForm } from '../components/SupplierForm'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Toast } from '../components/Toast'
import type { AppLayoutContext } from '../components/AppLayout'

type FormMode = { type: 'closed' } | { type: 'create' } | { type: 'edit'; supplier: Supplier }

function SuppliersPage() {
  const { roles } = useOutletContext<AppLayoutContext>()
  const canManage = roles.includes('Manager')

  const [suppliers, setSuppliers] = useState<Supplier[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [formMode, setFormMode] = useState<FormMode>({ type: 'closed' })
  const newButtonRef = useRef<HTMLButtonElement>(null)
  const formWasOpen = useRef(false)

  // O botão "Novo" some enquanto o formulário está aberto; ao fechar, o foco volta para ele.
  useEffect(() => {
    if (formMode.type === 'closed' && formWasOpen.current) newButtonRef.current?.focus()
    formWasOpen.current = formMode.type !== 'closed'
  }, [formMode.type])
  const [pendingDelete, setPendingDelete] = useState<Supplier | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [toast, setToast] = useState<string | null>(null)

  useEffect(() => {
    getSuppliers()
      .then(setSuppliers)
      .catch(() => setError('Não foi possível carregar os fornecedores.'))
      .finally(() => setLoading(false))
  }, [])

  async function confirmDelete() {
    if (!pendingDelete) return
    setError(null)
    setDeleting(true)
    try {
      await deleteSupplier(pendingDelete.id)
      setSuppliers((current) => current.filter((supplier) => supplier.id !== pendingDelete.id))
      setPendingDelete(null)
      setToast('Fornecedor removido com sucesso.')
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 403) {
        setError('Seu perfil não tem permissão para excluir fornecedores.')
      } else if (axios.isAxiosError(err) && err.response?.status === 422) {
        setPendingDelete(null)
        setError(err.response.data?.detail ?? 'O fornecedor não pode ser excluído.')
      } else {
        setError('Não foi possível excluir o fornecedor.')
      }
    } finally {
      setDeleting(false)
    }
  }

  function handleSaved(saved: Supplier) {
    const isEditing = formMode.type === 'edit'
    setSuppliers((current) => {
      const exists = current.some((s) => s.id === saved.id)
      return exists ? current.map((s) => (s.id === saved.id ? saved : s)) : [...current, saved]
    })
    setFormMode({ type: 'closed' })
    setError(null)
    setToast(isEditing ? 'Fornecedor atualizado com sucesso.' : 'Fornecedor cadastrado com sucesso.')
  }

  const activeCount = suppliers.filter((supplier) => supplier.isActive).length

  return (
    <div>
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight text-ink">Fornecedores</h1>
        {canManage && formMode.type === 'closed' && (
          <button
            ref={newButtonRef}
            onClick={() => setFormMode({ type: 'create' })}
            className="rounded-full bg-honey px-5 py-2 text-sm font-semibold text-hive transition hover:bg-honey-light"
          >
            Novo fornecedor
          </button>
        )}
      </div>

      {canManage && formMode.type !== 'closed' && (
        <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
          <SupplierForm
            key={formMode.type === 'edit' ? formMode.supplier.id : 'create'}
            supplier={formMode.type === 'edit' ? formMode.supplier : undefined}
            onSaved={handleSaved}
            onCancel={() => setFormMode({ type: 'closed' })}
          />
        </div>
      )}

      <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
        <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
          Fornecedores ativos
        </p>
        <p className="mt-2 text-2xl font-bold tabular-nums text-ink">
          {loading ? '—' : activeCount}
        </p>
      </div>

      {error && (
        <p className="mt-4 text-sm text-danger" role="alert">
          {error}
        </p>
      )}

      <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
        {loading ? (
          <p className="text-sm text-ink-muted">Carregando fornecedores...</p>
        ) : (
          <SupplierTable
            suppliers={[...suppliers].sort((a, b) => a.name.localeCompare(b.name, 'pt-BR'))}
            onEdit={canManage ? (supplier) => setFormMode({ type: 'edit', supplier }) : undefined}
            onDelete={
              canManage
                ? (id) => setPendingDelete(suppliers.find((s) => s.id === id) ?? null)
                : undefined
            }
          />
        )}
      </div>

      <ConfirmDialog
        open={pendingDelete !== null}
        title="Remover fornecedor"
        description={
          pendingDelete
            ? `Tem certeza que deseja remover "${pendingDelete.name}"? Ele sai das listagens, mas os pedidos de compra antigos continuam apontando para ele.`
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

export default SuppliersPage
