import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { clearToken, getToken } from '../services/token'

function decodeEmailFromToken(token: string): string | null {
  try {
    const payload = token.split('.')[1]
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    return (JSON.parse(decoded) as { email?: string }).email ?? null
  } catch {
    return null
  }
}

function DashboardPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState<string | null>(null)

  useEffect(() => {
    const token = getToken()
    if (!token) {
      navigate('/login')
      return
    }
    setEmail(decodeEmailFromToken(token))
  }, [navigate])

  function handleLogout() {
    clearToken()
    navigate('/')
  }

  return (
    <div className="min-h-screen bg-cream p-8 text-ink">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight">ProjectBee</h1>
        <button
          onClick={handleLogout}
          className="rounded-full border border-line px-4 py-1.5 text-sm font-medium text-ink transition hover:border-honey-dark hover:text-honey-dark"
        >
          Sair
        </button>
      </div>

      <p className="mt-6 text-ink-muted">
        {email ? <>Logado como <span className="font-medium text-ink">{email}</span>.</> : 'Carregando...'}
      </p>
    </div>
  )
}

export default DashboardPage
