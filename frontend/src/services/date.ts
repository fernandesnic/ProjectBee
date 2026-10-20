const dateTimeFormat = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })

// O banco grava em UTC, mas a data chega sem fuso; sem o "Z" o navegador leria como hora local.
export function parseUtc(value: string) {
  return new Date(/Z|[+-]\d{2}:\d{2}$/.test(value) ? value : `${value}Z`)
}

export function formatDateTime(value: string) {
  return dateTimeFormat.format(parseUtc(value))
}
