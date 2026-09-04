import { useEffect, useState } from 'react'
import axios from 'axios'
import { useOutletContext } from 'react-router-dom'
import { deleteProduct, getProducts, getStock, type Product, type StockBalance } from '../services/api'
import { ProductTable } from '../components/ProductTable'
import { ProductForm } from '../components/ProductForm'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Toast } from '../components/Toast'
import type { AppLayoutContext } from '../components/AppLayout'

type FormMode = { type: 'closed' } | { type: 'create' } | { type: 'edit'; product: Product }

function ProductsPage() {
  const { roles } = useOutletContext<AppLayoutContext>()
  const canManage = roles.includes('Manager')

  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [formMode, setFormMode] = useState<FormMode>({ type: 'closed' })
  const [pendingDelete, setPendingDelete] = useState<Product | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [toast, setToast] = useState<string | null>(null)
  const [stock, setStock] = useState<StockBalance[]>([])

  useEffect(() => {
  Promise.all([getProducts(), getStock()])
    .then(([loadedProducts, loadedStock]) => {
      setProducts(loadedProducts)
      setStock(loadedStock)
    })
    .catch(() => setError('Não foi possível carregar os produtos.'))
    .finally(() => setLoading(false))
}, [])

  async function confirmDelete() {
    if (!pendingDelete) return
    setError(null)
    setDeleting(true)
    try {
      await deleteProduct(pendingDelete.id)
      setProducts((current) => current.filter((product) => product.id !== pendingDelete.id))
      setPendingDelete(null)
      setToast('Produto removido com sucesso.')
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 403) {
        setError('Seu perfil não tem permissão para excluir produtos.')
      } else {
        setError('Não foi possível excluir o produto.')
      }
    } finally {
      setDeleting(false)
    }
  }

    function handleSaved(saved: Product) {
    const isEditing = formMode.type === 'edit'
    setProducts((current) => {
      const exists = current.some((p) => p.id === saved.id)
      return exists
        ? current.map((p) => (p.id === saved.id ? saved : p))
        : [...current, saved]
    })
    setFormMode({ type: 'closed' })
    setToast(isEditing ? 'Produto atualizado com sucesso.' : 'Produto cadastrado com sucesso.')
  }

  const productIdsWithStock = new Set(stock.map((item) => item.productId))
  const withoutStockCount = products.filter(
    (product) => product.isActive && !productIdsWithStock.has(product.id),
  ).length
  const valueByProduct = new Map<string, number>()
  for (const item of stock) {
    const current = valueByProduct.get(item.productId) ?? 0
    valueByProduct.set(item.productId, current + item.balance * item.productPrice)
  }

  return (
    <div>
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight text-ink">Produtos</h1>
       {canManage && formMode.type === 'closed' && (
          <button
            onClick={() => setFormMode({ type: 'create' })}
            className="rounded-full bg-honey px-5 py-2 text-sm font-semibold text-hive transition hover:bg-honey-light"
          >
            Novo produto
          </button>
        )}
      </div>

      {canManage && formMode.type !== 'closed' && (
        <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-6">
          <ProductForm
            key={formMode.type === 'edit' ? formMode.product.id : 'create'}
            product={formMode.type === 'edit' ? formMode.product : undefined}
            onSaved={handleSaved}
            onCancel={() => setFormMode({ type: 'closed' })}
          />
        </div>
      )}

      <div className="mt-6 grid gap-4 sm:grid-cols-2">
        <div className="rounded-2xl border border-line bg-cream-soft p-6">
          <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
            Total de produtos
          </p>
          <p className="mt-2 text-2xl font-bold tabular-nums text-ink">
            {loading ? '—' : products.length}
          </p>
        </div>
        <div className="rounded-2xl border border-line bg-cream-soft p-6">
            <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
              Produtos sem saldo
            </p>
            <p className="mt-2 text-2xl font-bold tabular-nums text-ink">
              {loading ? '—' : withoutStockCount}
            </p>
          </div>
      </div>

      {error && <p className="mt-4 text-sm text-red-600">{error}</p>}

      <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-6">
        {loading ? (
          <p className="text-sm text-ink-muted">Carregando produtos...</p>
        ) : (
          <ProductTable
            products={products}
            valueByProduct={valueByProduct}
            onEdit={canManage ? (product) => setFormMode({ type: 'edit', product }) : undefined}
            onDelete={canManage ? (id) => setPendingDelete(products.find((p) => p.id === id) ?? null) : undefined}
          />
        )}
      </div>

      <ConfirmDialog
        open={pendingDelete !== null}
        title="Remover produto"
        description={
          pendingDelete
            ? `Tem certeza que deseja remover "${pendingDelete.name}"? Os saldos de estoque vinculados a ele também serão removidos.`
            : undefined
        }
        busy={deleting}
        onConfirm={confirmDelete}
        onCancel={() => setPendingDelete(null)}
      />

      <Toast message={toast} onDismiss={() => setToast(null)} />
    </div>
  )
}

export default ProductsPage
