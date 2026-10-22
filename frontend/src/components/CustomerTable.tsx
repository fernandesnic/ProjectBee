import { formatDocument, type Customer } from '../services/api'
import { StatusBadge } from './StatusBadge'

interface CustomerTableProps {
  customers: Customer[]
  onEdit?: (customer: Customer) => void
  onDelete?: (id: string) => void
}

function contactOf(customer: Customer) {
  return [customer.email, customer.phone].filter(Boolean).join(' · ') || '—'
}

export function CustomerTable({ customers, onEdit, onDelete }: CustomerTableProps) {
  if (customers.length === 0) {
    return <p className="text-sm text-ink-muted">Nenhum cliente cadastrado.</p>
  }

  return (
    <>
      {/* Mobile: stacked cards, one per customer, with full-size touch targets */}
      <div className="flex flex-col gap-3 sm:hidden">
        {customers.map((customer) => (
          <div
            key={customer.id}
            className={`rounded-xl border border-line bg-cream p-4 ${
              customer.isActive ? '' : 'opacity-50'
            }`}
          >
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="truncate font-medium text-ink">{customer.name}</p>
                <p className="text-xs tabular-nums text-ink-muted">{formatDocument(customer.document)}</p>
              </div>
              <StatusBadge isActive={customer.isActive} />
            </div>

            <p className="mt-2 break-all text-sm text-ink-muted">{contactOf(customer)}</p>

            {(onEdit || onDelete) && (
              <div className="mt-3 flex gap-2 border-t border-line pt-3">
                {onEdit && (
                  <button
                    onClick={() => onEdit(customer)}
                    className="flex h-11 flex-1 items-center justify-center rounded-lg text-sm font-medium text-ink-muted transition hover:bg-cream-soft hover:text-honey-dark"
                  >
                    Editar
                  </button>
                )}
                {onDelete && (
                  <button
                    onClick={() => onDelete(customer.id)}
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
              <th className="py-2 font-medium">CPF / CNPJ</th>
              <th className="py-2 font-medium">Contato</th>
              <th className="py-2 font-medium">Status</th>
              {(onEdit || onDelete) && <th className="py-2" />}
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {customers.map((customer) => (
              <tr key={customer.id} className={customer.isActive ? '' : 'opacity-50'}>
                <td className="py-2 text-ink">{customer.name}</td>
                <td className="py-2 tabular-nums text-ink-muted">{formatDocument(customer.document)}</td>
                <td className="py-2 text-ink-muted">{contactOf(customer)}</td>
                <td className="py-2">
                  <StatusBadge isActive={customer.isActive} />
                </td>
                {(onEdit || onDelete) && (
                  <td className="py-2 text-right">
                    <div className="flex justify-end gap-2">
                      {onEdit && (
                        <button
                          onClick={() => onEdit(customer)}
                          className="rounded-md px-2 py-1.5 text-ink-muted transition hover:bg-cream-soft hover:text-honey-dark"
                        >
                          Editar
                        </button>
                      )}
                      {onDelete && (
                        <button
                          onClick={() => onDelete(customer.id)}
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
