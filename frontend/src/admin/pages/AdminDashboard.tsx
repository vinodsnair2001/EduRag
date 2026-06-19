import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import api from '@/lib/api'
import { useAuth } from '@/auth/AuthContext'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { BookOpen, Layers, FileText, Users, LogOut, UploadCloud, Sparkles } from 'lucide-react'
import type { ClassDto, MaterialDto, UserDto } from '@/types'

export default function AdminDashboard() {
  const { fullName, logout } = useAuth()
  const navigate = useNavigate()

  const { data: classes }   = useQuery<ClassDto[]>   ({ queryKey: ['admin-classes'],   queryFn: () => api.get('/admin/classes').then(r => r.data) })
  const { data: materials } = useQuery<MaterialDto[]>({ queryKey: ['admin-materials'], queryFn: () => api.get('/admin/materials').then(r => r.data) })
  const { data: users }     = useQuery<UserDto[]>    ({ queryKey: ['admin-users'],     queryFn: () => api.get('/admin/users').then(r => r.data) })

  const stats = [
    { label: 'Classes',   value: classes?.length ?? '—',   icon: BookOpen,    color: 'bg-violet-100 text-violet-700',  action: () => navigate('/admin/classes') },
    { label: 'Materials', value: materials?.length ?? '—', icon: FileText,    color: 'bg-blue-100 text-blue-700',      action: () => navigate('/admin/materials') },
    { label: 'Users',     value: users?.length ?? '—',     icon: Users,       color: 'bg-green-100 text-green-700',    action: () => navigate('/admin/users') },
    { label: 'Vectorized',value: materials?.filter(m => m.status === 2).length ?? '—', icon: Layers, color: 'bg-amber-100 text-amber-700', action: () => navigate('/admin/materials') },
  ]

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b px-6 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 bg-brand-500 rounded-lg flex items-center justify-center">
            <Sparkles className="w-5 h-5 text-white" />
          </div>
          <div>
            <h1 className="font-semibold text-gray-900">EduRAG Admin</h1>
            <p className="text-xs text-gray-500">Welcome, {fullName}</p>
          </div>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={() => navigate('/admin/classes')}>
            <BookOpen className="w-4 h-4 mr-2" /> Classes
          </Button>
          <Button variant="outline" size="sm" onClick={() => navigate('/admin/materials')}>
            <UploadCloud className="w-4 h-4 mr-2" /> Materials
          </Button>
          <Button variant="outline" size="sm" onClick={() => navigate('/admin/users')}>
            <Users className="w-4 h-4 mr-2" /> Users
          </Button>
          <Button variant="ghost" size="sm" onClick={() => { logout(); navigate('/login') }}>
            <LogOut className="w-4 h-4" />
          </Button>
        </div>
      </header>

      <main className="p-6 max-w-6xl mx-auto">
        <div className="mb-8">
          <h2 className="text-2xl font-bold text-gray-900">Dashboard</h2>
          <p className="text-gray-500 mt-1">Overview of your EduRAG platform</p>
        </div>

        {/* Stats Grid */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          {stats.map((s) => (
            <Card key={s.label} className="cursor-pointer hover:shadow-md transition-shadow" onClick={s.action}>
              <CardContent className="p-5">
                <div className={`inline-flex p-2 rounded-lg ${s.color} mb-3`}>
                  <s.icon className="w-5 h-5" />
                </div>
                <div className="text-2xl font-bold text-gray-900">{s.value}</div>
                <div className="text-sm text-gray-500 mt-0.5">{s.label}</div>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Quick Actions */}
        <Card>
          <CardHeader><CardTitle>Quick Actions</CardTitle></CardHeader>
          <CardContent className="flex flex-wrap gap-3">
            <Button onClick={() => navigate('/admin/classes')}>
              <BookOpen className="w-4 h-4 mr-2" /> Manage Classes
            </Button>
            <Button variant="outline" onClick={() => navigate('/admin/materials')}>
              <UploadCloud className="w-4 h-4 mr-2" /> Upload Material
            </Button>
            <Button variant="outline" onClick={() => navigate('/admin/users')}>
              <Users className="w-4 h-4 mr-2" /> Manage Users
            </Button>
          </CardContent>
        </Card>
      </main>
    </div>
  )
}
