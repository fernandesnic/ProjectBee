import { useState, type FormEvent } from 'react'
import axios from 'axios'
import { createProduct, type CreateProductPayload, type Product } from '../services/api'

interface ProductFormProps {
  onCreated: (product: Product) => void
  onCancel: () => void
}

const EMPTY_FORM: CreateProductPayload = { name: '', sku: '', desc: '', price: 0 }

export function ProductForm({ onCreated, onCancel }: ProductFormProps) {
  const [form, setForm] = useState<CreateProductPayload>(EMPTY_FORM)
  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors({})
    setSubmitting(true)

    try {
      const product = await createProduct(form)
      onCreated(product)
      setForm(EMPTY_FORM)
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 400) {
        setErrors(err.response.data?.errors ?? {})
      } else {
        setErrors({ geral: ['Não foi possível criar o produto.'] })
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
          className="mt-1 w-full rounded-lg border border-line bg-cream px-3 py-2 text-sm text-ink outline-none transition focus:border-honey-dark"
        />
        {fieldErrors('Name')}
      </div>

      <div>
        <label htmlFor="sku" className="text-sm font-medium text-ink-muted">
          SKU
        </label>
        <input
          id="sku"
          required
          value={form.sku}
          onChange={(event) => setForm((current) => ({ ...current, sku: event.target.value }))}
          className="mt-1 w-full rounded-lg border border-line bg-cream px-3 py-2 text-sm text-ink outline-none transition focus:border-honey-dark"
        />
        {fieldErrors('SKU')}
      </div>

      <div className="sm:col-span-2">
        <label htmlFor="desc" className="text-sm font-medium text-ink-muted">
          Descrição
        </label>
        <input
          id="desc"
          required
          value={form.desc}
          onChange={(event) => setForm((current) => ({ ...current, desc: event.target.value }))}
          className="mt-1 w-full rounded-lg border border-line bg-cream px-3 py-2 text-sm text-ink outline-none transition focus:border-honey-dark"
        />
        {fieldErrors('Desc')}
      </div>

      <div>
        <label htmlFor="price" className="text-sm font-medium text-ink-muted">
          Preço
        </label>
        <input
          id="price"
          type="number"
          step="0.01"
          min="0"
          required
          value={form.price}
          onChange={(event) =>
            setForm((current) => ({ ...current, price: Number(event.target.value) }))
          }
          className="mt-1 w-full rounded-lg border border-line bg-cream px-3 py-2 text-sm text-ink outline-none transition focus:border-honey-dark"
        />
        {fieldErrors('Price')}
      </div>

      {fieldErrors('geral')}

      <div className="flex items-center gap-3 sm:col-span-2">
        <button
          type="submit"
          disabled={submitting}
          className="rounded-full bg-honey px-6 py-2 text-sm font-semibold text-hive transition hover:bg-honey-light disabled:opacity-60"
        >
          {submitting ? 'Criando...' : 'Criar produto'}
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
