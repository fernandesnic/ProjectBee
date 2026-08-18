import type { Product } from '../services/api'

interface ProductTableProps {
  products: Product[]
  onDelete: (id: string) => void
}

export function ProductTable({ products, onDelete }: ProductTableProps) {
  if (products.length === 0) {
    return <p className="text-sm text-gray-500">Nenhum produto cadastrado.</p>
  }

  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-gray-200 text-gray-500">
          <th className="py-2">Nome</th>
          <th className="py-2">SKU</th>
          <th className="py-2">Preço</th>
          <th className="py-2" />
        </tr>
      </thead>
      <tbody>
        {products.map((product) => (
          <tr key={product.id} className="border-b border-gray-100">
            <td className="py-2">{product.name}</td>
            <td className="py-2">{product.sku}</td>
            <td className="py-2">
              {product.price.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
            </td>
            <td className="py-2 text-right">
              <button
                onClick={() => onDelete(product.id)}
                className="text-red-600 hover:underline"
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
