import { useEffect, useRef, useState } from 'react'
import {
  Bar,
  BarChart,
  CartesianGrid,
  LabelList,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { getProducts, getStock, type Product, type StockBalance } from '../services/api'

const COLORS = {
  ink: '#1c1712',
  inkMuted: '#6e6455',
  line: '#e6dcc2',
  cream: '#fbf7ec',
  creamSoft: '#f2ead4',
  accent: '#a8690a',
}

// Paleta categórica validada (8 tons, ordem fixa) — usada quando a cor precisa
// identificar produtos distintos numa mesma barra empilhada.
const CATEGORICAL = [
  '#2a78d6',
  '#eb6834',
  '#1baf7a',
  '#eda100',
  '#e87ba4',
  '#008300',
  '#4a3aa7',
  '#e34948',
]
const OUTROS_COLOR = '#9a9488'
const MAX_PRODUCT_SLOTS = CATEGORICAL.length

const KPIS = [
  { label: 'Faturamento (mês)', value: 48250.9 },
  { label: 'A receber', value: 12430.0 },
  { label: 'A pagar', value: 8190.5 },
  { label: 'Lucro', value: 15320.4 },
]

const REVENUE_TREND = [
  { mes: 'Mar', valor: 32100 },
  { mes: 'Abr', valor: 35800 },
  { mes: 'Mai', valor: 29900 },
  { mes: 'Jun', valor: 41200 },
  { mes: 'Jul', valor: 44700 },
  { mes: 'Ago', valor: 48250 },
]

function currency(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function compactCurrency(value: number) {
  if (Math.abs(value) >= 1000) {
    return `R$ ${(value / 1000).toLocaleString('pt-BR', { maximumFractionDigits: 1 })}k`
  }
  return currency(value)
}

interface DashboardCategory {
  key: 'geral' | 'produtos' | 'faturamento' | 'financeiro'
  label: string
  planned?: boolean
}

const CATEGORIES: DashboardCategory[] = [
  { key: 'geral', label: 'Visão geral' },
  { key: 'produtos', label: 'Produtos' },
  { key: 'faturamento', label: 'Faturamento', planned: true },
  { key: 'financeiro', label: 'Financeiro', planned: true },
]

interface CategoryMenuProps {
  active: DashboardCategory
  onSelect: (key: DashboardCategory['key']) => void
}

function CategoryMenu({ active, onSelect }: CategoryMenuProps) {
  const [open, setOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  return (
    <div ref={menuRef} className="relative inline-block">
      <button
        type="button"
        onClick={() => setOpen((current) => !current)}
        className="flex items-center gap-2 text-2xl font-bold tracking-tight text-ink transition hover:text-honey-dark"
      >
        {active.label}
        <span
          className={`text-base text-ink-muted transition-transform ${open ? 'rotate-180' : ''}`}
          aria-hidden="true"
        >
          ▾
        </span>
      </button>

      {open && (
        <div className="absolute left-0 z-10 mt-2 w-56 overflow-hidden rounded-xl border border-line bg-cream-soft py-1 shadow-lg">
          {CATEGORIES.map((category) =>
            category.planned ? (
              <span
                key={category.key}
                className="flex items-center justify-between px-4 py-2 text-sm text-ink-muted/50"
              >
                {category.label}
                <span className="rounded-full bg-line/60 px-2 py-0.5 text-[10px] font-medium">
                  planejado
                </span>
              </span>
            ) : (
              <button
                key={category.key}
                type="button"
                onClick={() => {
                  onSelect(category.key)
                  setOpen(false)
                }}
                className={`flex w-full items-center px-4 py-2 text-left text-sm transition ${
                  category.key === active.key
                    ? 'bg-honey/15 font-medium text-honey-dark'
                    : 'text-ink hover:bg-cream'
                }`}
              >
                {category.label}
              </button>
            ),
          )}
        </div>
      )}
    </div>
  )
}

interface ProductChartPoint {
  productId: string
  name: string
  balance: number
  value: number
}

function DashboardPage() {
  const [categoryKey, setCategoryKey] = useState<DashboardCategory['key']>('geral')
  const activeCategory = CATEGORIES.find((category) => category.key === categoryKey) ?? CATEGORIES[0]

  const [products, setProducts] = useState<Product[]>([])
  const [stock, setStock] = useState<StockBalance[]>([])
  const [loadingProducts, setLoadingProducts] = useState(true)
  const [productsError, setProductsError] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([getProducts(), getStock()])
      .then(([loadedProducts, loadedStock]) => {
        setProducts(loadedProducts)
        setStock(loadedStock)
      })
      .catch(() => setProductsError('Não foi possível carregar os dados de produtos.'))
      .finally(() => setLoadingProducts(false))
  }, [])

  const productById = new Map(products.map((product) => [product.id, product]))

  const valueByProduct = new Map<string, number>()
  const balanceByProduct = new Map<string, number>()
  for (const item of stock) {
    valueByProduct.set(
      item.productId,
      (valueByProduct.get(item.productId) ?? 0) + item.balance * item.productPrice,
    )
    balanceByProduct.set(
      item.productId,
      (balanceByProduct.get(item.productId) ?? 0) + item.balance,
    )
  }

  const productPoints: ProductChartPoint[] = [...valueByProduct.keys()].map((productId) => ({
    productId,
    name: productById.get(productId)?.name ?? 'Produto removido',
    balance: balanceByProduct.get(productId) ?? 0,
    value: valueByProduct.get(productId) ?? 0,
  }))

  const stockValueRanking = [...productPoints].sort((a, b) => b.value - a.value)

  const valueByStorage = new Map<string, number>()
  const storageNameById = new Map<string, string>()
  for (const item of stock) {
    valueByStorage.set(
      item.storageId,
      (valueByStorage.get(item.storageId) ?? 0) + item.balance * item.productPrice,
    )
    storageNameById.set(item.storageId, item.storageName)
  }
  const totalStockValue = [...valueByStorage.values()].reduce((sum, value) => sum + value, 0)
  const storageShares = [...valueByStorage.entries()]
    .map(([storageId, value]) => ({
      storageId,
      storageName: storageNameById.get(storageId) ?? 'Armazém removido',
      value,
      share: totalStockValue > 0 ? value / totalStockValue : 0,
    }))
    .sort((a, b) => b.value - a.value)

  const colorSlotByProductId = new Map<string, string>()
  products.slice(0, MAX_PRODUCT_SLOTS).forEach((product, index) => {
    colorSlotByProductId.set(product.id, CATEGORICAL[index])
  })

  const storageRows = new Map<string, Record<string, number | string>>()
  for (const item of stock) {
    const seriesKey = colorSlotByProductId.has(item.productId) ? item.productName : 'Outros'
    if (!storageRows.has(item.storageId)) {
      storageRows.set(item.storageId, { storageId: item.storageId, storageName: item.storageName })
    }
    const row = storageRows.get(item.storageId)!
    const current = typeof row[seriesKey] === 'number' ? (row[seriesKey] as number) : 0
    row[seriesKey] = current + item.balance * item.productPrice
  }
  const storageStackData = [...storageRows.values()]

  const stackSeries = products
    .filter((product) => colorSlotByProductId.has(product.id))
    .map((product) => ({ key: product.name, color: colorSlotByProductId.get(product.id)! }))
    .filter((series) => storageStackData.some((row) => typeof row[series.key] === 'number'))

  if (storageStackData.some((row) => typeof row.Outros === 'number')) {
    stackSeries.push({ key: 'Outros', color: OUTROS_COLOR })
  }

  return (
    <div>
      <CategoryMenu active={activeCategory} onSelect={setCategoryKey} />

      {categoryKey === 'geral' && (
        <>
          <div className="mt-3 inline-flex items-center gap-2 rounded-full border border-dashed border-line px-3 py-1 text-xs font-medium text-ink-muted">
            Prévia com dados fictícios — módulo financeiro ainda não implementado
          </div>

          <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {KPIS.map((kpi) => (
              <div
                key={kpi.label}
                className="rounded-2xl border border-dashed border-line bg-cream-soft p-6 opacity-80"
              >
                <p className="text-xs font-medium uppercase tracking-wide text-ink-muted">
                  {kpi.label}
                </p>
                <p className="mt-2 text-2xl font-bold tabular-nums text-ink">
                  {currency(kpi.value)}
                </p>
              </div>
            ))}
          </div>

          <div className="mt-6 rounded-2xl border border-dashed border-line bg-cream-soft p-6 opacity-80">
            <p className="text-sm font-semibold text-ink">Faturamento — últimos 6 meses</p>
            <div className="mt-4 h-64">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={REVENUE_TREND}>
                  <CartesianGrid strokeDasharray="3 3" stroke={COLORS.line} vertical={false} />
                  <XAxis
                    dataKey="mes"
                    stroke={COLORS.inkMuted}
                    fontSize={12}
                    tickLine={false}
                    axisLine={false}
                  />
                  <YAxis
                    stroke={COLORS.inkMuted}
                    fontSize={12}
                    tickLine={false}
                    axisLine={false}
                    tickFormatter={(value: number) => `${value / 1000}k`}
                  />
                  <Tooltip
                    cursor={{ fill: COLORS.line, opacity: 0.3 }}
                    contentStyle={{
                      background: COLORS.cream,
                      border: `1px solid ${COLORS.line}`,
                      borderRadius: 8,
                      fontSize: 12,
                    }}
                    formatter={(value) => currency(Number(value))}
                  />
                  <Bar dataKey="valor" fill="#f2a92e" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>
        </>
      )}

      {categoryKey === 'produtos' && (
        <div className="mt-6 space-y-6">
          {productsError && <p className="text-sm text-red-600">{productsError}</p>}

          <div className="rounded-2xl border border-line bg-cream-soft p-6">
            <p className="text-sm font-semibold text-ink">Valor em estoque por produto</p>
          
            <div className="mt-4" style={{ height: Math.max(220, stockValueRanking.length * 44) }}>
              {loadingProducts ? (
                <p className="text-sm text-ink-muted">Carregando...</p>
              ) : stockValueRanking.length === 0 ? (
                <p className="text-sm text-ink-muted">Nenhum saldo registrado ainda.</p>
              ) : (
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart
                    data={stockValueRanking}
                    layout="vertical"
                    margin={{ top: 8, right: 64, left: 0, bottom: 8 }}
                  >
                    <CartesianGrid strokeDasharray="3 3" stroke={COLORS.line} horizontal={false} />
                    <XAxis
                      type="number"
                      stroke={COLORS.inkMuted}
                      fontSize={12}
                      tickLine={false}
                      axisLine={false}
                      tickFormatter={compactCurrency}
                    />
                    <YAxis
                      type="category"
                      dataKey="name"
                      width={150}
                      stroke={COLORS.inkMuted}
                      fontSize={12}
                      tickLine={false}
                      axisLine={false}
                    />
                    <Tooltip
                      cursor={{ fill: COLORS.line, opacity: 0.3 }}
                      contentStyle={{
                        background: COLORS.cream,
                        border: `1px solid ${COLORS.line}`,
                        borderRadius: 8,
                        fontSize: 12,
                      }}
                      formatter={(value) => currency(Number(value))}
                    />
                    <Bar dataKey="value" fill={COLORS.accent} radius={[0, 4, 4, 0]} barSize={20}>
                      <LabelList
                        dataKey="value"
                        position="right"
                        formatter={(value) => compactCurrency(Number(value))}
                        fill={COLORS.inkMuted}
                        fontSize={12}
                      />
                    </Bar>
                  </BarChart>
                </ResponsiveContainer>
              )}
            </div>
          </div>

          <div className="rounded-2xl border border-line bg-cream-soft p-6">
            <p className="text-sm font-semibold text-ink">Concentração por armazém</p>
            <div className="mt-5 flex flex-col gap-4">
              {loadingProducts ? (
                <p className="text-sm text-ink-muted">Carregando...</p>
              ) : storageShares.length === 0 ? (
                <p className="text-sm text-ink-muted">Nenhum saldo registrado ainda.</p>
              ) : (
                storageShares.map((row) => (
                  <div key={row.storageId}>
                    <div className="flex items-baseline justify-between text-sm">
                      <span className="font-medium text-ink">{row.storageName}</span>
                      <span className="tabular-nums text-ink-muted">
                        {currency(row.value)} ·{' '}
                        {(row.share * 100).toLocaleString('pt-BR', { maximumFractionDigits: 0 })}%
                      </span>
                    </div>
                    <div
                      className="mt-1.5 h-2 overflow-hidden rounded-full"
                      style={{ background: 'rgba(168,105,10,0.15)' }}
                    >
                      <div
                        className="h-full rounded-full"
                        style={{ width: `${Math.round(row.share * 100)}%`, background: COLORS.accent }}
                      />
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>

          <div className="rounded-2xl border border-line bg-cream-soft p-6">
            <p className="text-sm font-semibold text-ink">Valor em estoque por armazém</p>
            <div
              className="mt-4"
              style={{ height: Math.max(220, storageStackData.length * 72 + 70) }}
            >
              {loadingProducts ? (
                <p className="text-sm text-ink-muted">Carregando...</p>
              ) : storageStackData.length === 0 ? (
                <p className="text-sm text-ink-muted">Nenhum saldo registrado ainda.</p>
              ) : (
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart
                    data={storageStackData}
                    layout="vertical"
                    margin={{ top: 8, right: 16, left: 0, bottom: 8 }}
                  >
                    <CartesianGrid strokeDasharray="3 3" stroke={COLORS.line} horizontal={false} />
                    <XAxis
                      type="number"
                      stroke={COLORS.inkMuted}
                      fontSize={12}
                      tickLine={false}
                      axisLine={false}
                      tickFormatter={compactCurrency}
                    />
                    <YAxis
                      type="category"
                      dataKey="storageName"
                      width={130}
                      stroke={COLORS.inkMuted}
                      fontSize={12}
                      tickLine={false}
                      axisLine={false}
                    />
                    <Tooltip
                      cursor={{ fill: COLORS.line, opacity: 0.3 }}
                      contentStyle={{
                        background: COLORS.cream,
                        border: `1px solid ${COLORS.line}`,
                        borderRadius: 8,
                        fontSize: 12,
                      }}
                      formatter={(value) => currency(Number(value))}
                    />
                    <Legend
                      iconType="rect"
                      wrapperStyle={{ fontSize: 12 }}
                      formatter={(value: string) => <span style={{ color: COLORS.inkMuted }}>{value}</span>}
                    />
                    {stackSeries.map((series, index) => (
                      <Bar
                        key={series.key}
                        dataKey={series.key}
                        stackId="value"
                        fill={series.color}
                        stroke={COLORS.creamSoft}
                        strokeWidth={2}
                        radius={index === stackSeries.length - 1 ? [0, 4, 4, 0] : 0}
                      />
                    ))}
                  </BarChart>
                </ResponsiveContainer>
              )}
            </div>

            {!loadingProducts && storageStackData.length > 0 && (
              <details className="mt-4 text-sm text-ink-muted">
                <summary className="cursor-pointer select-none font-medium text-ink-muted hover:text-ink">
                  Ver detalhamento em tabela
                </summary>
                <div className="mt-3 overflow-x-auto">
                  <table className="w-full text-left text-xs">
                    <thead>
                      <tr className="border-b border-line text-ink-muted">
                        <th className="py-1.5 font-medium">Armazém</th>
                        <th className="py-1.5 font-medium">Produto</th>
                        <th className="py-1.5 text-right font-medium">Valor</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-line">
                      {storageStackData.flatMap((row) =>
                        stackSeries
                          .filter((series) => typeof row[series.key] === 'number')
                          .map((series) => (
                            <tr key={`${row.storageId}-${series.key}`}>
                              <td className="py-1.5 text-ink">{row.storageName as string}</td>
                              <td className="py-1.5 text-ink-muted">{series.key}</td>
                              <td className="py-1.5 text-right tabular-nums text-ink">
                                {currency(row[series.key] as number)}
                              </td>
                            </tr>
                          )),
                      )}
                    </tbody>
                  </table>
                </div>
              </details>
            )}
          </div>
        </div>
      )}
    </div>
  )
}

export default DashboardPage
