const TOKEN_KEY = 'projectbee_token'

export const saveToken = (token: string) => localStorage.setItem(TOKEN_KEY, token)
export const getToken = () => localStorage.getItem(TOKEN_KEY)
export const clearToken = () => localStorage.removeItem(TOKEN_KEY)

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

export interface DecodedToken {
  email?: string
  roles: string[]
}

export function decodeToken(token: string): DecodedToken {
  try {
    const payload = token.split('.')[1]
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    const claims = JSON.parse(decoded)

    const roleClaim = claims[ROLE_CLAIM]
    const roles: string[] = Array.isArray(roleClaim) ? roleClaim : roleClaim ? [roleClaim] : []

    return { email: claims.email as string | undefined, roles }
  } catch {
    return { email: undefined, roles: [] }
  }
}
