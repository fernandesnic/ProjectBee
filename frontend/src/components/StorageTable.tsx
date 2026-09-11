import type { Storage } from '../services/api'
import { StatusBadge } from './StatusBadge'

interface StorageTableProps {
  storages: Storage[]
  valueByStorage: Map<string, number>
  onEdit?: (storage: Storage) => void
  onDelete?: (id: string) => void
}

export function StorageTable({ storages, valueByStorage, onEdit, onDelete }: StorageTableProps) {
  if (storages.length === 0) {
    return <p className="text-sm text-ink-muted">Nenhum armazém cadastrado.</p>
  }

  return (
    <>
      {/* Mobile: stacked cards, one per warehouse, with full-size touch targets */}
      <div className="flex flex-col gap-3 sm:hidden">
        {storages.map((storage) => (
          <div
            key={storage.id}
            className={`rounded-xl border border-line bg-cream p-4 ${
              storage.isActive ? '' : 'opacity-50'
            }`}
          >
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="truncate font-medium text-ink">{storage.name}</p>
                <p className="font-mono text-xs text-ink-muted">{storage.idNumber}</p>
              </div>
              <StatusBadge isActive={storage.isActive} />
            </div>

            <p className="mt-2 text-sm text-ink-muted">
              {`${storage.addressStreet}, ${storage.addressNumber} — ${storage.addressCity}`}
            </p>

            <dl className="mt-3">
              <dt className="text-xs text-ink-muted">Valor em estoque</dt>
              <dd className="tabular-nums font-medium text-ink">
                {(valueByStorage.get(storage.id) ?? 0).toLocaleString('pt-BR', {
                  style: 'currency',
                  currency: 'BRL',
                })}
              </dd>
            </dl>

            {(onEdit || onDelete) && (
              <div className="mt-3 flex gap-2 border-t border-line pt-3">
                {onEdit && (
                  <button
                    onClick={() => onEdit(storage)}
                    className="flex h-11 flex-1 items-center justify-center rounded-lg text-sm font-medium text-ink-muted transition hover:bg-cream-soft hover:text-honey-dark"
                  >
                    Editar
                  </button>
                )}
                {onDelete && (
                  <button
                    onClick={() => onDelete(storage.id)}
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
              <th className="py-2 font-medium">Código</th>
              <th className="py-2 font-medium">Endereço</th>
              <th className="py-2 pr-6 text-right font-medium">Valor em estoque</th>
              <th className="py-2 font-medium">Status</th>
              {(onEdit || onDelete) && <th className="py-2" />}
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {storages.map((storage) => (
              <tr key={storage.id} className={storage.isActive ? '' : 'opacity-50'}>
                <td className="py-2 text-ink">{storage.name}</td>
                <td className="py-2 font-mono text-ink-muted">{storage.idNumber}</td>
                <td className="py-2 text-ink-muted">
                  {`${storage.addressStreet}, ${storage.addressNumber} — ${storage.addressCity}`}
                </td>
                <td className="py-2 pr-6 text-right tabular-nums text-ink">
                  {(valueByStorage.get(storage.id) ?? 0).toLocaleString('pt-BR', {
                    style: 'currency',
                    currency: 'BRL',
                  })}
                </td>
                <td className="py-2">
                  <StatusBadge isActive={storage.isActive} />
                </td>
                {(onEdit || onDelete) && (
                  <td className="py-2 text-right">
                    <div className="flex justify-end gap-2">
                      {onEdit && (
                        <button
                          onClick={() => onEdit(storage)}
                          className="rounded-md px-2 py-1.5 text-ink-muted transition hover:bg-cream-soft hover:text-honey-dark"
                        >
                          Editar
                        </button>
                      )}
                      {onDelete && (
                        <button
                          onClick={() => onDelete(storage.id)}
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
