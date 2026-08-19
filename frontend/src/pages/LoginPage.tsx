import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5054'

function LoginPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const isDemo = searchParams.get('demo') === '1'

  const [email, setEmail] = useState(isDemo ? 'demo@projectbee.dev' : '')
  const [password, setPassword] = useState(isDemo ? 'Demo@1234' : '')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setLoading(true)

    try {
      const { data } = await axios.post<{ token: string }>(`${API_URL}/api/auth/login`, {
        Email: email,
        Password: password,
      })
      localStorage.setItem('projectbee_token', data.token)
      navigate('/app')
    } catch (err) {
      if (axios.isAxiosError(err)) {
        if (err.response?.status === 401) {
          setError('E-mail ou senha inválidos.')
        } else if (err.response) {
          setError(`Erro no servidor (${err.response.status}). Tente novamente.`)
        } else {
          setError(`Não foi possível conectar à API em ${API_URL}. Ela está rodando?`)
        }
      } else {
        setError('Erro inesperado. Tente novamente.')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-cream px-6 text-ink">
      <div className="w-full max-w-sm">
        <Link to="/" className="text-sm text-ink-muted transition hover:text-ink">
          ← Voltar
        </Link>

        <h1 className="mt-6 text-2xl font-bold tracking-tight">Entrar no ProjectBee</h1>
        {isDemo && (
          <p className="mt-2 text-sm text-ink-muted">
            Campos preenchidos com o acesso de demonstração.
          </p>
        )}

        <form onSubmit={handleSubmit} className="mt-8 space-y-4">
          <div>
            <label htmlFor="email" className="text-sm font-medium text-ink-muted">
              E-mail
            </label>
            <input
              id="email"
              type="email"
              required
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              className="mt-1 w-full rounded-lg border border-line bg-cream-soft px-3 py-2 text-sm text-ink outline-none transition focus:border-honey-dark"
            />
          </div>

          <div>
            <label htmlFor="password" className="text-sm font-medium text-ink-muted">
              Senha
            </label>
            <input
              id="password"
              type="password"
              required
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              className="mt-1 w-full rounded-lg border border-line bg-cream-soft px-3 py-2 text-sm text-ink outline-none transition focus:border-honey-dark"
            />
          </div>

          {error && <p className="text-sm text-red-600">{error}</p>}

          <button
            type="submit"
            disabled={loading}
            className="w-full rounded-full bg-honey px-6 py-2.5 text-sm font-semibold text-hive transition hover:bg-honey-light disabled:opacity-60"
          >
            {loading ? 'Entrando...' : 'Entrar'}
          </button>
        </form>

        {!isDemo && (
          <p className="mt-6 text-xs text-ink-muted">
            Acesso de demonstração: <span className="font-mono">demo@projectbee.dev</span> ·{' '}
            <span className="font-mono">Demo@1234</span>
          </p>
        )}
      </div>
    </div>
  )
}

export default LoginPage
