import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  getInvoices,
  getSalesOrders,
  invoiceCode,
  salesOrderCode,
  type Invoice,
  type SalesOrderSummary,
} from '../services/api'
import { formatDateTime } from '../services/date'
import { formatCurrency } from '../services/price'
import { InvoiceForm } from '../components/InvoiceForm'
import { InvoiceList } from '../components/InvoiceList'
import { Modal } from '../components/Modal'
import { Toast } from '../components/Toast'

function InvoicesPage() {
  const [ready, setReady] = useState<SalesOrderSummary[]>([])
  const [invoices, setInvoices] = useState<Invoice[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [toast, setToast] = useState<string | null>(null)

  const [invoicing, setInvoicing] = useState<SalesOrderSummary | null>(null)
  const [invoiceBusy, setInvoiceBusy] = useState(false)
  const closeInvoicing = useCallback(() => setInvoicing(null), [])
  const dismissToast = useCallback(() => setToast(null), [])

  const load = useCallback(
    () =>
      Promise.all([getSalesOrders('Paid'), getInvoices()])
        .then(([paidOrders, issued]) => {
          // Os pedidos pagos há mais tempo aparecem primeiro: são os próximos a sair.
          setReady([...paidOrders].sort((a, b) => a.number - b.number))
          setInvoices(issued)
        })
        .catch(() => setError('Não foi possível carregar os pedidos e as notas.'))
        .finally(() => setLoading(false)),
    [],
  )

  useEffect(() => {
    load()
  }, [load])

  function handleIssued(invoice: Invoice) {
    setReady((current) => current.filter((order) => order.id !== invoice.salesOrderId))
    setInvoices((current) => [invoice, ...current])
    setInvoicing(null)
    setError(null)
    const units = invoice.items.reduce((sum, item) => sum + item.quantity, 0)
    setToast(`Nota ${invoiceCode(invoice.number)} emitida. ${units} unidade(s) saíram do estoque.`)
  }

  // Outra pessoa faturou no meio: o modal fecha e as listas são relidas.
  function handleConflict(message: string) {
    setInvoicing(null)
    setError(message)
    load()
  }

  const readyTotal = ready.reduce((sum, order) => sum + order.total, 0)

  return (
    <div>
      <h1 className="text-2xl font-bold tracking-tight text-ink">Notas fiscais</h1>

      <div className="mt-6 grid gap-3 sm:grid-cols-2 sm:gap-4">
        <div className="rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
          <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">Prontos para faturar</p>
          <p className="mt-2 text-xl font-bold tabular-nums text-ink sm:text-2xl">
            {loading ? '—' : ready.length}
          </p>
          {!loading && ready.length > 0 && (
            <p className="mt-1 text-xs tabular-nums text-ink-muted">{formatCurrency(readyTotal)} pagos</p>
          )}
        </div>
        <div className="rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
          <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">Notas emitidas</p>
          <p className="mt-2 text-xl font-bold tabular-nums text-ink sm:text-2xl">
            {loading ? '—' : invoices.length}
          </p>
          {!loading && invoices.length > 0 && (
            <p className="mt-1 text-xs tabular-nums text-ink-muted">
              {formatCurrency(invoices.reduce((sum, invoice) => sum + invoice.total, 0))} faturados
            </p>
          )}
        </div>
      </div>

      {error && (
        <p className="mt-4 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger" role="alert">
          {error}
        </p>
      )}

      <section className="mt-6 rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
        <h2 className="text-sm font-semibold text-ink">Pedidos pagos, aguardando nota</h2>
        <div className="mt-3">
          {loading ? (
            <p className="text-sm text-ink-muted">Carregando pedidos...</p>
          ) : ready.length === 0 ? (
            <p className="text-sm text-ink-muted">
              Nenhum pedido pago aguardando nota. Os pedidos aparecem aqui depois que o boleto é pago.
            </p>
          ) : (
            <ul className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
              {ready.map((order) => (
                <li key={order.id} className="flex flex-col rounded-xl border border-line bg-cream p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <Link
                        to={`/app/sales/${order.id}`}
                        className="font-mono text-sm font-medium text-ink underline-offset-2 hover:text-honey-dark hover:underline"
                      >
                        {salesOrderCode(order.number)}
                      </Link>
                      <p className="truncate text-sm text-ink">{order.customerName}</p>
                    </div>
                    <p className="shrink-0 text-sm font-semibold tabular-nums text-ink">
                      {formatCurrency(order.total)}
                    </p>
                  </div>
                  <p className="mt-2 text-xs text-ink-muted">
                    <span className="tabular-nums">{order.units.toLocaleString('pt-BR')}</span> unidade(s) ·{' '}
                    {order.storageName} · {formatDateTime(order.createdAt)}
                  </p>
                  <button
                    type="button"
                    onClick={() => setInvoicing(order)}
                    className="mt-3 h-11 rounded-full bg-honey px-5 text-sm font-semibold text-hive transition hover:bg-honey-light sm:h-auto sm:py-2"
                  >
                    Gerar nota
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>

      <section className="mt-6 rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
        <h2 className="text-sm font-semibold text-ink">Notas emitidas</h2>
        <div className="mt-3">
          {loading ? <p className="text-sm text-ink-muted">Carregando notas...</p> : <InvoiceList invoices={invoices} />}
        </div>
      </section>

      <Modal
        open={invoicing !== null}
        title="Emitir nota fiscal"
        subtitle={
          invoicing && (
            <>
              <span className="font-mono font-medium text-ink">{salesOrderCode(invoicing.number)}</span> ·{' '}
              {invoicing.customerName}
            </>
          )
        }
        busy={invoiceBusy}
        onClose={closeInvoicing}
      >
        {invoicing && (
          <InvoiceForm
            salesOrderId={invoicing.id}
            onDone={handleIssued}
            onCancel={closeInvoicing}
            onConflict={handleConflict}
            onBusyChange={setInvoiceBusy}
          />
        )}
      </Modal>

      <Toast message={toast} onDismiss={dismissToast} />
    </div>
  )
}

export default InvoicesPage
