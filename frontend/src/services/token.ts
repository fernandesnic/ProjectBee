const TOKEN_KEY = 'projectbee_token'

export const saveToken = (token: string) => localStorage.setItem(TOKEN_KEY, token)
export const getToken = () => localStorage.getItem(TOKEN_KEY)
export const clearToken = () => localStorage.removeItem(TOKEN_KEY)

// O evento "storage" só dispara nas outras abas: é como uma aba fica sabendo que a outra saiu.
export function onTokenRemovedInOtherTab(callback: () => void): () => void {
  function handleStorage(event: StorageEvent) {
    if (event.key === TOKEN_KEY && event.newValue === null) callback()
    if (event.key === null) callback() // localStorage.clear()
  }
  window.addEventListener('storage', handleStorage)
  return () => window.removeEventListener('storage', handleStorage)
}

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

export interface DecodedToken {
  email?: string
  roles: string[]
  expiresAt?: number // epoch em ms
}

export function decodeToken(token: string): DecodedToken {
  try {
    const payload = token.split('.')[1]
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    const claims = JSON.parse(decoded)

    const roleClaim = claims[ROLE_CLAIM]
    const roles: string[] = Array.isArray(roleClaim) ? roleClaim : roleClaim ? [roleClaim] : []
    const expiresAt = typeof claims.exp === 'number' ? claims.exp * 1000 : undefined

    return { email: claims.email as string | undefined, roles, expiresAt }
  } catch {
    return { email: undefined, roles: [] }
  }
}

// Token guardado e ainda dentro da validade. Um token vencido é apagado aqui mesmo,
// para a tela não abrir como logada e só descobrir no primeiro 401.
export function getValidToken(): string | null {
  const token = getToken()
  if (!token) return null

  const { expiresAt } = decodeToken(token)
  if (expiresAt !== undefined && expiresAt <= Date.now()) {
    clearToken()
    return null
  }
  return token
}
