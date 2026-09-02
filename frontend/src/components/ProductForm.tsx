import { useState, type FormEvent } from 'react'
import axios from 'axios'
import {
  createProduct,
  updateProduct,
  type CreateProductPayload,
  type Product,
} from '../services/api'
interface ProductFormProps {
  product?: Product              // ausente = criação | presente = edição
  onSaved: (product: Product) => void
  onCancel: () => void
}

type FormState = CreateProductPayload & { isActive: boolean }

export function ProductForm({ product, onSaved, onCancel }: ProductFormProps) {
  const isEditing = Boolean(product)

  const [form, setForm] = useState<FormState>({
    name: product?.name ?? '',
    sku: product?.sku ?? '',
    desc: product?.desc ?? '',
    price: product?.price ?? 0,
    isActive: product?.isActive ?? true,
  })

  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)

    async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors({})
    setSubmitting(true)

    try {
      if (product) {
        await updateProduct(product.id, {
          name: form.name,
          desc: form.desc,
          price: form.price,
          isActive: form.isActive,
        })
        onSaved({ ...product, ...form })
      } else {
        const created = await createProduct(form)
        onSaved(created)
      }
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 400) {
        setErrors(err.response.data?.errors ?? {})
      } else {
        setErrors({ geral: ['Não foi possível salvar o produto.'] })
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
            disabled={isEditing}
            value={form.sku}
            onChange={(event) => setForm((current) => ({ ...current, sku: event.target.value }))}
            className={`mt-1 w-full rounded-lg border border-line bg-cream px-3 py-2 text-sm text-ink outline-none transition focus:border-honey-dark ${
              isEditing ? 'cursor-not-allowed opacity-60' : ''
            }`}
          />
          {isEditing && (
            <p className="mt-1 text-xs text-ink-muted">O SKU não pode ser alterado.</p>
          )}
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
      
      {isEditing && (
      <div className="flex items-center gap-2 self-end pb-2">
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
          Produto ativo
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
        {submitting ? 'Salvando...' : isEditing ? 'Salvar alterações' : 'Criar produto'}        
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
