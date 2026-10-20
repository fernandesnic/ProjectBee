import { useEffect, useState, type FormEvent } from 'react'
import {
  createPurchaseOrder,
  formErrorsFrom,
  getProducts,
  getStorages,
  getSuppliers,
  type Product,
  type PurchaseOrderDetail,
  type Storage,
  type Supplier,
} from '../services/api'
import { formatCurrency, parsePrice } from '../services/price'
import { FieldError } from './FieldError'

interface PurchaseOrderFormProps {
  onSaved: (order: PurchaseOrderDetail) => void
  onCancel: () => void
}

interface ItemRow {
  key: number
  productId: string
  quantity: string
  unitPrice: string
}

let nextKey = 1
const emptyRow = (): ItemRow => ({ key: nextKey++, productId: '', quantity: '', unitPrice: '' })

const inputClass =
  'mt-1 w-full rounded-lg border border-line bg-cream px-3 py-2 text-sm text-ink outline-none transition focus:border-honey-dark'

export function PurchaseOrderForm({ onSaved, onCancel }: PurchaseOrderFormProps) {
  const [suppliers, setSuppliers] = useState<Supplier[]>([])
  const [storages, setStorages] = useState<Storage[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [loadingOptions, setLoadingOptions] = useState(true)

  const [supplierId, setSupplierId] = useState('')
  const [storageId, setStorageId] = useState('')
  const [notes, setNotes] = useState('')
  const [rows, setRows] = useState<ItemRow[]>(() => [emptyRow()])

  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    Promise.all([getSuppliers(), getStorages(), getProducts()])
      .then(([loadedSuppliers, loadedStorages, loadedProducts]) => {
        const byName = (a: { name: string }, b: { name: string }) => a.name.localeCompare(b.name, 'pt-BR')
        setSuppliers(loadedSuppliers.filter((s) => s.isActive).sort(byName))
        setStorages(loadedStorages.filter((s) => s.isActive).sort(byName))
        setProducts(loadedProducts.filter((p) => p.isActive).sort(byName))
      })
      .catch(() => setErrors({ geral: ['Não foi possível carregar fornecedores, armazéns e produtos.'] }))
      .finally(() => setLoadingOptions(false))
  }, [])

  function updateRow(key: number, changes: Partial<ItemRow>) {
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...changes } : row)))
  }

  const total = rows.reduce((sum, row) => {
    const price = parsePrice(row.unitPrice)
    const quantity = Number(row.quantity)
    return price !== null && Number.isInteger(quantity) ? sum + price * quantity : sum
  }, 0)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors({})

    // Preço ilegível viraria NaN, que o JSON manda como null e a API recusa sem dizer o campo.
    const priceErrors: Record<string, string[]> = {}
    const items = rows.map((row, index) => {
      const unitPrice = parsePrice(row.unitPrice)
      if (unitPrice === null)
        priceErrors[`Items[${index}].UnitPrice`] = ['Informe o preço com até 2 casas decimais, por exemplo 12,50.']
      return { productId: row.productId, quantity: Number(row.quantity), unitPrice: unitPrice ?? 0 }
    })
    if (Object.keys(priceErrors).length > 0) {
      setErrors(priceErrors)
      return
    }

    setSubmitting(true)
    try {
      onSaved(await createPurchaseOrder({ supplierId, storageId, notes: notes.trim(), items }))
    } catch (err) {
      setErrors(formErrorsFrom(err, 'Não foi possível criar o pedido.'))
    } finally {
      setSubmitting(false)
    }
  }

  if (loadingOptions) {
    return <p className="text-sm text-ink-muted">Carregando opções...</p>
  }

  const chosen = new Set(rows.map((row) => row.productId))

  return (
    <form onSubmit={handleSubmit} className="grid gap-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label htmlFor="po-supplier" className="text-sm font-medium text-ink-muted">
            Fornecedor
          </label>
          <select
            id="po-supplier"
            required
            autoFocus
            value={supplierId}
            onChange={(event) => setSupplierId(event.target.value)}
            className={inputClass}
          >
            <option value="">Selecione...</option>
            {suppliers.map((supplier) => (
              <option key={supplier.id} value={supplier.id}>
                {supplier.name}
              </option>
            ))}
          </select>
          <FieldError messages={errors.SupplierId} />
        </div>

        <div>
          <label htmlFor="po-storage" className="text-sm font-medium text-ink-muted">
            Armazém de recebimento
          </label>
          <select
            id="po-storage"
            required
            value={storageId}
            onChange={(event) => setStorageId(event.target.value)}
            className={inputClass}
          >
            <option value="">Selecione...</option>
            {storages.map((storage) => (
              <option key={storage.id} value={storage.id}>
                {storage.name} ({storage.idNumber})
              </option>
            ))}
          </select>
          <FieldError messages={errors.StorageId} />
        </div>
      </div>

      <fieldset className="grid gap-3">
        <legend className="text-sm font-medium text-ink-muted">Itens</legend>
        {rows.map((row, index) => (
          <div
            key={row.key}
            className="grid grid-cols-2 gap-3 rounded-xl border border-line bg-cream p-3 sm:grid-cols-[minmax(0,1fr)_7rem_8rem_auto] sm:items-start sm:border-0 sm:bg-transparent sm:p-0"
          >
            <div className="col-span-2 sm:col-span-1">
              <label htmlFor={`po-product-${row.key}`} className="text-xs text-ink-muted">
                Produto
              </label>
              <select
                id={`po-product-${row.key}`}
                required
                value={row.productId}
                onChange={(event) => updateRow(row.key, { productId: event.target.value })}
                className={inputClass}
              >
                <option value="">Selecione...</option>
                {products.map((product) => (
                  <option
                    key={product.id}
                    value={product.id}
                    disabled={product.id !== row.productId && chosen.has(product.id)}
                  >
                    {product.name} ({product.sku})
                  </option>
                ))}
              </select>
              <FieldError messages={errors[`Items[${index}].ProductId`]} />
            </div>

            <div>
              <label htmlFor={`po-quantity-${row.key}`} className="text-xs text-ink-muted">
                Quantidade
              </label>
              <input
                id={`po-quantity-${row.key}`}
                type="number"
                inputMode="numeric"
                min="1"
                step="1"
                required
                value={row.quantity}
                onChange={(event) => updateRow(row.key, { quantity: event.target.value })}
                className={`${inputClass} tabular-nums`}
              />
              <FieldError messages={errors[`Items[${index}].Quantity`]} />
            </div>

            <div>
              <label htmlFor={`po-price-${row.key}`} className="text-xs text-ink-muted">
                Preço unitário
              </label>
              <input
                id={`po-price-${row.key}`}
                inputMode="decimal"
                placeholder="0,00"
                required
                value={row.unitPrice}
                onChange={(event) => updateRow(row.key, { unitPrice: event.target.value })}
                className={`${inputClass} tabular-nums`}
              />
              <FieldError messages={errors[`Items[${index}].UnitPrice`]} />
            </div>

            <button
              type="button"
              onClick={() => setRows((current) => current.filter((r) => r.key !== row.key))}
              disabled={rows.length === 1}
              aria-label={`Remover item ${index + 1}`}
              className="col-span-2 h-11 rounded-lg text-sm font-medium text-danger transition hover:bg-danger/10 disabled:invisible sm:col-span-1 sm:mt-5 sm:h-10 sm:px-3"
            >
              Remover
            </button>
          </div>
        ))}

        <div className="flex items-center justify-between gap-3">
          <button
            type="button"
            onClick={() => setRows((current) => [...current, emptyRow()])}
            disabled={rows.length >= products.length}
            className="rounded-full border border-line px-4 py-1.5 text-sm font-medium text-ink transition hover:border-honey-dark hover:text-honey-dark disabled:opacity-60"
          >
            Adicionar item
          </button>
          <p className="text-sm text-ink-muted">
            Total <span className="font-semibold tabular-nums text-ink">{formatCurrency(total)}</span>
          </p>
        </div>
        <FieldError messages={errors.Items} />
      </fieldset>

      <div>
        <label htmlFor="po-notes" className="text-sm font-medium text-ink-muted">
          Observação <span className="font-normal">(opcional)</span>
        </label>
        <textarea
          id="po-notes"
          rows={2}
          maxLength={500}
          value={notes}
          onChange={(event) => setNotes(event.target.value)}
          className={`${inputClass} resize-none`}
        />
        <FieldError messages={errors.Notes} />
      </div>

      <FieldError messages={errors.geral} />

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={submitting}
          className="rounded-full bg-honey px-6 py-2 text-sm font-semibold text-hive transition hover:bg-honey-light disabled:opacity-60"
        >
          {submitting ? 'Salvando...' : 'Criar pedido'}
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
