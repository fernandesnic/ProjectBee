import { useEffect, useRef, useState } from 'react'
import axios from 'axios'
import { useOutletContext } from 'react-router-dom'
import { deleteCustomer, getCustomers, type Customer } from '../services/api'
import { CustomerTable } from '../components/CustomerTable'
import { CustomerForm } from '../components/CustomerForm'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Toast } from '../components/Toast'
import type { AppLayoutContext } from '../components/AppLayout'

type FormMode = { type: 'closed' } | { type: 'create' } | { type: 'edit'; customer: Customer }

function CustomersPage() {
  const { roles } = useOutletContext<AppLayoutContext>()
  const canManage = roles.includes('Manager')

  const [customers, setCustomers] = useState<Customer[]>([])
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
  const [pendingDelete, setPendingDelete] = useState<Customer | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [toast, setToast] = useState<string | null>(null)

  useEffect(() => {
    getCustomers()
      .then(setCustomers)
      .catch(() => setError('Não foi possível carregar os clientes.'))
      .finally(() => setLoading(false))
  }, [])

  async function confirmDelete() {
    if (!pendingDelete) return
    setError(null)
    setDeleting(true)
    try {
      await deleteCustomer(pendingDelete.id)
      setCustomers((current) => current.filter((customer) => customer.id !== pendingDelete.id))
      setPendingDelete(null)
      setToast('Cliente removido com sucesso.')
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 403) {
        setError('Seu perfil não tem permissão para excluir clientes.')
      } else if (axios.isAxiosError(err) && err.response?.status === 422) {
        setPendingDelete(null)
        setError(err.response.data?.detail ?? 'O cliente não pode ser excluído.')
      } else {
        setError('Não foi possível excluir o cliente.')
      }
    } finally {
      setDeleting(false)
    }
  }

  function handleSaved(saved: Customer) {
    const isEditing = formMode.type === 'edit'
    setCustomers((current) => {
      const exists = current.some((s) => s.id === saved.id)
      return exists ? current.map((s) => (s.id === saved.id ? saved : s)) : [...current, saved]
    })
    setFormMode({ type: 'closed' })
    setError(null)
    setToast(isEditing ? 'Cliente atualizado com sucesso.' : 'Cliente cadastrado com sucesso.')
  }

  const activeCount = customers.filter((customer) => customer.isActive).length

  return (
    <div>
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight text-ink">Clientes</h1>
        {canManage && formMode.type === 'closed' && (
          <button
            ref={newButtonRef}
            onClick={() => setFormMode({ type: 'create' })}
            className="rounded-full bg-honey px-5 py-2 text-sm font-semibold text-hive transition hover:bg-honey-light"
          >
            Novo cliente
          </button>
        )}
      </div>

      {canManage && formMode.type !== 'closed' && (
        <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
          <CustomerForm
            key={formMode.type === 'edit' ? formMode.customer.id : 'create'}
            customer={formMode.type === 'edit' ? formMode.customer : undefined}
            onSaved={handleSaved}
            onCancel={() => setFormMode({ type: 'closed' })}
          />
        </div>
      )}

      <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
        <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
          Clientes ativos
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
          <p className="text-sm text-ink-muted">Carregando clientes...</p>
        ) : (
          <CustomerTable
            customers={[...customers].sort((a, b) => a.name.localeCompare(b.name, 'pt-BR'))}
            onEdit={canManage ? (customer) => setFormMode({ type: 'edit', customer }) : undefined}
            onDelete={
              canManage
                ? (id) => setPendingDelete(customers.find((s) => s.id === id) ?? null)
                : undefined
            }
          />
        )}
      </div>

      <ConfirmDialog
        open={pendingDelete !== null}
        title="Remover cliente"
        description={
          pendingDelete
            ? `Tem certeza que deseja remover "${pendingDelete.name}"? Ele sai das listagens, mas os pedidos de venda antigos continuam apontando para ele.`
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

export default CustomersPage
