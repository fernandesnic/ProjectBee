import { useState, type FormEvent } from 'react'
import {
  createProduct,
  formErrorsFrom,
  updateProduct,
  type CreateProductPayload,
  type Product,
} from '../services/api'

const WITH_THOUSANDS = /^\d{1,3}(\.\d{3})+(,\d{1,2})?$/ // 1.234 ou 1.234,56
const PLAIN = /^\d+([.,]\d{1,2})?$/ // 1234, 12,50 ou 12.50

// Aceita "12,50", "12.50", "1234,56" e "1.234,56". Fora isso, devolve null.
function parsePrice(text: string): number | null {
  const trimmed = text.trim()
  if (WITH_THOUSANDS.test(trimmed)) {
    return Number(trimmed.replace(/\./g, '').replace(',', '.'))
  }
  if (PLAIN.test(trimmed)) {
    return Number(trimmed.replace(',', '.'))
  }
  return null
}

function formatPriceInput(price: number) {
  return price.toFixed(2).replace('.', ',')
}
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

  const [priceText, setPriceText] = useState(
    product ? formatPriceInput(product.price) : ''
  )

  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)

    async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors({})

    // Preço ilegível viraria NaN, que o JSON manda como null e a API recusa sem dizer o campo.
    const price = parsePrice(priceText)
    if (price === null) {
      setErrors({ Price: ['Informe o preço com até 2 casas decimais, por exemplo 12,50.'] })
      return
    }

    setSubmitting(true)
    try {
      if (product) {
        const updated = await updateProduct(product.id, {
          name: form.name,
          desc: form.desc,
          price,
          isActive: form.isActive,
          rowVersion: product.rowVersion,
        })
        onSaved(updated)
      } else {
        const created = await createProduct({ ...form, price })
        onSaved(created)
      }
    } catch (err) {
      setErrors(formErrorsFrom(err, 'Não foi possível salvar o produto.'))
    } finally {
      setSubmitting(false)
    }
  }

  function fieldErrors(field: string) {
    const messages = errors[field]
    if (!messages?.length) return null
    return (
      <p className="mt-1 text-xs text-danger" role="alert">
        {messages.join(' ')}
      </p>
    )
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
          autoFocus
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
          type="text"
          inputMode="decimal"
          placeholder="0,00"
          required
          value={priceText}
          onChange={(event) => setPriceText(event.target.value)}
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
