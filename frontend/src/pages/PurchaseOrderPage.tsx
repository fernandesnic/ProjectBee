import { useCallback, useEffect, useRef, useState } from 'react'
import axios from 'axios'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import {
  formatDocument,
  getPurchaseOrder,
  isPending,
  purchaseOrderCode,
  type PurchaseOrderDetail,
} from '../services/api'
import { formatDateTime } from '../services/date'
import { formatCurrency } from '../services/price'
import { PurchaseStatusBadge } from '../components/StatusBadge'
import { PurchaseReceiptList } from '../components/PurchaseReceiptList'
import { ReceiptForm } from '../components/ReceiptForm'
import { Toast } from '../components/Toast'

function PurchaseOrderPage() {
  const { id = '' } = useParams()
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

  const [receiving, setReceiving] = useState(false)
  const receiveButtonRef = useRef<HTMLButtonElement>(null)
  const receivingWasOpen = useRef(false)

  // O botão some enquanto o formulário está aberto; ao fechar, o foco volta para ele.
  useEffect(() => {
    if (!receiving && receivingWasOpen.current) receiveButtonRef.current?.focus()
    receivingWasOpen.current = receiving
  }, [receiving])

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

  // Outra pessoa recebeu ou cancelou no meio: a tela velha é descartada e o pedido é relido.
  function handleConflict(message: string) {
    setReceiving(false)
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

  const total = order.items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0)

  return (
    <div>
      <Link to="/app/purchases" className="text-sm text-ink-muted transition hover:text-ink">
        ← Pedidos de compra
      </Link>

      <div className="mt-3 flex flex-wrap items-center gap-3">
        <h1 className="font-mono text-2xl font-bold tracking-tight text-ink">
          {purchaseOrderCode(order.number)}
        </h1>
        <PurchaseStatusBadge status={order.status} />
        {isPending(order.status) && !receiving && (
          <div className="flex w-full gap-2 sm:ml-auto sm:w-auto">
            <button
              ref={receiveButtonRef}
              onClick={() => setReceiving(true)}
              className="h-11 flex-1 rounded-full bg-honey px-5 text-sm font-semibold text-hive transition hover:bg-honey-light sm:h-auto sm:flex-none sm:py-2"
            >
              Registrar recebimento
            </button>
          </div>
        )}
      </div>

      {receiving && (
        <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
          <h2 className="mb-4 text-sm font-semibold text-ink">Recebimento de nota</h2>
          <ReceiptForm
            order={order}
            onDone={handleReceived}
            onCancel={() => setReceiving(false)}
            onConflict={handleConflict}
          />
        </div>
      )}

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
        <div className="mt-3 overflow-x-auto">
          <table className="w-full min-w-[36rem] text-left text-sm">
            <thead>
              <tr className="border-b border-line text-ink-muted">
                <th className="py-2 font-medium">Produto</th>
                <th className="py-2 pr-4 text-right font-medium">Pedido</th>
                <th className="py-2 pr-4 text-right font-medium">Recebido</th>
                <th className="py-2 pr-4 text-right font-medium">
                  {order.status === 'Cancelled' ? 'Cancelado' : 'Pendente'}
                </th>
                <th className="py-2 pr-4 text-right font-medium">Preço</th>
                <th className="py-2 text-right font-medium">Total</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-line">
              {order.items.map((item) => (
                <tr key={item.id}>
                  <td className="py-2">
                    <p className="text-ink">{item.productName}</p>
                    <p className="font-mono text-xs text-ink-muted">{item.productSku}</p>
                  </td>
                  <td className="py-2 pr-4 text-right tabular-nums text-ink">
                    {item.quantity.toLocaleString('pt-BR')}
                  </td>
                  <td className="py-2 pr-4 text-right tabular-nums text-ink">
                    {item.receivedQuantity.toLocaleString('pt-BR')}
                  </td>
                  <td className="py-2 pr-4 text-right tabular-nums text-ink-muted">
                    {(item.quantity - item.receivedQuantity).toLocaleString('pt-BR')}
                  </td>
                  <td className="py-2 pr-4 text-right tabular-nums text-ink">{formatCurrency(item.unitPrice)}</td>
                  <td className="py-2 text-right tabular-nums text-ink">
                    {formatCurrency(item.quantity * item.unitPrice)}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t border-line">
                <td colSpan={5} className="pt-3 text-right text-ink-muted">
                  Total do pedido
                </td>
                <td className="pt-3 text-right font-semibold tabular-nums text-ink">{formatCurrency(total)}</td>
              </tr>
            </tfoot>
          </table>
        </div>
      </section>

      <section className="mt-6 rounded-2xl border border-line bg-cream-soft p-4 sm:p-6">
        <h2 className="text-sm font-semibold text-ink">Recebimentos</h2>
        <div className="mt-3">
          <PurchaseReceiptList receipts={order.receipts} />
        </div>
      </section>

      <Toast message={toast} onDismiss={dismissToast} />
    </div>
  )
}

export default PurchaseOrderPage
