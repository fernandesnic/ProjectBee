import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'

const COLORS = {
  honey: '#f2a92e',
  line: '#e6dcc2',
  inkMuted: '#6e6455',
  cream: '#fbf7ec',
}

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

function DashboardPage() {
  return (
    <div>
      <h1 className="text-2xl font-bold tracking-tight text-ink">Visão geral</h1>

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
              <Bar dataKey="valor" fill={COLORS.honey} radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>
    </div>
  )
}

export default DashboardPage
