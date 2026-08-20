import { useEffect, useState } from 'react'
import axios from 'axios'
import { useOutletContext } from 'react-router-dom'
import { deleteProduct, getProducts, type Product } from '../services/api'
import { ProductTable } from '../components/ProductTable'
import type { AppLayoutContext } from '../components/AppLayout'

function currency(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function ProductsPage() {
  const { roles } = useOutletContext<AppLayoutContext>()
  const canManage = roles.includes('Manager')

  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    getProducts()
      .then(setProducts)
      .catch(() => setError('Não foi possível carregar os produtos.'))
      .finally(() => setLoading(false))
  }, [])

  async function handleDelete(id: string) {
    setError(null)
    try {
      await deleteProduct(id)
      setProducts((current) => current.filter((product) => product.id !== id))
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 403) {
        setError('Seu perfil não tem permissão para excluir produtos.')
      } else {
        setError('Não foi possível excluir o produto.')
      }
    }
  }

  const averagePrice =
    products.length > 0
      ? products.reduce((sum, product) => sum + product.price, 0) / products.length
      : 0

  return (
    <div>
      <h1 className="text-2xl font-bold tracking-tight text-ink">Produtos</h1>

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
            Preço médio
          </p>
          <p className="mt-2 text-2xl font-bold tabular-nums text-ink">
            {loading ? '—' : currency(averagePrice)}
          </p>
        </div>
      </div>

      {error && <p className="mt-4 text-sm text-red-600">{error}</p>}

      <div className="mt-6 rounded-2xl border border-line bg-cream-soft p-6">
        {loading ? (
          <p className="text-sm text-ink-muted">Carregando produtos...</p>
        ) : (
          <ProductTable products={products} onDelete={canManage ? handleDelete : undefined} />
        )}
      </div>
    </div>
  )
}

export default ProductsPage
