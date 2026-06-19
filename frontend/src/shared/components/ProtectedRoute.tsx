import { Navigate } from 'react-router-dom'
import { useAuth } from '@/auth/AuthContext'
import type { UserRole } from '@/types'

interface Props {
  role: UserRole
  children: React.ReactNode
}

export default function ProtectedRoute({ role, children }: Props) {
  const { isAuthenticated, role: userRole } = useAuth()
  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (userRole !== role)  return <Navigate to="/login" replace />
  return <>{children}</>
}
