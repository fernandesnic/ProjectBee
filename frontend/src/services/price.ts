const WITH_THOUSANDS = /^\d{1,3}(\.\d{3})+(,\d{1,2})?$/ // 1.234 ou 1.234,56
const PLAIN = /^\d+([.,]\d{1,2})?$/ // 1234, 12,50 ou 12.50

// Aceita "12,50", "12.50", "1234,56" e "1.234,56". Fora isso, devolve null.
export function parsePrice(text: string): number | null {
  const trimmed = text.trim()
  if (WITH_THOUSANDS.test(trimmed)) {
    return Number(trimmed.replace(/\./g, '').replace(',', '.'))
  }
  if (PLAIN.test(trimmed)) {
    return Number(trimmed.replace(',', '.'))
  }
  return null
}

export function formatPriceInput(price: number) {
  return price.toFixed(2).replace('.', ',')
}

export function formatCurrency(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}
