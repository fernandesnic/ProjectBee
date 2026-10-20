import { useState, type FormEvent } from 'react'
import axios from 'axios'
import {
  formErrorsFrom,
  receivePurchaseOrder,
  type PurchaseOrderDetail,
} from '../services/api'
import { formatCurrency, formatPriceInput, parsePrice } from '../services/price'
import { FieldError } from './FieldError'

interface ReceiptFormProps {
  order: PurchaseOrderDetail
  onDone: (order: PurchaseOrderDetail) => void
  onCancel: () => void
  onConflict: (message: string) => void // o pedido mudou desde a leitura: a página recarrega
}

interface Row {
  itemId: string
  quantity: string
  batch: string
  price: string
}

type Errors = Record<string, string[]>

const inputClass =
  'mt-1 w-full rounded-lg border border-line bg-cream px-3 py-2 text-sm text-ink outline-none transition focus:border-honey-dark'

export function ReceiptForm({ order, onDone, onCancel, onConflict }: ReceiptFormProps) {
  const pendingItems = order.items.filter((item) => item.receivedQuantity < item.quantity)
  const itemsById = new Map(order.items.map((item) => [item.id, item]))

  const [invoiceNumber, setInvoiceNumber] = useState('')
  const [divergenceReason, setDivergenceReason] = useState('')
  // Começa com o pendente inteiro e o preço do pedido: o caso comum é a nota bater com o pedido.
  const [rows, setRows] = useState<Row[]>(() =>
    pendingItems.map((item) => ({
      itemId: item.id,
      quantity: String(item.quantity - item.receivedQuantity),
      batch: '',
      price: formatPriceInput(item.unitPrice),
    })),
  )
  const [errors, setErrors] = useState<Errors>({})
  const [submitting, setSubmitting] = useState(false)

  function updateRow(itemId: string, changes: Partial<Row>) {
    setRows((current) => current.map((row) => (row.itemId === itemId ? { ...row, ...changes } : row)))
  }

  // Linha com quantidade vazia ou zero fica de fora da nota (o item continua pendente).
  const included = rows.filter((row) => Number(row.quantity) > 0)
  const divergent = included.filter((row) => {
    const price = parsePrice(row.price)
    return price !== null && price !== itemsById.get(row.itemId)?.unitPrice
  })

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors({})

    const local: Errors = {}
    if (included.length === 0) local.geral = ['Informe a quantidade recebida de pelo menos um item.']
    for (const row of included) {
      const item = itemsById.get(row.itemId)!
      const pending = item.quantity - item.receivedQuantity
      if (Number(row.quantity) > pending)
        local[`${row.itemId}.Quantity`] = [
          `Maior que o pendente (${pending.toLocaleString('pt-BR')}). Recuse o excedente ou faça um novo pedido para ele.`,
        ]
      if (parsePrice(row.price) === null)
        local[`${row.itemId}.UnitPrice`] = ['Informe o preço com até 2 casas decimais, por exemplo 12,50.']
    }
    if (Object.keys(local).length > 0) {
      setErrors(local)
      return
    }

    setSubmitting(true)
    try {
      const updated = await receivePurchaseOrder(order.id, {
        invoiceNumber: invoiceNumber.trim(),
        priceDivergenceReason: divergent.length > 0 ? divergenceReason.trim() : null,
        rowVersion: order.rowVersion,
        items: included.map((row) => ({
          purchaseOrderItemId: row.itemId,
          batch: row.batch,
          quantity: Number(row.quantity),
          unitPrice: parsePrice(row.price)!,
        })),
      })
      setSubmitting(false)
      onDone(updated)
    } catch (err) {
      setSubmitting(false)
      if (axios.isAxiosError(err) && err.response?.status === 409) {
        onConflict(err.response.data?.detail ?? 'O pedido foi alterado por outra operação. Confira o pendente atualizado.')
        return
      }
      // A API devolve erros por posição na lista enviada (Items[0].Batch); aqui cada linha é um item.
      const result: Errors = {}
      for (const [key, messages] of Object.entries(formErrorsFrom(err, 'Não foi possível registrar o recebimento.'))) {
        const match = /^Items\[(\d+)\]\.(\w+)$/.exec(key)
        const row = match ? included[Number(match[1])] : undefined
        result[row ? `${row.itemId}.${match![2]}` : key] = messages
      }
      setErrors(result)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="grid gap-4">
      <div className="sm:max-w-xs">
        <label htmlFor="rc-invoice" className="text-sm font-medium text-ink-muted">
          Número da nota fiscal
        </label>
        <input
          id="rc-invoice"
          required
          autoFocus
          maxLength={20}
          value={invoiceNumber}
          onChange={(event) => setInvoiceNumber(event.target.value)}
          className={`${inputClass} font-mono`}
        />
        <FieldError messages={errors.InvoiceNumber} />
      </div>

      <fieldset className="grid gap-3">
        <legend className="text-sm font-medium text-ink-muted">Itens recebidos</legend>
        {rows.map((row) => {
          const item = itemsById.get(row.itemId)!
          const pending = item.quantity - item.receivedQuantity
          const price = parsePrice(row.price)
          const diverges = Number(row.quantity) > 0 && price !== null && price !== item.unitPrice
          return (
            <div key={row.itemId} className="rounded-xl border border-line bg-cream p-3">
              <div className="flex flex-wrap items-baseline justify-between gap-x-4">
                <p className="text-sm font-medium text-ink">{item.productName}</p>
                <p className="text-xs text-ink-muted">
                  Pendente <span className="tabular-nums">{pending.toLocaleString('pt-BR')}</span> · pedido a{' '}
                  <span className="tabular-nums">{formatCurrency(item.unitPrice)}</span>
                </p>
              </div>
              <div className="mt-2 grid grid-cols-2 gap-3 sm:grid-cols-3">
                <div>
                  <label htmlFor={`rc-qty-${row.itemId}`} className="text-xs text-ink-muted">
                    Quantidade
                  </label>
                  <input
                    id={`rc-qty-${row.itemId}`}
                    type="number"
                    inputMode="numeric"
                    min="0"
                    step="1"
                    value={row.quantity}
                    onChange={(event) => updateRow(row.itemId, { quantity: event.target.value })}
                    className={`${inputClass} tabular-nums`}
                  />
                  <FieldError messages={errors[`${row.itemId}.Quantity`]} />
                </div>
                <div>
                  <label htmlFor={`rc-batch-${row.itemId}`} className="text-xs text-ink-muted">
                    Lote
                  </label>
                  <input
                    id={`rc-batch-${row.itemId}`}
                    required={Number(row.quantity) > 0}
                    maxLength={30}
                    autoCapitalize="characters"
                    value={row.batch}
                    onChange={(event) => updateRow(row.itemId, { batch: event.target.value.toUpperCase() })}
                    className={`${inputClass} font-mono`}
                  />
                  <FieldError messages={errors[`${row.itemId}.Batch`]} />
                </div>
                <div className="col-span-2 sm:col-span-1">
                  <label htmlFor={`rc-price-${row.itemId}`} className="text-xs text-ink-muted">
                    Preço na nota
                  </label>
                  <input
                    id={`rc-price-${row.itemId}`}
                    inputMode="decimal"
                    required={Number(row.quantity) > 0}
                    value={row.price}
                    onChange={(event) => updateRow(row.itemId, { price: event.target.value })}
                    className={`${inputClass} tabular-nums ${diverges ? 'border-honey-dark' : ''}`}
                  />
                  {diverges && <p className="mt-1 text-xs text-honey-text">Diferente do pedido.</p>}
                  <FieldError messages={errors[`${row.itemId}.UnitPrice`]} />
                </div>
              </div>
            </div>
          )
        })}
        <p className="text-xs text-ink-muted">
          Deixe a quantidade em 0 para o item que não veio nesta nota: ele continua pendente.
        </p>
      </fieldset>

      {divergent.length > 0 && (
        <div>
          <label htmlFor="rc-divergence" className="text-sm font-medium text-ink-muted">
            Motivo da divergência de preço
          </label>
          <textarea
            id="rc-divergence"
            required
            minLength={5}
            maxLength={250}
            rows={2}
            value={divergenceReason}
            onChange={(event) => setDivergenceReason(event.target.value)}
            placeholder="Ex.: reajuste de tabela confirmado pelo comprador"
            className={`${inputClass} resize-none`}
          />
          <p className="mt-1 text-xs text-ink-muted">
            O preço da nota é o que fica registrado no recebimento e no futuro título a pagar.
          </p>
          <FieldError messages={errors.PriceDivergenceReason} />
        </div>
      )}

      {errors.geral?.length ? (
        <p className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger" role="alert">
          {errors.geral.join(' ')}
        </p>
      ) : null}
      <FieldError messages={errors.Items} />

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={submitting}
          className="rounded-full bg-honey px-6 py-2 text-sm font-semibold text-hive transition hover:bg-honey-light disabled:opacity-60"
        >
          {submitting ? 'Registrando...' : 'Registrar recebimento'}
        </button>
        <button
          type="button"
          onClick={onCancel}
          disabled={submitting}
          className="text-sm text-ink-muted transition hover:text-ink disabled:opacity-60"
        >
          Cancelar
        </button>
      </div>
    </form>
  )
}
