import type { Product } from '../services/api'
import { StatusBadge } from './StatusBadge'

interface ProductTableProps {
  products: Product[]
  valueByProduct: Map<string, number>
  balanceByProduct: Map<string, number>
  onEdit?: (product: Product) => void
  onDelete?: (id: string) => void
}

export function ProductTable({
  products,
  valueByProduct,
  balanceByProduct,
  onEdit,
  onDelete,
}: ProductTableProps) {
  if (products.length === 0) {
    return <p className="text-sm text-ink-muted">Nenhum produto cadastrado.</p>
  }

  return (
    <>
      {/* Mobile: stacked cards, one per product, with full-size touch targets */}
      <div className="flex flex-col gap-3 sm:hidden">
        {products.map((product) => (
          <div
            key={product.id}
            className={`rounded-xl border border-line bg-cream p-4 ${
              product.isActive ? '' : 'opacity-50'
            }`}
          >
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="truncate font-medium text-ink">{product.name}</p>
                <p className="font-mono text-xs text-ink-muted">{product.sku}</p>
              </div>
              <StatusBadge isActive={product.isActive} />
            </div>

            <dl className="mt-3 grid grid-cols-2 gap-x-3 gap-y-2 text-sm">
              <div>
                <dt className="text-xs text-ink-muted">Preço</dt>
                <dd className="tabular-nums text-ink">
                  {product.price.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                </dd>
              </div>
              <div>
                <dt className="text-xs text-ink-muted">Saldo</dt>
                <dd className="tabular-nums text-ink">
                  {(balanceByProduct.get(product.id) ?? 0).toLocaleString('pt-BR')}
                </dd>
              </div>
              <div className="col-span-2">
                <dt className="text-xs text-ink-muted">Valor em estoque</dt>
                <dd className="tabular-nums font-medium text-ink">
                  {(valueByProduct.get(product.id) ?? 0).toLocaleString('pt-BR', {
                    style: 'currency',
                    currency: 'BRL',
                  })}
                </dd>
              </div>
            </dl>

            {(onEdit || onDelete) && (
              <div className="mt-3 flex gap-2 border-t border-line pt-3">
                {onEdit && (
                  <button
                    onClick={() => onEdit(product)}
                    className="flex h-11 flex-1 items-center justify-center rounded-lg text-sm font-medium text-ink-muted transition hover:bg-cream-soft hover:text-honey-dark"
                  >
                    Editar
                  </button>
                )}
                {onDelete && (
                  <button
                    onClick={() => onDelete(product.id)}
                    className="flex h-11 flex-1 items-center justify-center rounded-lg text-sm font-medium text-danger transition hover:bg-danger/10"
                  >
                    Remover
                  </button>
                )}
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Desktop/tablet: dense table */}
      <div className="hidden overflow-x-auto sm:block">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-line text-ink-muted">
              <th className="py-2 font-medium">Nome</th>
              <th className="py-2 font-medium">SKU</th>
              <th className="py-2 font-medium">Preço</th>
              <th className="py-2 text-right font-medium">Saldo</th>
              <th className="py-2 pr-6 text-right font-medium">Valor em estoque</th>
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
                <td className="py-2 text-right tabular-nums text-ink">
                  {(balanceByProduct.get(product.id) ?? 0).toLocaleString('pt-BR')}
                </td>
                <td className="py-2 pr-6 text-right tabular-nums text-ink">
                  {(valueByProduct.get(product.id) ?? 0).toLocaleString('pt-BR', {
                    style: 'currency',
                    currency: 'BRL',
                  })}
                </td>
                <td className="py-2">
                  <StatusBadge isActive={product.isActive} />
                </td>
                {(onEdit || onDelete) && (
                  <td className="py-2 text-right">
                    <div className="flex justify-end gap-2">
                      {onEdit && (
                        <button
                          onClick={() => onEdit(product)}
                          className="rounded-md px-2 py-1.5 text-ink-muted transition hover:bg-cream-soft hover:text-honey-dark"
                        >
                          Editar
                        </button>
                      )}
                      {onDelete && (
                        <button
                          onClick={() => onDelete(product.id)}
                          className="rounded-md px-2 py-1.5 text-danger transition hover:bg-danger/10"
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
      </div>
    </>
  )
}
