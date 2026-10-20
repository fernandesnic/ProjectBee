import { useState, type FormEvent } from 'react'
import axios from 'axios'
import { cancelPurchaseOrder, formErrorsFrom, type PurchaseOrderDetail } from '../services/api'
import { FieldError } from './FieldError'

interface CancelPurchaseFormProps {
  order: PurchaseOrderDetail
  onDone: (order: PurchaseOrderDetail) => void
  onCancel: () => void
  onConflict: (message: string) => void
  onBusyChange?: (busy: boolean) => void
}

export function CancelPurchaseForm({ order, onDone, onCancel, onConflict, onBusyChange }: CancelPurchaseFormProps) {
  const [reason, setReason] = useState('')
  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)

  const received = order.items.reduce((sum, item) => sum + item.receivedQuantity, 0)
  const pending = order.items.reduce((sum, item) => sum + item.quantity - item.receivedQuantity, 0)

  function setBusy(busy: boolean) {
    setSubmitting(busy)
    onBusyChange?.(busy)
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors({})
    setBusy(true)
    try {
      const updated = await cancelPurchaseOrder(order.id, { reason: reason.trim(), rowVersion: order.rowVersion })
      setBusy(false)
      onDone(updated)
    } catch (err) {
      setBusy(false)
      if (axios.isAxiosError(err) && err.response?.status === 409) {
        onConflict(err.response.data?.detail ?? 'O pedido foi alterado por outra operação. Confira antes de cancelar.')
        return
      }
      setErrors(formErrorsFrom(err, 'Não foi possível cancelar o pedido.'))
    }
  }

  return (
    <form onSubmit={handleSubmit} className="grid gap-4">
      <p className="text-sm text-ink-muted">
        {received > 0 ? (
          <>
            Cancela as <span className="tabular-nums font-medium text-ink">{pending.toLocaleString('pt-BR')}</span>{' '}
            unidade(s) pendentes. As{' '}
            <span className="tabular-nums font-medium text-ink">{received.toLocaleString('pt-BR')}</span> já
            recebidas continuam no estoque, e as notas lançadas seguem registradas.
          </>
        ) : (
          'Nada foi recebido ainda: o pedido inteiro deixa de esperar entrega.'
        )}
      </p>

      <div>
        <label htmlFor="cancel-reason" className="text-sm font-medium text-ink-muted">
          Motivo
        </label>
        <textarea
          id="cancel-reason"
          required
          minLength={5}
          maxLength={250}
          rows={3}
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          placeholder="Ex.: fornecedor sem estoque para o restante"
          className="mt-1 w-full resize-none rounded-lg border border-line bg-cream px-3 py-2.5 text-base text-ink outline-none transition focus:border-honey-dark sm:text-sm"
        />
        <p className="mt-1 text-right text-xs tabular-nums text-ink-muted">{reason.length}/250</p>
        <FieldError messages={errors.Reason} />
      </div>

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
          disabled={submitting}
          className="h-11 rounded-full bg-danger px-6 text-sm font-semibold text-white transition hover:bg-danger-dark disabled:opacity-60 sm:h-auto sm:py-2"
        >
          {submitting ? 'Cancelando...' : received > 0 ? 'Cancelar pendente' : 'Cancelar pedido'}
        </button>
      </div>
    </form>
  )
}
