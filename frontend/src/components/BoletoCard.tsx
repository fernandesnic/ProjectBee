import { useState } from 'react'
import { formatDigitableLine, type Boleto } from '../services/api'
import { formatDate, formatDateTime } from '../services/date'
import { formatCurrency } from '../services/price'

interface BoletoCardProps {
  boleto: Boleto
  onPay?: () => void // ausente: o perfil não pode simular o pagamento
  paying?: boolean
}

export function BoletoCard({ boleto, onPay, paying = false }: BoletoCardProps) {
  const [copied, setCopied] = useState(false)
  const isPaid = boleto.status === 'Paid'

  async function copyLine() {
    try {
      await navigator.clipboard.writeText(boleto.digitableLine)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      setCopied(false)
    }
  }

  return (
    <div>
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-xs text-ink-muted">Valor</p>
          <p className="text-2xl font-bold tabular-nums text-ink">{formatCurrency(boleto.amount)}</p>
        </div>
        <span
          className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-medium ${
            isPaid ? 'bg-honey/15 text-honey-text' : 'bg-honey-light/60 text-ink'
          }`}
        >
          {isPaid ? 'Pago' : 'Aguardando pagamento'}
        </span>
      </div>

      <dl className="mt-4 grid grid-cols-2 gap-4 text-sm sm:grid-cols-3">
        <div>
          <dt className="text-xs text-ink-muted">Vencimento</dt>
          <dd className="tabular-nums text-ink">{formatDate(boleto.dueDate)}</dd>
        </div>
        <div>
          <dt className="text-xs text-ink-muted">Nosso número</dt>
          <dd className="break-all font-mono text-ink">{boleto.ourNumber}</dd>
        </div>
        {isPaid && boleto.paidAt && (
          <div className="col-span-2 sm:col-span-1">
            <dt className="text-xs text-ink-muted">Pago em</dt>
            <dd className="text-ink">{formatDateTime(boleto.paidAt)}</dd>
            {boleto.paidBy && <dd className="break-all text-xs text-ink-muted">{boleto.paidBy}</dd>}
          </div>
        )}
      </dl>

      <div className="mt-4 rounded-xl border border-dashed border-line bg-cream p-3 sm:p-4">
        <p className="text-xs text-ink-muted">Linha digitável</p>
        <p className="mt-1 break-words font-mono text-sm tabular-nums text-ink sm:text-base">
          {formatDigitableLine(boleto.digitableLine)}
        </p>
        <button
          type="button"
          onClick={copyLine}
          className="mt-3 h-11 w-full rounded-full border border-line px-4 text-sm font-medium text-ink transition hover:border-honey-dark hover:text-honey-dark sm:h-auto sm:w-auto sm:py-1.5"
        >
          {copied ? 'Copiado ✓' : 'Copiar linha digitável'}
        </button>
      </div>

      {!isPaid && onPay && (
        <div className="mt-4 flex flex-col gap-2 border-t border-line pt-4 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-xs text-ink-muted">
            Boleto de demonstração: não está registrado em banco. A simulação faz o papel do retorno bancário.
          </p>
          <button
            type="button"
            onClick={onPay}
            disabled={paying}
            className="h-11 shrink-0 rounded-full bg-honey px-5 text-sm font-semibold text-hive transition hover:bg-honey-light disabled:opacity-60 sm:h-auto sm:py-2"
          >
            {paying ? 'Registrando pagamento...' : 'Simular pagamento'}
          </button>
        </div>
      )}
    </div>
  )
}
