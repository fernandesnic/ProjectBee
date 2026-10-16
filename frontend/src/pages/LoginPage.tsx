import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import axios from 'axios'
import { isColdStartError, login } from '../services/api'
import { saveToken } from '../services/token'

const MAX_ATTEMPTS = 4
const WAIT_BETWEEN_MS = 3000

const sleep = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms))

function LoginPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const isDemo = searchParams.get('demo') === '1'

  const demoPassword = import.meta.env.VITE_DEMO_PASSWORD ?? 'Demo@1234'

  const [email, setEmail] = useState(isDemo ? 'demo@projectbee.dev' : '')
  const [password, setPassword] = useState(isDemo ? demoPassword : '')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [waking, setWaking] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setLoading(true)

    try {
      for (let attempt = 1; attempt <= MAX_ATTEMPTS; attempt++) {
        try {
          const { token } = await login({ email, password })
          saveToken(token)
          navigate('/app')
          return
        } catch (err) {
          const ultimaTentativa = attempt === MAX_ATTEMPTS
          if (!isColdStartError(err) || ultimaTentativa) throw err

          setWaking(true) // avisa a tela: API está acordando
          await sleep(WAIT_BETWEEN_MS)
        }
      }
    } catch (err) {
      if (axios.isAxiosError(err)) {
        if (err.response?.status === 401) {
          setError('E-mail ou senha inválidos.')
        } else if (err.response) {
          setError(`Erro no servidor (${err.response.status}). Tente novamente.`)
        } else {
          setError('Não foi possível conectar ao servidor agora. Tente novamente em instantes.')
        }
      } else {
        setError('Erro inesperado. Tente novamente.')
      }
    } finally {
      setLoading(false)
      setWaking(false)
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

          {error && <p className="text-sm text-danger" role="alert">{error}</p>}

          {waking && (
            <p className="text-xs text-ink-muted">
              Acordando a API, a primeira conexão pode levar até 30 segundos...
            </p>
          )}

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
            <span className="font-mono">{demoPassword}</span>
          </p>
        )}
      </div>
    </div>
  )
}

export default LoginPage