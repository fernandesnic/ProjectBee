import type { Storage } from '../services/api'

interface StorageTableProps {
  storages: Storage[]
  onEdit?: (storage: Storage) => void
  onDelete?: (id: string) => void
}

export function StorageTable({ storages, onEdit, onDelete }: StorageTableProps) {
  if (storages.length === 0) {
    return <p className="text-sm text-ink-muted">Nenhum armazém cadastrado.</p>
  }

  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-line text-ink-muted">
          <th className="py-2 font-medium">Nome</th>
          <th className="py-2 font-medium">Código</th>
          <th className="py-2 font-medium">Endereço</th>
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
            <td className="py-2">
              <span
                className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                  storage.isActive
                    ? 'bg-honey/15 text-honey-dark'
                    : 'bg-line/60 text-ink-muted'
                }`}
              >
                {storage.isActive ? 'Ativo' : 'Inativo'}
              </span>
            </td>
            {(onEdit || onDelete) && (
              <td className="py-2 text-right">
                <div className="flex justify-end gap-4">
                  {onEdit && (
                    <button
                      onClick={() => onEdit(storage)}
                      className="text-ink-muted transition hover:text-honey-dark hover:underline"
                    >
                      Editar
                    </button>
                  )}
                  {onDelete && (
                    <button
                      onClick={() => onDelete(storage.id)}
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