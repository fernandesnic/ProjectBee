import { useEffect, useRef, useState } from 'react'
import { useNavigate, useOutletContext } from 'react-router-dom'
import {
  getSalesOrders,
  SALES_STATUS_LABEL,
  salesOrderCode,
  type SalesOrderDetail,
  type SalesOrderStatus,
  type SalesOrderSummary,
} from '../services/api'
import { formatCurrency } from '../services/price'
import { SalesOrderTable } from '../components/SalesOrderTable'
import { SalesOrderForm } from '../components/SalesOrderForm'
import type { AppLayoutContext } from '../components/AppLayout'

type Filter = 'all' | SalesOrderStatus

const FILTERS: Filter[] = ['all', 'AwaitingPayment', 'Paid', 'Invoiced']

function SalesOrdersPage() {
  const { roles } = useOutletContext<AppLayoutContext>()
  const canManage = roles.includes('Manager')
  const navigate = useNavigate()

  const [orders, setOrders] = useState<SalesOrderSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [filter, setFilter] = useState<Filter>('all')
  const [creating, setCreating] = useState(false)
  const newButtonRef = useRef<HTMLButtonElement>(null)
  const formWasOpen = useRef(false)

  // O botão "Novo" some enquanto o formulário está aberto; ao fechar, o foco volta para ele.
  useEffect(() => {
    if (!creating && formWasOpen.current) newButtonRef.current?.focus()
    formWasOpen.current = creating
  }, [creating])

  useEffect(() => {
    getSalesOrders()
      .then(setOrders)
      .catch(() => setError('Não foi possível carregar os pedidos de venda.'))
      .finally(() => setLoading(false))
  }, [])

  // O pedido novo abre direto no detalhe, onde está o boleto que acabou de ser emitido.
  function handleCreated(order: SalesOrderDetail) {
    navigate(`/app/sales/${order.id}`, {
      state: { toast: `Pedido ${salesOrderCode(order.number)} criado e boleto emitido.` },
    })
  }

  const awaiting = orders.filter((order) => order.status === 'AwaitingPayment')
  const toInvoice = orders.filter((order) => order.status === 'Paid')
  const countOf = (f: Filter) => (f === 'all' ? orders.length : orders.filter((o) => o.status === f).length)
  const visible = filter === 'all' ? orders : orders.filter((order) => order.status === filter)

  return (
    <div>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold tracking-tight text-ink">Pedidos de venda</h1>
        {canManage && !creating && (
          <button
            ref={newButtonRef}
            onClick={() => setCreating(true)}
            className="h-11 rounded-full bg-honey px-5 text-sm font-semibold text-hive transition hover:bg-honey-light sm:h-auto sm:py-2"
          >
            Novo pedido
          </button>
        )}
      </div>

      {canManage && creating && (
        <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
          <SalesOrderForm onSaved={handleCreated} onCancel={() => setCreating(false)} />
        </div>
      )}

      <div className="mt-6 grid gap-3 sm:grid-cols-2 sm:gap-4">
        <div className="rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
          <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
            Boletos em aberto
          </p>
          <p className="mt-2 text-xl font-bold tabular-nums text-ink sm:text-2xl">
            {loading ? '—' : formatCurrency(awaiting.reduce((sum, order) => sum + order.total, 0))}
          </p>
          {!loading && (
            <p className="mt-1 text-xs text-ink-muted">
              {awaiting.length} pedido(s) aguardando pagamento
            </p>
          )}
        </div>
        <div className="rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
          <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
            Pagos a faturar
          </p>
          <p className="mt-2 text-xl font-bold tabular-nums text-ink sm:text-2xl">
            {loading ? '—' : toInvoice.length}
          </p>
        </div>
      </div>

      {error && (
        <p className="mt-4 text-sm text-danger" role="alert">
          {error}
        </p>
      )}

      <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-3 sm:p-6">
        <div className="-mx-1 mb-4 flex gap-2 overflow-x-auto px-1 pb-1" role="group" aria-label="Filtrar por status">
          {FILTERS.map((f) => (
            <button
              key={f}
              type="button"
              onClick={() => setFilter(f)}
              aria-pressed={filter === f}
              className={`shrink-0 rounded-full px-3 py-1.5 text-sm font-medium transition ${
                filter === f ? 'bg-honey/15 text-honey-text' : 'text-ink-muted hover:bg-cream hover:text-ink'
              }`}
            >
              {f === 'all' ? 'Todos' : SALES_STATUS_LABEL[f]}
              {!loading && <span className="ml-1.5 tabular-nums opacity-70">{countOf(f)}</span>}
            </button>
          ))}
        </div>

        {loading ? (
          <p className="text-sm text-ink-muted">Carregando pedidos...</p>
        ) : (
          <SalesOrderTable orders={visible} />
        )}
      </div>
    </div>
  )
}

export default SalesOrdersPage
