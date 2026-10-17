import { Link } from 'react-router-dom'
import { getValidToken } from '../services/token'

function NotFoundPage() {
  const loggedIn = getValidToken() !== null

  return (
    <div className="flex min-h-screen items-center justify-center bg-cream px-6 text-ink">
      <div className="w-full max-w-sm">
        <p className="text-sm font-semibold tabular-nums text-honey-text">404</p>
        <h1 className="mt-2 text-2xl font-bold tracking-tight">Página não encontrada</h1>
        <p className="mt-2 text-sm text-ink-muted">
          O endereço não existe ou foi digitado errado.
        </p>
        <Link
          to={loggedIn ? '/app' : '/'}
          className="mt-8 inline-block rounded-full bg-honey px-6 py-2.5 text-sm font-semibold text-hive transition hover:bg-honey-light"
        >
          {loggedIn ? 'Voltar ao painel' : 'Voltar ao início'}
        </Link>
      </div>
    </div>
  )
}

export default NotFoundPage
