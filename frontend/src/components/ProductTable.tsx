import type { Product } from '../services/api'

interface ProductTableProps {
  products: Product[]
  onEdit?: (product: Product) => void
  onDelete?: (id: string) => void
}

export function ProductTable({ products, onEdit, onDelete }: ProductTableProps) {
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
          <th className="py-2 font-medium">Status</th>
          {(onEdit || onDelete) && <th className="py-2" />}
        </tr>
      </thead>
      <tbody className="divide-y divide-line">
        {products.map((product) => (
          <tr key={product.id} className={product.isActive ? '' : 'opacity-50'}>
            <td className="py-2 text-ink">{product.name}</td>
            <td className="py-2 font-mono text-ink-muted">{product.sku}</td>
            <td className="py-2 tabular-nums text-ink">
              {product.price.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
            </td>
            <td className="py-2">
              <span
                className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                  product.isActive
                    ? 'bg-honey/15 text-honey-dark'
                    : 'bg-line/60 text-ink-muted'
                }`}
              >
                {product.isActive ? 'Ativo' : 'Inativo'}
              </span>
            </td>
            {(onEdit || onDelete) && (
              <td className="py-2 text-right">
                <div className="flex justify-end gap-4">
                  {onEdit && (
                    <button
                      onClick={() => onEdit(product)}
                      className="text-ink-muted transition hover:text-honey-dark hover:underline"
                    >
                      Editar
                    </button>
                  )}
                  {onDelete && (
                    <button
                      onClick={() => onDelete(product.id)}
                      className="text-red-600 transition hover:underline"
                    >
                      Remover
                    </button>
                  )}
                </div>
              </td>
            )}
          </tr>
        ))}
      </tbody>
    </table>
  )
}
