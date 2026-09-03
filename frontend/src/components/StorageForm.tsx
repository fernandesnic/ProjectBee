import { useState, type FormEvent } from 'react'
import axios from 'axios'
import { createStorage, updateStorage, type Storage } from '../services/api'

interface StorageFormProps {
  storage?: Storage              // ausente = criação | presente = edição
  onSaved: (storage: Storage) => void
  onCancel: () => void
}

interface FormState {
  name: string
  idNumber: string
  addressStreet: string
  addressNumber: string
  addressCity: string
  isActive: boolean
}

export function StorageForm({ storage, onSaved, onCancel }: StorageFormProps) {
  const isEditing = Boolean(storage)

  const [form, setForm] = useState<FormState>({
    name: storage?.name ?? '',
    idNumber: storage?.idNumber ?? '',
    addressStreet: storage?.addressStreet ?? '',
    addressNumber: storage?.addressNumber ?? '',
    addressCity: storage?.addressCity ?? '',
    isActive: storage?.isActive ?? true,
  })

  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors({})
    setSubmitting(true)

    try {
      if (storage) {
        await updateStorage(storage.id, {
          name: form.name,
          addressStreet: form.addressStreet,
          addressNumber: form.addressNumber,
          addressCity: form.addressCity,
          isActive: form.isActive,
        })
        onSaved({ ...storage, ...form })
      } else {
        const created = await createStorage({
          name: form.name,
          idNumber: form.idNumber,
          addressStreet: form.addressStreet,
          addressNumber: form.addressNumber,
          addressCity: form.addressCity,
        })
        onSaved(created)
      }
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 400) {
        setErrors(err.response.data?.errors ?? {})
      } else {
        setErrors({ geral: ['Não foi possível salvar o armazém.'] })
      }
    } finally {
      setSubmitting(false)
    }
  }

  function fieldErrors(field: string) {
    const messages = errors[field]
    if (!messages?.length) return null
    return <p className="mt-1 text-xs text-red-600">{messages.join(' ')}</p>
  }

  const inputClass =
    'mt-1 w-full rounded-lg border border-line bg-cream px-3 py-2 text-sm text-ink outline-none transition focus:border-honey-dark'

  return (
    <form onSubmit={handleSubmit} className="grid gap-4 sm:grid-cols-2">
      <div>
        <label htmlFor="name" className="text-sm font-medium text-ink-muted">
          Nome
        </label>
        <input
          id="name"
          required
          value={form.name}
          onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
          className={inputClass}
        />
        {fieldErrors('Name')}
      </div>

      <div>
        <label htmlFor="idNumber" className="text-sm font-medium text-ink-muted">
          Código
        </label>
        <input
          id="idNumber"
          required
          disabled={isEditing}
          value={form.idNumber}
          onChange={(event) => setForm((current) => ({ ...current, idNumber: event.target.value }))}
          className={`${inputClass} ${isEditing ? 'cursor-not-allowed opacity-60' : ''}`}
        />
        {isEditing && (
          <p className="mt-1 text-xs text-ink-muted">O código não pode ser alterado.</p>
        )}
        {fieldErrors('IdNumber')}
      </div>

      <div className="sm:col-span-2">
        <label htmlFor="addressStreet" className="text-sm font-medium text-ink-muted">
          Rua
        </label>
        <input
          id="addressStreet"
          required
          value={form.addressStreet}
          onChange={(event) =>
            setForm((current) => ({ ...current, addressStreet: event.target.value }))
          }
          className={inputClass}
        />
        {fieldErrors('AddressStreet')}
      </div>

      <div>
        <label htmlFor="addressNumber" className="text-sm font-medium text-ink-muted">
          Número
        </label>
        <input
          id="addressNumber"
          required
          value={form.addressNumber}
          onChange={(event) =>
            setForm((current) => ({ ...current, addressNumber: event.target.value }))
          }
          className={inputClass}
        />
        {fieldErrors('AddressNumber')}
      </div>

      <div>
        <label htmlFor="addressCity" className="text-sm font-medium text-ink-muted">
          Cidade
        </label>
        <input
          id="addressCity"
          required
          value={form.addressCity}
          onChange={(event) =>
            setForm((current) => ({ ...current, addressCity: event.target.value }))
          }
          className={inputClass}
        />
        {fieldErrors('AddressCity')}
      </div>

      {isEditing && (
        <div className="flex items-center gap-2 self-end pb-2 sm:col-span-2">
          <input
            id="isActive"
            type="checkbox"
            checked={form.isActive}
            onChange={(event) =>
              setForm((current) => ({ ...current, isActive: event.target.checked }))
            }
            className="h-4 w-4 rounded border-line accent-honey"
          />
          <label htmlFor="isActive" className="text-sm font-medium text-ink-muted">
            Armazém ativo
          </label>
        </div>
      )}

      {fieldErrors('geral')}

      <div className="flex items-center gap-3 sm:col-span-2">
        <button
          type="submit"
          disabled={submitting}
          className="rounded-full bg-honey px-6 py-2 text-sm font-semibold text-hive transition hover:bg-honey-light disabled:opacity-60"
        >
          {submitting ? 'Salvando...' : isEditing ? 'Salvar alterações' : 'Criar armazém'}
        </button>

        <button
          type="button"
          onClick={onCancel}
          className="text-sm text-ink-muted transition hover:text-ink"
        >
          Cancelar
        </button>
      </div>
    </form>
  )
}