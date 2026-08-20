import { Link, NavLink, Navigate, Outlet, useNavigate } from 'react-router-dom'
import { clearToken, decodeToken, getToken } from '../services/token'

export interface AppLayoutContext {
  email?: string
  roles: string[]
}

const NAV_ITEMS = [
  { to: '/app', label: 'Dashboard', end: true },
  { to: '/app/products', label: 'Produtos', end: false },
]

const PLANNED_ITEMS = ['Faturamento', 'Financeiro']

function AppLayout() {
  const navigate = useNavigate()
  const token = getToken()

  if (!token) {
    return <Navigate to="/login" replace />
  }

  const { email, roles } = decodeToken(token)

  function handleLogout() {
    clearToken()
    navigate('/')
  }

  return (
    <div className="flex min-h-screen bg-cream text-ink">
      <aside className="flex w-56 shrink-0 flex-col border-r border-line bg-cream-soft px-4 py-6">
        <Link to="/app" className="px-2 text-lg font-bold tracking-tight text-ink">
          ProjectBee
        </Link>

        <nav className="mt-8 flex flex-col gap-1">
          {NAV_ITEMS.map((item) => (
            <NavLink
              key={item.label}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                `rounded-lg px-3 py-2 text-sm font-medium transition ${
                  isActive
                    ? 'bg-honey/15 text-honey-dark'
                    : 'text-ink-muted hover:bg-cream hover:text-ink'
                }`
              }
            >
              {item.label}
            </NavLink>
          ))}

          {PLANNED_ITEMS.map((label) => (
            <span
              key={label}
              className="flex items-center justify-between rounded-lg px-3 py-2 text-sm text-ink-muted/50"
            >
              {label}
              <span className="rounded-full bg-line/60 px-2 py-0.5 text-[10px] font-medium">
                planejado
              </span>
            </span>
          ))}
        </nav>
      </aside>

      <div className="flex-1">
        <header className="flex items-center justify-between border-b border-line px-8 py-4">
          <p className="text-sm text-ink-muted">
            {email ? (
              <>
                Logado como <span className="font-medium text-ink">{email}</span>
              </>
            ) : (
              'Sessão ativa'
            )}
          </p>
          <button
            onClick={handleLogout}
            className="rounded-full border border-line px-4 py-1.5 text-sm font-medium text-ink transition hover:border-honey-dark hover:text-honey-dark"
          >
            Sair
          </button>
        </header>

        <main className="p-8">
          <Outlet context={{ email, roles } satisfies AppLayoutContext} />
        </main>
      </div>
    </div>
  )
}

export default AppLayout
