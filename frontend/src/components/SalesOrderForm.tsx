import { useEffect, useState, type FormEvent } from 'react'
import {
  createSalesOrder,
  formErrorsFrom,
  getCustomers,
  getProducts,
  getStock,
  getStorages,
  PAYMENT_METHOD_LABEL,
  type Customer,
  type PaymentMethod,
  type Product,
  type SalesOrderDetail,
  type StockBalance,
  type Storage,
} from '../services/api'
import { formatCurrency } from '../services/price'
import { FieldError } from './FieldError'

interface SalesOrderFormProps {
  onSaved: (order: SalesOrderDetail) => void
  onCancel: () => void
}

interface ItemRow {
  key: number
  productId: string
  quantity: string
}

// Pix e cartão aparecem para mostrar o caminho, mas só o boleto está disponível por enquanto.
const PAYMENT_METHODS: { value: PaymentMethod; available: boolean }[] = [
  { value: 'Boleto', available: true },
  { value: 'Pix', available: false },
  { value: 'CreditCard', available: false },
]

let nextKey = 1
const emptyRow = (): ItemRow => ({ key: nextKey++, productId: '', quantity: '' })

const inputClass =
  'mt-1 w-full rounded-lg border border-line bg-cream px-3 py-2 text-sm text-ink outline-none transition focus:border-honey-dark'

export function SalesOrderForm({ onSaved, onCancel }: SalesOrderFormProps) {
  const [customers, setCustomers] = useState<Customer[]>([])
  const [storages, setStorages] = useState<Storage[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [stock, setStock] = useState<StockBalance[]>([])
  const [loadingOptions, setLoadingOptions] = useState(true)

  const [customerId, setCustomerId] = useState('')
  const [storageId, setStorageId] = useState('')
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>('Boleto')
  const [notes, setNotes] = useState('')
  const [rows, setRows] = useState<ItemRow[]>(() => [emptyRow()])

  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    Promise.all([getCustomers(), getStorages(), getProducts(), getStock()])
      .then(([loadedCustomers, loadedStorages, loadedProducts, loadedStock]) => {
        const byName = (a: { name: string }, b: { name: string }) => a.name.localeCompare(b.name, 'pt-BR')
        setCustomers(loadedCustomers.filter((c) => c.isActive).sort(byName))
        setStorages(loadedStorages.filter((s) => s.isActive).sort(byName))
        setProducts(loadedProducts.filter((p) => p.isActive).sort(byName))
        setStock(loadedStock)
      })
      .catch(() => setErrors({ geral: ['Não foi possível carregar clientes, armazéns e produtos.'] }))
      .finally(() => setLoadingOptions(false))
  }, [])

  function updateRow(key: number, changes: Partial<ItemRow>) {
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...changes } : row)))
  }

  // Saldo físico do armazém escolhido. A API ainda desconta o que outros pedidos não faturados
  // já venderam, então o pedido pode ser recusado mesmo com o número daqui parecendo suficiente.
  function inStock(productId: string) {
    return stock
      .filter((item) => item.productId === productId && item.storageId === storageId)
      .reduce((sum, item) => sum + item.balance, 0)
  }

  const priceOf = (productId: string) => products.find((p) => p.id === productId)?.price ?? 0

  const total = rows.reduce((sum, row) => {
    const quantity = Number(row.quantity)
    return Number.isInteger(quantity) ? sum + priceOf(row.productId) * quantity : sum
  }, 0)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors({})
    setSubmitting(true)
    try {
      const items = rows.map((row) => ({ productId: row.productId, quantity: Number(row.quantity) }))
      onSaved(await createSalesOrder({ customerId, storageId, paymentMethod, notes: notes.trim(), items }))
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
          <label htmlFor="so-customer" className="text-sm font-medium text-ink-muted">
            Cliente
          </label>
          <select
            id="so-customer"
            required
            autoFocus
            value={customerId}
            onChange={(event) => setCustomerId(event.target.value)}
            className={inputClass}
          >
            <option value="">Selecione...</option>
            {customers.map((customer) => (
              <option key={customer.id} value={customer.id}>
                {customer.name}
              </option>
            ))}
          </select>
          <FieldError messages={errors.CustomerId} />
        </div>

        <div>
          <label htmlFor="so-storage" className="text-sm font-medium text-ink-muted">
            Armazém de expedição
          </label>
          <select
            id="so-storage"
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

      <fieldset>
        <legend className="text-sm font-medium text-ink-muted">Condição de pagamento</legend>
        <div className="mt-1 grid gap-2 min-[400px]:grid-cols-3">
          {PAYMENT_METHODS.map((method) => (
            <label
              key={method.value}
              className={`flex h-11 items-center gap-2 rounded-lg border px-3 text-sm transition ${
                !method.available
                  ? 'cursor-not-allowed border-line text-ink-muted/50'
                  : paymentMethod === method.value
                    ? 'cursor-pointer border-honey-dark bg-honey/15 font-medium text-honey-text'
                    : 'cursor-pointer border-line bg-cream text-ink hover:border-honey-dark'
              }`}
            >
              <input
                type="radio"
                name="so-payment"
                value={method.value}
                checked={paymentMethod === method.value}
                disabled={!method.available}
                onChange={() => setPaymentMethod(method.value)}
                className="accent-honey"
              />
              {PAYMENT_METHOD_LABEL[method.value]}
              {!method.available && (
                <span className="ml-auto rounded-full bg-line/60 px-2 py-0.5 text-[10px] font-medium">em breve</span>
              )}
            </label>
          ))}
        </div>
        <p className="mt-1 text-xs text-ink-muted">O boleto é emitido junto com o pedido, com vencimento em 3 dias.</p>
        <FieldError messages={errors.PaymentMethod} />
      </fieldset>

      <fieldset className="grid gap-3">
        <legend className="text-sm font-medium text-ink-muted">Itens</legend>
        {rows.map((row, index) => {
          const available = row.productId && storageId ? inStock(row.productId) : null
          const quantity = Number(row.quantity)
          return (
            <div
              key={row.key}
              className="grid grid-cols-2 gap-3 rounded-xl border border-line bg-cream p-3 sm:grid-cols-[minmax(0,1fr)_7rem_8rem_auto] sm:items-start sm:border-0 sm:bg-transparent sm:p-0"
            >
              <div className="col-span-2 sm:col-span-1">
                <label htmlFor={`so-product-${row.key}`} className="text-xs text-ink-muted">
                  Produto
                </label>
                <select
                  id={`so-product-${row.key}`}
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
                {available !== null && (
                  <p
                    className={`mt-1 text-xs tabular-nums ${
                      quantity > available ? 'text-danger' : 'text-ink-muted'
                    }`}
                  >
                    Em estoque no armazém: {available.toLocaleString('pt-BR')}
                  </p>
                )}
                <FieldError messages={errors[`Items[${index}].ProductId`]} />
              </div>

              <div>
                <label htmlFor={`so-quantity-${row.key}`} className="text-xs text-ink-muted">
                  Quantidade
                </label>
                <input
                  id={`so-quantity-${row.key}`}
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
                <p className="text-xs text-ink-muted">Preço unitário</p>
                <p className="mt-1 flex h-[38px] items-center text-sm tabular-nums text-ink">
                  {row.productId ? formatCurrency(priceOf(row.productId)) : '—'}
                </p>
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
          )
        })}

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
        <label htmlFor="so-notes" className="text-sm font-medium text-ink-muted">
          Observação <span className="font-normal">(opcional)</span>
        </label>
        <textarea
          id="so-notes"
          rows={2}
          maxLength={500}
          value={notes}
          onChange={(event) => setNotes(event.target.value)}
          className={`${inputClass} resize-none`}
        />
        <FieldError messages={errors.Notes} />
      </div>

      {errors.geral?.length ? (
        <p className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger" role="alert">
          {errors.geral.join(' ')}
        </p>
      ) : null}

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={submitting}
          className="rounded-full bg-honey px-6 py-2 text-sm font-semibold text-hive transition hover:bg-honey-light disabled:opacity-60"
        >
          {submitting ? 'Salvando...' : 'Criar pedido e emitir boleto'}
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
