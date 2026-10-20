import { useState, type FormEvent } from 'react'
import {
  createSupplier,
  formatDocument,
  formErrorsFrom,
  updateSupplier,
  type Supplier,
} from '../services/api'
import { FieldError } from './FieldError'

interface SupplierFormProps {
  supplier?: Supplier            // ausente = criação | presente = edição
  onSaved: (supplier: Supplier) => void
  onCancel: () => void
}

interface FormState {
  name: string
  document: string
  email: string
  phone: string
  isActive: boolean
}

export function SupplierForm({ supplier, onSaved, onCancel }: SupplierFormProps) {
  const isEditing = Boolean(supplier)

  const [form, setForm] = useState<FormState>({
    name: supplier?.name ?? '',
    document: supplier ? formatDocument(supplier.document) : '',
    email: supplier?.email ?? '',
    phone: supplier?.phone ?? '',
    isActive: supplier?.isActive ?? true,
  })

  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors({})
    setSubmitting(true)

    try {
      const fields = { name: form.name, email: form.email.trim(), phone: form.phone.trim() }
      if (supplier) {
        const updated = await updateSupplier(supplier.id, {
          ...fields,
          isActive: form.isActive,
          rowVersion: supplier.rowVersion,
        })
        onSaved(updated)
      } else {
        // A API guarda só os dígitos: pontos, barra e traço digitados ficam de fora.
        const created = await createSupplier({ ...fields, document: form.document.replace(/\D/g, '') })
        onSaved(created)
      }
    } catch (err) {
      setErrors(formErrorsFrom(err, 'Não foi possível salvar o fornecedor.'))
    } finally {
      setSubmitting(false)
    }
  }

  const inputClass =
    'mt-1 w-full rounded-lg border border-line bg-cream px-3 py-2 text-sm text-ink outline-none transition focus:border-honey-dark'

  return (
    <form onSubmit={handleSubmit} className="grid gap-4 sm:grid-cols-2">
      <div>
        <label htmlFor="name" className="text-sm font-medium text-ink-muted">
          Nome ou razão social
        </label>
        <input
          id="name"
          required
          autoFocus
          maxLength={150}
          value={form.name}
          onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
          className={inputClass}
        />
        <FieldError messages={errors.Name} />
      </div>

      <div>
        <label htmlFor="document" className="text-sm font-medium text-ink-muted">
          CPF ou CNPJ
        </label>
        <input
          id="document"
          required
          inputMode="numeric"
          disabled={isEditing}
          value={form.document}
          onChange={(event) => setForm((current) => ({ ...current, document: event.target.value }))}
          className={`${inputClass} tabular-nums ${isEditing ? 'cursor-not-allowed opacity-60' : ''}`}
        />
        {isEditing ? (
          <p className="mt-1 text-xs text-ink-muted">O documento não pode ser alterado.</p>
        ) : (
          <p className="mt-1 text-xs text-ink-muted">Com ou sem pontuação.</p>
        )}
        <FieldError messages={errors.Document} />
      </div>

      <div>
        <label htmlFor="email" className="text-sm font-medium text-ink-muted">
          E-mail <span className="font-normal">(opcional)</span>
        </label>
        <input
          id="email"
          type="email"
          maxLength={256}
          value={form.email}
          onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))}
          className={inputClass}
        />
        <FieldError messages={errors.Email} />
      </div>

      <div>
        <label htmlFor="phone" className="text-sm font-medium text-ink-muted">
          Telefone <span className="font-normal">(opcional)</span>
        </label>
        <input
          id="phone"
          type="tel"
          maxLength={20}
          value={form.phone}
          onChange={(event) => setForm((current) => ({ ...current, phone: event.target.value }))}
          className={inputClass}
        />
        <FieldError messages={errors.Phone} />
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
            Fornecedor ativo
          </label>
        </div>
      )}

      <FieldError messages={errors.geral} />

      <div className="flex items-center gap-3 sm:col-span-2">
        <button
          type="submit"
          disabled={submitting}
          className="rounded-full bg-honey px-6 py-2 text-sm font-semibold text-hive transition hover:bg-honey-light disabled:opacity-60"
        >
          {submitting ? 'Salvando...' : isEditing ? 'Salvar alterações' : 'Criar fornecedor'}
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
