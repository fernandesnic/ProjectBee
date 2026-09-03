import { useEffect, useState } from 'react'
import axios from 'axios'
import { useOutletContext } from 'react-router-dom'
import { deleteStorage, getStorages, type Storage } from '../services/api'
import { StorageTable } from '../components/StorageTable'
import { StorageForm } from '../components/StorageForm'
import type { AppLayoutContext } from '../components/AppLayout'

type FormMode = { type: 'closed' } | { type: 'create' } | { type: 'edit'; storage: Storage }

function StoragesPage() {
  const { roles } = useOutletContext<AppLayoutContext>()
  const canManage = roles.includes('Manager')

  const [storages, setStorages] = useState<Storage[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [formMode, setFormMode] = useState<FormMode>({ type: 'closed' })

  useEffect(() => {
    getStorages()
      .then(setStorages)
      .catch(() => setError('Não foi possível carregar os armazéns.'))
      .finally(() => setLoading(false))
  }, [])

  async function handleDelete(id: string) {
    setError(null)
    try {
      await deleteStorage(id)
      setStorages((current) => current.filter((storage) => storage.id !== id))
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 403) {
        setError('Seu perfil não tem permissão para excluir armazéns.')
      } else {
        setError('Não foi possível excluir o armazém.')
      }
    }
  }

  function handleSaved(saved: Storage) {
    setStorages((current) => {
      const exists = current.some((s) => s.id === saved.id)
      return exists
        ? current.map((s) => (s.id === saved.id ? saved : s))
        : [...current, saved]
    })
    setFormMode({ type: 'closed' })
  }

  const activeCount = storages.filter((storage) => storage.isActive).length
  const cityCount = new Set(storages.map((storage) => storage.addressCity)).size

  return (
    <div>
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight text-ink">Armazéns</h1>
        {canManage && formMode.type === 'closed' && (
          <button
            onClick={() => setFormMode({ type: 'create' })}
            className="rounded-full bg-honey px-5 py-2 text-sm font-semibold text-hive transition hover:bg-honey-light"
          >
            Novo armazém
          </button>
        )}
      </div>

      {canManage && formMode.type !== 'closed' && (
        <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-6">
          <StorageForm
            key={formMode.type === 'edit' ? formMode.storage.id : 'create'}
            storage={formMode.type === 'edit' ? formMode.storage : undefined}
            onSaved={handleSaved}
            onCancel={() => setFormMode({ type: 'closed' })}
          />
        </div>
      )}

      <div className="mt-6 grid gap-4 sm:grid-cols-2">
        <div className="rounded-2xl border border-line bg-cream-soft p-6">
          <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
            Armazéns ativos
          </p>
          <p className="mt-2 text-2xl font-bold tabular-nums text-ink">
            {loading ? '—' : activeCount}
          </p>
        </div>
        <div className="rounded-2xl border border-line bg-cream-soft p-6">
          <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
            Cidades atendidas
          </p>
          <p className="mt-2 text-2xl font-bold tabular-nums text-ink">
            {loading ? '—' : cityCount}
          </p>
        </div>
      </div>

      {error && <p className="mt-4 text-sm text-red-600">{error}</p>}

      <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-6">
        {loading ? (
          <p className="text-sm text-ink-muted">Carregando armazéns...</p>
        ) : (
          <StorageTable
            storages={storages}
            onEdit={canManage ? (storage) => setFormMode({ type: 'edit', storage }) : undefined}
            onDelete={canManage ? handleDelete : undefined}
          />
        )}
      </div>
    </div>
  )
}

export default StoragesPage