import type { Product } from '../services/api'

interface ProductTableProps {
  products: Product[]
  onDelete: (id: string) => void
}

export function ProductTable({ products, onDelete }: ProductTableProps) {
  if (products.length === 0) {
    return <p className="text-sm text-ink-muted">Nenhum produto cadastrado.</p>
  }

  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-line text-ink-muted">
          <th className="py-2 font-medium">Nome</th>
          <th className="py-2 font-medium">SKU</th>
          <th className="py-2 font-medium">Preço</th>
          <th className="py-2" />
        </tr>
      </thead>
      <tbody className="divide-y divide-line">
        {products.map((product) => (
          <tr key={product.id}>
            <td className="py-2 text-ink">{product.name}</td>
            <td className="py-2 font-mono text-ink-muted">{product.sku}</td>
            <td className="py-2 tabular-nums text-ink">
              {product.price.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
            </td>
            <td className="py-2 text-right">
              <button
                onClick={() => onDelete(product.id)}
                className="text-red-600 transition hover:underline"
              >
                Remover
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
