import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Toaster } from 'react-hot-toast'
import { AuthProvider } from '@/auth/AuthContext'
import ProtectedRoute from '@/shared/components/ProtectedRoute'

import LoginPage              from '@/auth/LoginPage'
import AdminDashboard         from '@/admin/pages/AdminDashboard'
import ClassListPage          from '@/admin/pages/ClassListPage'
import ClassDetailPage        from '@/admin/pages/ClassDetailPage'
import MaterialListPage       from '@/admin/pages/MaterialListPage'
import UserManagementPage     from '@/admin/pages/UserManagementPage'
import ClassSubjectSelectPage from '@/student/pages/ClassSubjectSelectPage'
import ChatPage               from '@/student/pages/ChatPage'

const qc = new QueryClient({ defaultOptions: { queries: { retry: 1, staleTime: 30_000 } } })

export default function App() {
  return (
    <QueryClientProvider client={qc}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />

            {/* Admin */}
            <Route path="/admin/dashboard" element={<ProtectedRoute role="Admin"><AdminDashboard /></ProtectedRoute>} />
            <Route path="/admin/classes"   element={<ProtectedRoute role="Admin"><ClassListPage /></ProtectedRoute>} />
            <Route path="/admin/classes/:id" element={<ProtectedRoute role="Admin"><ClassDetailPage /></ProtectedRoute>} />
            <Route path="/admin/materials" element={<ProtectedRoute role="Admin"><MaterialListPage /></ProtectedRoute>} />
            <Route path="/admin/users"     element={<ProtectedRoute role="Admin"><UserManagementPage /></ProtectedRoute>} />

            {/* Student */}
            <Route path="/student/select" element={<ProtectedRoute role="Student"><ClassSubjectSelectPage /></ProtectedRoute>} />
            <Route path="/student/chat/:classId/:subjectId" element={<ProtectedRoute role="Student"><ChatPage /></ProtectedRoute>} />

            <Route path="*" element={<Navigate to="/login" replace />} />
          </Routes>
        </BrowserRouter>
        <Toaster position="top-right" toastOptions={{ className: 'font-display text-sm', duration: 3000 }} />
      </AuthProvider>
    </QueryClientProvider>
  )
}
