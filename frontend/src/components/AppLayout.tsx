import { useEffect, useState } from 'react'
import { Link, NavLink, Navigate, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { clearToken, decodeToken, getValidToken } from '../services/token'

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
    isActive ? 'bg-honey/15 text-honey-text' : 'text-ink-muted hover:bg-cream hover:text-ink'
  }`

function MenuIcon({ open }: { open: boolean }) {
  return (
    <svg
      viewBox="0 0 24 24"
      className="h-5 w-5"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      aria-hidden="true"
    >
      {open ? <path d="M6 6l12 12M18 6L6 18" /> : <path d="M4 6h16M4 12h16M4 18h16" />}
    </svg>
  )
}

function AppLayout() {
  const navigate = useNavigate()
  const location = useLocation()
  const token = getValidToken()

  const [openGroups, setOpenGroups] = useState<Set<string>>(() => {
    const activeGroup = NAV_GROUPS.find((group) =>
      group.children.some((child) => child.to && location.pathname.startsWith(child.to)),
    )
    return new Set(activeGroup ? [activeGroup.label] : [])
  })
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const [lastPathname, setLastPathname] = useState(location.pathname)

  if (location.pathname !== lastPathname) {
    setLastPathname(location.pathname)
    setMobileNavOpen(false)
  }

  useEffect(() => {
    if (!mobileNavOpen) return
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') setMobileNavOpen(false)
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [mobileNavOpen])

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
    // replace: o botão Voltar não deve reabrir a tela de onde o usuário saiu.
    navigate('/login', { replace: true })
  }

  return (
    <div className="flex min-h-screen bg-cream text-ink">
      {mobileNavOpen && (
        <div
          className="fixed inset-0 z-30 bg-ink/40 lg:hidden"
          onClick={() => setMobileNavOpen(false)}
          aria-hidden="true"
        />
      )}

      <aside
        className={`fixed inset-y-0 left-0 z-40 flex w-64 shrink-0 -translate-x-full flex-col border-r border-line bg-cream-soft px-4 py-6 transition-transform duration-200 ease-out lg:static lg:z-auto lg:w-56 lg:translate-x-0 ${
          mobileNavOpen ? 'translate-x-0' : ''
        }`}
      >
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
        <header className="flex items-center justify-between gap-3 border-b border-line px-4 py-4 sm:px-6 lg:px-8">
          <div className="flex min-w-0 items-center gap-3">
            <button
              type="button"
              onClick={() => setMobileNavOpen((current) => !current)}
              aria-label={mobileNavOpen ? 'Fechar menu' : 'Abrir menu'}
              aria-expanded={mobileNavOpen}
              className="-ml-1.5 flex h-11 w-11 shrink-0 items-center justify-center rounded-lg text-ink transition hover:bg-cream-soft lg:hidden"
            >
              <MenuIcon open={mobileNavOpen} />
            </button>
            <Link to="/app" className="text-base font-bold tracking-tight text-ink lg:hidden">
              ProjectBee
            </Link>
            <p className="hidden truncate text-sm text-ink-muted sm:block">
              {email ? (
                <>
                  Logado como <span className="font-medium text-ink">{email}</span>
                </>
              ) : (
                'Sessão ativa'
              )}
            </p>
          </div>
          <button
            onClick={handleLogout}
            className="shrink-0 rounded-full border border-line px-4 py-1.5 text-sm font-medium text-ink transition hover:border-honey-dark hover:text-honey-dark"
          >
            Sair
          </button>
        </header>

        <main className="p-4 sm:p-6 lg:p-8">
          <Outlet context={{ email, roles } satisfies AppLayoutContext} />
        </main>
      </div>
    </div>
  )
}

export default AppLayout
