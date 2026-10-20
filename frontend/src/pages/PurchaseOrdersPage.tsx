import { useEffect, useRef, useState } from 'react'
import { useNavigate, useOutletContext } from 'react-router-dom'
import {
  getPurchaseOrders,
  isPending,
  PURCHASE_STATUS_LABEL,
  purchaseOrderCode,
  type PurchaseOrderDetail,
  type PurchaseOrderStatus,
  type PurchaseOrderSummary,
} from '../services/api'
import { PurchaseOrderTable } from '../components/PurchaseOrderTable'
import { PurchaseOrderForm } from '../components/PurchaseOrderForm'
import type { AppLayoutContext } from '../components/AppLayout'

type Filter = 'all' | PurchaseOrderStatus

const FILTERS: Filter[] = ['all', 'Open', 'Partial', 'Received', 'Cancelled']

function PurchaseOrdersPage() {
  const { roles } = useOutletContext<AppLayoutContext>()
  const canManage = roles.includes('Manager')
  const navigate = useNavigate()

  const [orders, setOrders] = useState<PurchaseOrderSummary[]>([])
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
    getPurchaseOrders()
      .then(setOrders)
      .catch(() => setError('Não foi possível carregar os pedidos de compra.'))
      .finally(() => setLoading(false))
  }, [])

  // O pedido novo abre direto no detalhe, onde o próximo passo (receber) acontece.
  function handleCreated(order: PurchaseOrderDetail) {
    navigate(`/app/purchases/${order.id}`, {
      state: { toast: `Pedido ${purchaseOrderCode(order.number)} criado com sucesso.` },
    })
  }

  const pending = orders.filter((order) => isPending(order.status))
  const unitsToReceive = pending.reduce((sum, order) => sum + order.orderedUnits - order.receivedUnits, 0)
  const countOf = (f: Filter) => (f === 'all' ? orders.length : orders.filter((o) => o.status === f).length)
  const visible = filter === 'all' ? orders : orders.filter((order) => order.status === filter)

  return (
    <div>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold tracking-tight text-ink">Pedidos de compra</h1>
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
          <PurchaseOrderForm onSaved={handleCreated} onCancel={() => setCreating(false)} />
        </div>
      )}

      <div className="mt-6 grid gap-3 sm:grid-cols-2 sm:gap-4">
        <div className="rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
          <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
            Pedidos pendentes
          </p>
          <p className="mt-2 text-xl font-bold tabular-nums text-ink sm:text-2xl">
            {loading ? '—' : pending.length}
          </p>
        </div>
        <div className="rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
          <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
            Unidades a receber
          </p>
          <p className="mt-2 text-xl font-bold tabular-nums text-ink sm:text-2xl">
            {loading ? '—' : unitsToReceive.toLocaleString('pt-BR')}
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
              {f === 'all' ? 'Todos' : PURCHASE_STATUS_LABEL[f]}
              {!loading && <span className="ml-1.5 tabular-nums opacity-70">{countOf(f)}</span>}
            </button>
          ))}
        </div>

        {loading ? (
          <p className="text-sm text-ink-muted">Carregando pedidos...</p>
        ) : (
          <PurchaseOrderTable orders={visible} />
        )}
      </div>
    </div>
  )
}

export default PurchaseOrdersPage
