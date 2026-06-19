import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { ArrowLeft, Plus, UserCircle } from 'lucide-react'
import toast from 'react-hot-toast'
import type { UserDto } from '@/types'

export default function UserManagementPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)
  const [form, setForm] = useState({ email: '', fullName: '', password: '', role: 1 })

  const { data: users, isLoading } = useQuery<UserDto[]>({
    queryKey: ['admin-users'],
    queryFn: () => api.get('/admin/users').then(r => r.data),
  })

  const register = useMutation({
    mutationFn: () => api.post('/auth/register', form),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-users'] }); toast.success('User created.'); setOpen(false); setForm({ email: '', fullName: '', password: '', role: 1 }) },
    onError: (e: any) => toast.error(e.response?.data?.message ?? 'Failed to create user.'),
  })

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-4xl mx-auto">
        <div className="flex items-center gap-3 mb-6">
          <Button variant="ghost" size="icon" onClick={() => navigate('/admin/dashboard')}><ArrowLeft className="w-5 h-5" /></Button>
          <div><h1 className="text-2xl font-bold">Users</h1><p className="text-sm text-gray-500">{users?.length ?? 0} accounts</p></div>
          <Button className="ml-auto" onClick={() => setOpen(true)}>
            <Plus className="w-4 h-4 mr-2" /> New User
          </Button>
        </div>

        <Card>
          <CardContent className="p-0">
            {isLoading
              ? <div className="p-4 space-y-3">{Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-14" />)}</div>
              : users?.map(u => (
                <div key={u.id} className="flex items-center gap-4 px-5 py-4 border-b last:border-b-0">
                  <UserCircle className="w-9 h-9 text-gray-300 shrink-0" />
                  <div className="flex-1 min-w-0">
                    <div className="font-medium">{u.fullName}</div>
                    <div className="text-sm text-gray-500">{u.email}</div>
                  </div>
                  <Badge variant={u.role === 0 ? 'default' : 'secondary'}>{u.role === 0 ? 'Admin' : 'Student'}</Badge>
                  <Badge variant={u.isActive ? 'success' : 'secondary'}>{u.isActive ? 'Active' : 'Inactive'}</Badge>
                </div>
              ))
            }
            {!isLoading && !users?.length && <div className="p-12 text-center text-gray-400">No users found.</div>}
          </CardContent>
        </Card>
      </div>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader><DialogTitle>Create User</DialogTitle></DialogHeader>
          <div className="space-y-3 pt-2">
            <Input placeholder="Full name" value={form.fullName} onChange={e => setForm(f => ({ ...f, fullName: e.target.value }))} />
            <Input type="email" placeholder="Email" value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} />
            <Input type="password" placeholder="Password (min 8 chars)" value={form.password} onChange={e => setForm(f => ({ ...f, password: e.target.value }))} />
            <select className="h-9 w-full rounded-md border border-input px-3 text-sm bg-background" value={form.role} onChange={e => setForm(f => ({ ...f, role: +e.target.value }))}>
              <option value={1}>Student</option>
              <option value={0}>Admin</option>
            </select>
            <Button className="w-full" onClick={() => register.mutate()} disabled={register.isPending || !form.email || !form.password || !form.fullName}>
              {register.isPending ? 'Creating…' : 'Create User'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}
