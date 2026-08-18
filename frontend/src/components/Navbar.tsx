import { Link } from 'react-router-dom'

export function Navbar() {
  return (
    <nav className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
      <Link to="/" className="text-lg font-bold">
        ProjectBee
      </Link>
      <Link to="/login" className="text-sm text-gray-600 hover:text-gray-900">
        Entrar
      </Link>
    </nav>
  )
}
