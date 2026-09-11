export function StatusBadge({ isActive }: { isActive: boolean }) {
  return (
    <span
      className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-medium ${
        isActive ? 'bg-honey/15 text-honey-text' : 'bg-line/60 text-ink-muted'
      }`}
    >
      {isActive ? 'Ativo' : 'Inativo'}
    </span>
  )
}
