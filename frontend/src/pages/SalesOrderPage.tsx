import { useCallback, useEffect, useState } from 'react'
import axios from 'axios'
import { Link, useLocation, useNavigate, useOutletContext, useParams } from 'react-router-dom'
import {
  formatDocument,
  getSalesOrder,
  invoiceCode,
  PAYMENT_METHOD_LABEL,
  payBoleto,
  salesOrderCode,
  type SalesOrderDetail,
} from '../services/api'
import { formatDateTime } from '../services/date'
import { SalesOrderItems } from '../components/SalesOrderItems'
import { SalesStatusBadge } from '../components/StatusBadge'
import { BoletoCard } from '../components/BoletoCard'
import type { AppLayoutContext } from '../components/AppLayout'
import { Toast } from '../components/Toast'

function SalesOrderPage() {
  const { id = '' } = useParams()
  const { roles } = useOutletContext<AppLayoutContext>()
  const canManage = roles.includes('Manager')
  const location = useLocation()
  const navigate = useNavigate()

  const [order, setOrder] = useState<SalesOrderDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [paying, setPaying] = useState(false)
  // Quem acabou de criar chega aqui pela lista: o aviso de sucesso aparece no destino.
  const [toast, setToast] = useState<string | null>(
    () => (location.state as { toast?: string } | null)?.toast ?? null,
  )

  // Sem isso, o aviso voltaria a cada F5: o state fica guardado no histórico do navegador.
  useEffect(() => {
    if (location.state) navigate(location.pathname, { replace: true, state: null })
  }, [location.state, location.pathname, navigate])

  const loadOrder = useCallback(
    () =>
      getSalesOrder(id)
        .then(setOrder)
        .catch((err) =>
          setError(
            axios.isAxiosError(err) && err.response?.status === 404
              ? 'Pedido de venda não encontrado.'
              : 'Não foi possível carregar o pedido.',
          ),
        )
        .finally(() => setLoading(false)),
    [id],
  )

  useEffect(() => {
    loadOrder()
  }, [loadOrder])

  const dismissToast = useCallback(() => setToast(null), [])

  async function handlePay() {
    if (!order) return
    setError(null)
    setPaying(true)
    try {
      const updated = await payBoleto(order.id, order.rowVersion)
      setOrder(updated)
      setToast('Pagamento registrado. O pedido já pode ser faturado.')
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 409) {
        // Outra pessoa pagou no meio: a tela velha é descartada e o pedido é relido.
        setError(err.response.data?.detail ?? 'O pedido foi alterado por outra operação.')
        loadOrder()
      } else if (axios.isAxiosError(err) && typeof err.response?.data?.detail === 'string') {
        setError(err.response.data.detail)
      } else {
        setError('Não foi possível registrar o pagamento.')
      }
    } finally {
      setPaying(false)
    }
  }

  if (loading) return <p className="text-sm text-ink-muted">Carregando pedido...</p>

  if (!order)
    return (
      <div>
        <p className="text-sm text-danger" role="alert">
          {error}
        </p>
        <Link to="/app/sales" className="mt-4 inline-block text-sm text-ink-muted hover:text-ink">
          ← Voltar para os pedidos
        </Link>
      </div>
    )

  return (
    <div>
      <Link
        to="/app/sales"
        className="-ml-1 inline-flex h-11 items-center px-1 text-sm text-ink-muted transition hover:text-ink sm:h-auto"
      >
        ← Pedidos de venda
      </Link>

      <div className="mt-1 flex flex-wrap items-center gap-3 sm:mt-3">
        <h1 className="font-mono text-2xl font-bold tracking-tight text-ink">
          {salesOrderCode(order.number)}
        </h1>
        <SalesStatusBadge status={order.status} />
        {order.status === 'Paid' && (
          <Link
            to="/app/invoices"
            className="flex h-11 w-full items-center justify-center rounded-full bg-honey px-5 text-sm font-semibold text-hive transition hover:bg-honey-light sm:ml-auto sm:h-auto sm:w-auto sm:py-2"
          >
            Emitir nota fiscal
          </Link>
        )}
      </div>

      <dl className="mt-6 grid gap-4 rounded-2xl border border-line bg-cream-soft p-4 text-sm sm:grid-cols-2 sm:p-6 lg:grid-cols-4">
        <div>
          <dt className="text-xs text-ink-muted">Cliente</dt>
          <dd className="font-medium text-ink">{order.customerName}</dd>
          <dd className="text-xs tabular-nums text-ink-muted">{formatDocument(order.customerDocument)}</dd>
        </div>
        <div>
          <dt className="text-xs text-ink-muted">Armazém de expedição</dt>
          <dd className="font-medium text-ink">{order.storageName}</dd>
        </div>
        <div>
          <dt className="text-xs text-ink-muted">Pagamento</dt>
          <dd className="font-medium text-ink">{PAYMENT_METHOD_LABEL[order.paymentMethod]}</dd>
        </div>
        <div>
          <dt className="text-xs text-ink-muted">Criado</dt>
          <dd className="text-ink">{formatDateTime(order.createdAt)}</dd>
          <dd className="break-all text-xs text-ink-muted">{order.createdBy}</dd>
        </div>
        {order.notes && (
          <div className="sm:col-span-2 lg:col-span-4">
            <dt className="text-xs text-ink-muted">Observação</dt>
            <dd className="whitespace-pre-line text-ink">{order.notes}</dd>
          </div>
        )}
      </dl>

      {order.invoice && (
        <div className="mt-4 rounded-2xl border border-line bg-cream-soft p-4 text-sm sm:p-6">
          <p className="font-medium text-ink">
            Faturado na nota <span className="font-mono">{invoiceCode(order.invoice.number)}</span>
            <span className="font-normal text-ink-muted"> em {formatDateTime(order.invoice.createdAt)}</span>
          </p>
          <p className="mt-1 text-ink-muted">A mercadoria já saiu do estoque de {order.storageName}.</p>
        </div>
      )}

      {error && (
        <p className="mt-4 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger" role="alert">
          {error}
        </p>
      )}

      {order.boleto && (
        <section className="mt-6 rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
          <h2 className="text-sm font-semibold text-ink">Boleto</h2>
          <div className="mt-3">
            <BoletoCard boleto={order.boleto} onPay={canManage ? handlePay : undefined} paying={paying} />
          </div>
        </section>
      )}

      <section className="mt-6 rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
        <h2 className="text-sm font-semibold text-ink">Itens</h2>
        <div className="mt-3">
          <SalesOrderItems items={order.items} />
        </div>
      </section>

      <Toast message={toast} onDismiss={dismissToast} />
    </div>
  )
}

export default SalesOrderPage
