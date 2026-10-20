import { useCallback, useEffect, useState } from 'react'
import axios from 'axios'
import { Link, useLocation, useNavigate, useOutletContext, useParams } from 'react-router-dom'
import {
  formatDocument,
  getPurchaseOrder,
  isPending,
  purchaseOrderCode,
  type PurchaseOrderDetail,
} from '../services/api'
import { formatDateTime } from '../services/date'
import { PurchaseOrderItems } from '../components/PurchaseOrderItems'
import { PurchaseStatusBadge } from '../components/StatusBadge'
import { PurchaseReceiptList } from '../components/PurchaseReceiptList'
import { ReceiptForm } from '../components/ReceiptForm'
import { CancelPurchaseForm } from '../components/CancelPurchaseForm'
import { Modal } from '../components/Modal'
import type { AppLayoutContext } from '../components/AppLayout'
import { Toast } from '../components/Toast'

function PurchaseOrderPage() {
  const { id = '' } = useParams()
  const { roles } = useOutletContext<AppLayoutContext>()
  const canManage = roles.includes('Manager')
  const location = useLocation()
  const navigate = useNavigate()

  const [order, setOrder] = useState<PurchaseOrderDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
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
      getPurchaseOrder(id)
        .then(setOrder)
        .catch((err) =>
          setError(
            axios.isAxiosError(err) && err.response?.status === 404
              ? 'Pedido de compra não encontrado.'
              : 'Não foi possível carregar o pedido.',
          ),
        )
        .finally(() => setLoading(false)),
    [id],
  )

  useEffect(() => {
    loadOrder()
  }, [loadOrder])

  // Recebimento e cancelamento abrem no Modal, como os movimentos de estoque: painel inferior
  // no celular, e o foco volta sozinho para o botão que abriu.
  const [receiving, setReceiving] = useState(false)
  const [receiveBusy, setReceiveBusy] = useState(false)
  const closeReceiving = useCallback(() => setReceiving(false), [])
  const [cancelling, setCancelling] = useState(false)
  const [cancelBusy, setCancelBusy] = useState(false)
  const closeCancel = useCallback(() => setCancelling(false), [])

  const dismissToast = useCallback(() => setToast(null), [])

  function handleReceived(updated: PurchaseOrderDetail) {
    const invoice = updated.receipts[updated.receipts.length - 1]?.invoiceNumber
    setOrder(updated)
    setReceiving(false)
    setError(null)
    setToast(
      updated.status === 'Received'
        ? `Nota ${invoice} registrada. Pedido recebido por completo.`
        : `Nota ${invoice} registrada. O restante continua pendente.`,
    )
  }

  function handleCancelled(updated: PurchaseOrderDetail) {
    setOrder(updated)
    setCancelling(false)
    setError(null)
    setToast(`Pedido ${purchaseOrderCode(updated.number)} cancelado.`)
  }

  // Outra pessoa recebeu ou cancelou no meio: a tela velha é descartada e o pedido é relido.
  function handleConflict(message: string) {
    setReceiving(false)
    setCancelling(false)
    setError(message)
    loadOrder()
  }

  if (loading) return <p className="text-sm text-ink-muted">Carregando pedido...</p>

  if (!order)
    return (
      <div>
        <p className="text-sm text-danger" role="alert">
          {error}
        </p>
        <Link to="/app/purchases" className="mt-4 inline-block text-sm text-ink-muted hover:text-ink">
          ← Voltar para os pedidos
        </Link>
      </div>
    )

  return (
    <div>
      <Link
        to="/app/purchases"
        className="-ml-1 inline-flex h-11 items-center px-1 text-sm text-ink-muted transition hover:text-ink sm:h-auto"
      >
        ← Pedidos de compra
      </Link>

      <div className="mt-1 flex flex-wrap items-center gap-3 sm:mt-3">
        <h1 className="font-mono text-2xl font-bold tracking-tight text-ink">
          {purchaseOrderCode(order.number)}
        </h1>
        <PurchaseStatusBadge status={order.status} />
        {isPending(order.status) && (
          <div className="grid w-full gap-2 min-[400px]:grid-cols-2 sm:ml-auto sm:flex sm:w-auto">
            <button
              onClick={() => setReceiving(true)}
              className="h-11 rounded-full bg-honey px-5 text-sm font-semibold text-hive transition hover:bg-honey-light sm:h-auto sm:py-2"
            >
              Registrar recebimento
            </button>
            {canManage && (
              <button
                onClick={() => setCancelling(true)}
                className="h-11 rounded-full border border-line px-5 text-sm font-medium text-danger transition hover:border-danger hover:bg-danger/10 sm:h-auto sm:py-2"
              >
                {order.status === 'Partial' ? 'Cancelar pendente' : 'Cancelar pedido'}
              </button>
            )}
          </div>
        )}
      </div>

      <dl className="mt-6 grid gap-4 rounded-2xl border border-line bg-cream-soft p-4 text-sm sm:grid-cols-3 sm:p-6">
        <div>
          <dt className="text-xs text-ink-muted">Fornecedor</dt>
          <dd className="font-medium text-ink">{order.supplierName}</dd>
          <dd className="text-xs tabular-nums text-ink-muted">{formatDocument(order.supplierDocument)}</dd>
        </div>
        <div>
          <dt className="text-xs text-ink-muted">Armazém de recebimento</dt>
          <dd className="font-medium text-ink">{order.storageName}</dd>
        </div>
        <div>
          <dt className="text-xs text-ink-muted">Criado</dt>
          <dd className="text-ink">{formatDateTime(order.createdAt)}</dd>
          <dd className="break-all text-xs text-ink-muted">{order.createdBy}</dd>
        </div>
        {order.notes && (
          <div className="sm:col-span-3">
            <dt className="text-xs text-ink-muted">Observação</dt>
            <dd className="whitespace-pre-line text-ink">{order.notes}</dd>
          </div>
        )}
      </dl>

      {order.status === 'Cancelled' && (
        <div className="mt-4 rounded-2xl border border-line bg-cream-soft p-4 text-sm sm:p-6">
          <p className="font-medium text-ink">
            Pendente cancelado em {order.cancelledAt ? formatDateTime(order.cancelledAt) : '—'}
            {order.cancelledBy && <span className="font-normal text-ink-muted"> por {order.cancelledBy}</span>}
          </p>
          {order.cancelReason && <p className="mt-1 italic text-ink-muted">“{order.cancelReason}”</p>}
          {order.receipts.length > 0 && (
            <p className="mt-2 text-ink-muted">O que já tinha sido recebido continua no estoque.</p>
          )}
        </div>
      )}

      {error && (
        <p className="mt-4 rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger" role="alert">
          {error}
        </p>
      )}

      <section className="mt-6 rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
        <h2 className="text-sm font-semibold text-ink">Itens</h2>
        <div className="mt-3">
          <PurchaseOrderItems items={order.items} cancelled={order.status === 'Cancelled'} />
        </div>
      </section>

      <section className="mt-6 rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
        <h2 className="text-sm font-semibold text-ink">Recebimentos</h2>
        <div className="mt-3">
          <PurchaseReceiptList receipts={order.receipts} />
        </div>
      </section>

      <Modal
        open={receiving}
        title="Recebimento de nota"
        subtitle={
          <>
            <span className="font-mono font-medium text-ink">{purchaseOrderCode(order.number)}</span> ·{' '}
            {order.supplierName}
            <span className="mt-0.5 block text-xs">Entrada em {order.storageName}</span>
          </>
        }
        busy={receiveBusy}
        onClose={closeReceiving}
      >
        {receiving && (
          <ReceiptForm
            order={order}
            onDone={handleReceived}
            onCancel={closeReceiving}
            onConflict={handleConflict}
            onBusyChange={setReceiveBusy}
          />
        )}
      </Modal>

      <Modal
        open={cancelling}
        title={order.status === 'Partial' ? 'Cancelar pendente' : 'Cancelar pedido'}
        subtitle={
          <>
            <span className="font-mono font-medium text-ink">{purchaseOrderCode(order.number)}</span> ·{' '}
            {order.supplierName}
          </>
        }
        busy={cancelBusy}
        onClose={closeCancel}
      >
        {cancelling && (
          <CancelPurchaseForm
            order={order}
            onDone={handleCancelled}
            onCancel={closeCancel}
            onConflict={handleConflict}
            onBusyChange={setCancelBusy}
          />
        )}
      </Modal>

      <Toast message={toast} onDismiss={dismissToast} />
    </div>
  )
}

export default PurchaseOrderPage
