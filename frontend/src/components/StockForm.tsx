import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import {
  createStock,
  getProducts,
  getStorages,
  updateStock,
  type Product,
  type StockBalance,
  type Storage,
} from '../services/api'

interface StockFormProps {
  item?: StockBalance            // ausente = criação | presente = edição
  onSaved: () => void
  onCancel: () => void
}

export function StockForm({ item, onSaved, onCancel }: StockFormProps) {
  const isEditing = Boolean(item)

  const [products, setProducts] = useState<Product[]>([])
  const [storages, setStorages] = useState<Storage[]>([])
  const [loadingOptions, setLoadingOptions] = useState(!isEditing)

  const [form, setForm] = useState({
    productId: item?.productId ?? '',
    storageId: item?.storageId ?? '',
    batch: item?.batch ?? '',
    balance: item?.balance ?? 0,
  })

  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (isEditing) return          // na edição os selects nem aparecem

    Promise.all([getProducts(), getStorages()])
      .then(([loadedProducts, loadedStorages]) => {
        setProducts(loadedProducts.filter((p) => p.isActive))
        setStorages(loadedStorages.filter((s) => s.isActive))
      })
      .catch(() => setErrors({ geral: ['Não foi possível carregar produtos e armazéns.'] }))
      .finally(() => setLoadingOptions(false))
  }, [isEditing])

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors({})
    setSubmitting(true)

    try {
      if (item) {
        await updateStock(item.productId, item.storageId, item.batch, form.balance)
      } else {
        await createStock(form)
      }
      onSaved()
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 400) {
        setErrors(err.response.data?.errors ?? {})
      } else if (axios.isAxiosError(err) && err.response?.status === 409) {
        setErrors({ geral: ['Já existe saldo para este produto, armazém e lote.'] })
      } else {
        setErrors({ geral: ['Não foi possível salvar o saldo.'] })
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

  if (loadingOptions) {
    return <p className="text-sm text-ink-muted">Carregando produtos e armazéns...</p>
  }

  return (
    <form onSubmit={handleSubmit} className="grid gap-4 sm:grid-cols-2">
      {isEditing ? (
        <div className="sm:col-span-2 rounded-lg border border-line bg-cream px-4 py-3 text-sm">
          <p className="text-ink">
            <span className="font-medium">{item!.productName}</span> em{' '}
            <span className="font-medium">{item!.storageName}</span>
          </p>
          <p className="mt-1 text-xs text-ink-muted">
            Lote <span className="font-mono">{item!.batch}</span> — produto, armazém e lote não
            podem ser alterados.
          </p>
        </div>
      ) : (
        <>
          <div>
            <label htmlFor="productId" className="text-sm font-medium text-ink-muted">
              Produto
            </label>
            <select
              id="productId"
              required
              value={form.productId}
              onChange={(event) =>
                setForm((current) => ({ ...current, productId: event.target.value }))
              }
              className={inputClass}
            >
              <option value="">Selecione...</option>
              {products.map((product) => (
                <option key={product.id} value={product.id}>
                  {product.name} ({product.sku})
                </option>
              ))}
            </select>
            {fieldErrors('ProductId')}
          </div>

          <div>
            <label htmlFor="storageId" className="text-sm font-medium text-ink-muted">
              Armazém
            </label>
            <select
              id="storageId"
              required
              value={form.storageId}
              onChange={(event) =>
                setForm((current) => ({ ...current, storageId: event.target.value }))
              }
              className={inputClass}
            >
              <option value="">Selecione...</option>
              {storages.map((storage) => (
                <option key={storage.id} value={storage.id}>
                  {storage.name} ({storage.idNumber})
                </option>
              ))}
            </select>
            {fieldErrors('StorageId')}
          </div>

          <div>
            <label htmlFor="batch" className="text-sm font-medium text-ink-muted">
              Lote
            </label>
            <input
              id="batch"
              required
              value={form.batch}
              onChange={(event) =>
                setForm((current) => ({ ...current, batch: event.target.value }))
              }
              className={inputClass}
            />
            <p className="mt-1 text-xs text-ink-muted">Apenas letras e números, sem hífen.</p>
            {fieldErrors('Batch')}
          </div>
        </>
      )}

      <div>
        <label htmlFor="balance" className="text-sm font-medium text-ink-muted">
          Saldo
        </label>
        <input
          id="balance"
          type="number"
          min="1"
          step="1"
          required
          value={form.balance}
          onChange={(event) =>
            setForm((current) => ({ ...current, balance: Number(event.target.value) }))
          }
          className={inputClass}
        />
        {fieldErrors('Balance')}
      </div>

      {fieldErrors('geral')}

      <div className="flex items-center gap-3 sm:col-span-2">
        <button
          type="submit"
          disabled={submitting}
          className="rounded-full bg-honey px-6 py-2 text-sm font-semibold text-hive transition hover:bg-honey-light disabled:opacity-60"
        >
          {submitting ? 'Salvando...' : isEditing ? 'Salvar saldo' : 'Registrar saldo'}
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