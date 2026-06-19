import { createContext, useContext, useState, useCallback, type ReactNode } from 'react'
import type { AuthState, UserRole } from '@/types'

interface AuthContextValue extends AuthState {
  login: (token: string, role: UserRole, userId: string, fullName: string) => void
  logout: () => void
  isAuthenticated: boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(() => {
    const token = sessionStorage.getItem('__edurag_token')
    const role  = sessionStorage.getItem('__edurag_role') as UserRole | null
    const userId    = sessionStorage.getItem('__edurag_uid')
    const fullName  = sessionStorage.getItem('__edurag_name')
    return { token, role, userId, fullName }
  })

  const login = useCallback((token: string, role: UserRole, userId: string, fullName: string) => {
    sessionStorage.setItem('__edurag_token', token)
    sessionStorage.setItem('__edurag_role',  role)
    sessionStorage.setItem('__edurag_uid',   userId)
    sessionStorage.setItem('__edurag_name',  fullName)
    setState({ token, role, userId, fullName })
  }, [])

  const logout = useCallback(() => {
    sessionStorage.clear()
    setState({ token: null, role: null, userId: null, fullName: null })
  }, [])

  return (
    <AuthContext.Provider value={{ ...state, login, logout, isAuthenticated: !!state.token }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
