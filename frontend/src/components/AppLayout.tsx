import { useState } from 'react'
import { Link, NavLink, Navigate, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { clearToken, decodeToken, getToken } from '../services/token'

export interface AppLayoutContext {
  email?: string
  roles: string[]
}

interface NavChild {
  to?: string
  label: string
}

interface NavGroup {
  label: string
  children: NavChild[]
}

const NAV_GROUPS: NavGroup[] = [
  {
    label: 'Cadastros',
    children: [
      { to: '/app/products', label: 'Produtos' },
      { to: '/app/storages', label: 'Armazéns' },
      { label: 'Clientes' },
    ],
  },
  {
    label: 'Estoque',
    children: [{ to: '/app/stock', label: 'Saldos' }],
  },
  {
    label: 'Faturamento',
    children: [{ label: 'Notas' }, { label: 'Emissão' }],
  },
  {
    label: 'Financeiro',
    children: [
      { label: 'Contas a receber' },
      { label: 'Contas a pagar' },
      { label: 'Fluxo de caixa' },
    ],
  },
]

const navLinkClassName = ({ isActive }: { isActive: boolean }) =>
  `rounded-lg px-3 py-2 text-sm font-medium transition ${
    isActive ? 'bg-honey/15 text-honey-dark' : 'text-ink-muted hover:bg-cream hover:text-ink'
  }`

function AppLayout() {
  const navigate = useNavigate()
  const location = useLocation()
  const token = getToken()

  const [openGroups, setOpenGroups] = useState<Set<string>>(() => {
    const activeGroup = NAV_GROUPS.find((group) =>
      group.children.some((child) => child.to && location.pathname.startsWith(child.to)),
    )
    return new Set(activeGroup ? [activeGroup.label] : [])
  })

  if (!token) {
    return <Navigate to="/login" replace />
  }

  const { email, roles } = decodeToken(token)

  function toggleGroup(label: string) {
    setOpenGroups((current) => {
      const next = new Set(current)
      if (next.has(label)) {
        next.delete(label)
      } else {
        next.add(label)
      }
      return next
    })
  }

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
          <NavLink to="/app" end className={navLinkClassName}>
            Dashboard
          </NavLink>

          {NAV_GROUPS.map((group) => {
            const isOpen = openGroups.has(group.label)
            return (
              <div key={group.label} className="mt-2">
                <button
                  type="button"
                  onClick={() => toggleGroup(group.label)}
                  className="flex w-full items-center justify-between rounded-lg px-3 py-2 text-xs font-semibold uppercase tracking-wide text-ink-muted/70 transition hover:text-ink"
                >
                  {group.label}
                  <span
                    className={`transition-transform ${isOpen ? 'rotate-90' : ''}`}
                    aria-hidden="true"
                  >
                    ›
                  </span>
                </button>
                {isOpen && (
                  <div className="mt-1 flex flex-col gap-1">
                    {group.children.map((item) =>
                      item.to ? (
                        <NavLink key={item.label} to={item.to} className={navLinkClassName}>
                          {item.label}
                        </NavLink>
                      ) : (
                        <span
                          key={item.label}
                          className="flex items-center justify-between rounded-lg px-3 py-2 text-sm text-ink-muted/50"
                        >
                          {item.label}
                          <span className="rounded-full bg-line/60 px-2 py-0.5 text-[10px] font-medium">
                            planejado
                          </span>
                        </span>
                      ),
                    )}
                  </div>
                )}
              </div>
            )
          })}
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
