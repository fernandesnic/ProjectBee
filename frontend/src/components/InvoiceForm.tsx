import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import {
  createInvoice,
  formErrorsFrom,
  getSalesOrder,
  type Invoice,
  type SalesOrderDetail,
} from '../services/api'
import { formatDateTime } from '../services/date'
import { formatCurrency } from '../services/price'

interface InvoiceFormProps {
  salesOrderId: string
  onDone: (invoice: Invoice) => void
  onCancel: () => void
  onConflict: (message: string) => void
  onBusyChange?: (busy: boolean) => void
}

// O pedido é relido ao abrir: a lista não traz o RowVersion, e a nota precisa da versão atual.
export function InvoiceForm({ salesOrderId, onDone, onCancel, onConflict, onBusyChange }: InvoiceFormProps) {
  const [order, setOrder] = useState<SalesOrderDetail | null>(null)
  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    getSalesOrder(salesOrderId)
      .then(setOrder)
      .catch(() => setErrors({ geral: ['Não foi possível carregar o pedido.'] }))
  }, [salesOrderId])

  function setBusy(busy: boolean) {
    setSubmitting(busy)
    onBusyChange?.(busy)
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (!order) return
    setErrors({})
    setBusy(true)
    try {
      const invoice = await createInvoice(order.id, order.rowVersion)
      setBusy(false)
      onDone(invoice)
    } catch (err) {
      setBusy(false)
      if (axios.isAxiosError(err) && err.response?.status === 409) {
        onConflict(err.response.data?.detail ?? 'O pedido foi alterado por outra operação. Confira antes de faturar.')
        return
      }
      setErrors(formErrorsFrom(err, 'Não foi possível emitir a nota.'))
    }
  }

  if (!order) {
    return errors.geral?.length ? (
      <p className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger" role="alert">
        {errors.geral.join(' ')}
      </p>
    ) : (
      <p className="text-sm text-ink-muted">Carregando pedido...</p>
    )
  }

  const total = order.items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0)
  const units = order.items.reduce((sum, item) => sum + item.quantity, 0)

  return (
    <form onSubmit={handleSubmit} className="grid gap-4">
      <p className="text-sm text-ink-muted">
        A nota baixa <span className="tabular-nums font-medium text-ink">{units.toLocaleString('pt-BR')}</span>{' '}
        unidade(s) do estoque de <span className="font-medium text-ink">{order.storageName}</span>. Os lotes mais
        antigos saem primeiro.
      </p>

      <ul className="divide-y divide-line rounded-xl border border-line bg-cream px-3 text-sm">
        {order.items.map((item) => (
          <li key={item.id} className="flex flex-wrap justify-between gap-x-4 py-2">
            <span className="text-ink">
              <span className="tabular-nums">{item.quantity.toLocaleString('pt-BR')}</span> × {item.productName}
            </span>
            <span className="tabular-nums text-ink-muted">{formatCurrency(item.quantity * item.unitPrice)}</span>
          </li>
        ))}
        <li className="flex justify-between py-2 font-semibold text-ink">
          Total
          <span className="tabular-nums">{formatCurrency(total)}</span>
        </li>
      </ul>

      {order.boleto?.paidAt && (
        <p className="text-xs text-ink-muted">
          Boleto pago em {formatDateTime(order.boleto.paidAt)}
          {order.boleto.paidBy && <span className="break-all"> por {order.boleto.paidBy}</span>}
        </p>
      )}

      {order.status === 'Invoiced' && (
        <p className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger" role="alert">
          Este pedido já foi faturado por outra pessoa. Feche e recarregue a lista.
        </p>
      )}

      {errors.geral?.length ? (
        <p className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger" role="alert">
          {errors.geral.join(' ')}
        </p>
      ) : null}

      <div className="flex flex-col-reverse gap-2 pt-1 sm:flex-row sm:items-center sm:justify-end sm:gap-3">
        <button
          type="button"
          onClick={onCancel}
          disabled={submitting}
          className="h-11 rounded-full px-5 text-sm font-medium text-ink-muted transition hover:text-ink disabled:opacity-60 sm:h-auto sm:py-2"
        >
          Voltar
        </button>
        <button
          type="submit"
          disabled={submitting || order.status !== 'Paid'}
          className="h-11 rounded-full bg-honey px-6 text-sm font-semibold text-hive transition hover:bg-honey-light disabled:opacity-60 sm:h-auto sm:py-2"
        >
          {submitting ? 'Emitindo...' : 'Emitir nota'}
        </button>
      </div>
    </form>
  )
}
