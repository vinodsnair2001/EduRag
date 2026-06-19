import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { ArrowLeft, Plus, UserCircle, Pencil, UserX, UserCheck } from 'lucide-react'
import toast from 'react-hot-toast'
import type { UserDto, ClassDto, SubjectDto, StudentPermissionDto } from '@/types'

const EMPTY_CREATE = { email: '', fullName: '', password: '', classId: 0, role: 1 }

function emptyEdit(u: UserDto) {
  return { fullName: u.fullName, email: u.email, classId: u.classId ?? 0, isActive: u.isActive, newPassword: '' }
}

export default function UserManagementPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()

  const [createOpen, setCreateOpen] = useState(false)
  const [createForm, setCreateForm] = useState(EMPTY_CREATE)

  const [editTarget, setEditTarget] = useState<UserDto | null>(null)
  const [editForm, setEditForm] = useState({ fullName: '', email: '', classId: 0, isActive: true, newPassword: '' })
  const [editPermissions, setEditPermissions] = useState<number[]>([])

  const { data: users, isLoading } = useQuery<UserDto[]>({
    queryKey: ['admin-users'],
    queryFn: () => api.get('/admin/users').then(r => r.data),
  })

  const { data: classes = [] } = useQuery<ClassDto[]>({
    queryKey: ['admin-classes'],
    queryFn: () => api.get('/admin/classes').then(r => r.data),
  })

  // Subjects for the class currently selected in the edit dialog
  const { data: editSubjects = [] } = useQuery<SubjectDto[]>({
    queryKey: ['admin-subjects', editForm.classId],
    queryFn: () => api.get(`/admin/classes/${editForm.classId}/subjects`).then(r => r.data),
    enabled: editForm.classId > 0 && editTarget !== null,
  })

  // Current permissions for the student being edited
  const { data: existingPerms } = useQuery<StudentPermissionDto[]>({
    queryKey: ['student-permissions', editTarget?.id],
    queryFn: () => api.get(`/admin/students/${editTarget!.id}/permissions`).then(r => r.data),
    enabled: editTarget !== null,
  })

  // Populate checkboxes when permissions load (or when edit target changes)
  useEffect(() => {
    if (editTarget && existingPerms) {
      setEditPermissions(existingPerms.map(p => p.subjectId))
    }
    if (!editTarget) {
      setEditPermissions([])
    }
  }, [existingPerms, editTarget])

  const createMutation = useMutation({
    mutationFn: () =>
      createForm.role === 1
        ? api.post('/admin/students', {
            email: createForm.email,
            fullName: createForm.fullName,
            password: createForm.password,
            classId: createForm.classId,
          })
        : api.post('/auth/register', {
            email: createForm.email,
            fullName: createForm.fullName,
            password: createForm.password,
            role: 0,
          }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-users'] })
      toast.success('Account created.')
      setCreateOpen(false)
      setCreateForm(EMPTY_CREATE)
    },
    onError: (e: any) => toast.error(e.response?.data?.message ?? 'Failed to create account.'),
  })

  const updateMutation = useMutation({
    mutationFn: async () => {
      await api.put(`/admin/students/${editTarget!.id}`, {
        fullName: editForm.fullName,
        email: editForm.email,
        classId: editForm.classId,
        isActive: editForm.isActive,
        newPassword: editForm.newPassword || null,
      })
      await api.put(`/admin/students/${editTarget!.id}/permissions`, {
        subjectIds: editPermissions,
      })
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-users'] })
      qc.invalidateQueries({ queryKey: ['student-permissions', editTarget?.id] })
      toast.success('Student updated.')
      setEditTarget(null)
    },
    onError: (e: any) => toast.error(e.response?.data?.message ?? 'Failed to update student.'),
  })

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/admin/students/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-users'] })
      toast.success('Student deactivated.')
    },
    onError: (e: any) => toast.error(e.response?.data?.message ?? 'Failed to deactivate student.'),
  })

  const reactivateMutation = useMutation({
    mutationFn: (u: UserDto) =>
      api.put(`/admin/students/${u.id}`, {
        fullName: u.fullName,
        email: u.email,
        classId: u.classId ?? 0,
        isActive: true,
        newPassword: null,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-users'] })
      toast.success('Student reactivated.')
    },
    onError: (e: any) => toast.error(e.response?.data?.message ?? 'Failed to reactivate student.'),
  })

  function openEdit(u: UserDto) {
    setEditForm(emptyEdit(u))
    setEditPermissions([])
    setEditTarget(u)
  }

  function togglePermission(subjectId: number) {
    setEditPermissions(prev =>
      prev.includes(subjectId) ? prev.filter(id => id !== subjectId) : [...prev, subjectId]
    )
  }

  const createValid =
    createForm.email.trim() !== '' &&
    createForm.fullName.trim() !== '' &&
    createForm.password.length >= 8 &&
    (createForm.role === 0 || createForm.classId > 0)

  const editValid =
    editForm.fullName.trim() !== '' &&
    editForm.email.trim() !== '' &&
    editForm.classId > 0

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-4xl mx-auto">
        <div className="flex items-center gap-3 mb-6">
          <Button variant="ghost" size="icon" onClick={() => navigate('/admin/dashboard')}>
            <ArrowLeft className="w-5 h-5" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold">Users</h1>
            <p className="text-sm text-gray-500">{users?.length ?? 0} accounts</p>
          </div>
          <Button className="ml-auto" onClick={() => setCreateOpen(true)}>
            <Plus className="w-4 h-4 mr-2" /> New User
          </Button>
        </div>

        <Card>
          <CardContent className="p-0">
            {isLoading ? (
              <div className="p-4 space-y-3">
                {Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-14" />)}
              </div>
            ) : (
              users?.map(u => (
                <div key={u.id} className="flex items-center gap-3 px-5 py-4 border-b last:border-b-0">
                  <UserCircle className="w-9 h-9 text-gray-300 shrink-0" />
                  <div className="flex-1 min-w-0">
                    <div className="font-medium">{u.fullName}</div>
                    <div className="text-sm text-gray-500">{u.email}</div>
                  </div>
                  <Badge variant={u.role === 0 ? 'default' : 'secondary'}>
                    {u.role === 0 ? 'Admin' : 'Student'}
                  </Badge>
                  <Badge variant={u.isActive ? 'outline' : 'secondary'}>
                    {u.isActive ? 'Active' : 'Inactive'}
                  </Badge>

                  {u.role === 1 && (
                    <div className="flex gap-1 shrink-0">
                      <Button
                        variant="ghost" size="icon" title="Edit student"
                        onClick={() => openEdit(u)}
                      >
                        <Pencil className="w-4 h-4" />
                      </Button>
                      {u.isActive ? (
                        <Button
                          variant="ghost" size="icon" title="Deactivate"
                          className="text-red-500 hover:text-red-700 hover:bg-red-50"
                          disabled={deactivateMutation.isPending}
                          onClick={() => deactivateMutation.mutate(u.id)}
                        >
                          <UserX className="w-4 h-4" />
                        </Button>
                      ) : (
                        <Button
                          variant="ghost" size="icon" title="Reactivate"
                          className="text-green-600 hover:text-green-800 hover:bg-green-50"
                          disabled={reactivateMutation.isPending}
                          onClick={() => reactivateMutation.mutate(u)}
                        >
                          <UserCheck className="w-4 h-4" />
                        </Button>
                      )}
                    </div>
                  )}
                </div>
              ))
            )}
            {!isLoading && !users?.length && (
              <div className="p-12 text-center text-gray-400">No users found.</div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* ── Create dialog ──────────────────────────────────────────────── */}
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent>
          <DialogHeader><DialogTitle>Create User</DialogTitle></DialogHeader>
          <div className="space-y-3 pt-2">
            <Input
              placeholder="Full name"
              value={createForm.fullName}
              onChange={e => setCreateForm(f => ({ ...f, fullName: e.target.value }))}
            />
            <Input
              type="email" placeholder="Email"
              value={createForm.email}
              onChange={e => setCreateForm(f => ({ ...f, email: e.target.value }))}
            />
            <Input
              type="password" placeholder="Password (min 8 chars)"
              value={createForm.password}
              onChange={e => setCreateForm(f => ({ ...f, password: e.target.value }))}
            />
            <select
              aria-label="Role"
              className="h-9 w-full rounded-md border border-input px-3 text-sm bg-background"
              value={createForm.role}
              onChange={e => setCreateForm(f => ({ ...f, role: +e.target.value, classId: 0 }))}
            >
              <option value={1}>Student</option>
              <option value={0}>Admin</option>
            </select>
            {createForm.role === 1 && (
              <select
                aria-label="Class"
                className="h-9 w-full rounded-md border border-input px-3 text-sm bg-background"
                value={createForm.classId}
                onChange={e => setCreateForm(f => ({ ...f, classId: +e.target.value }))}
              >
                <option value={0}>Select class…</option>
                {classes.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            )}
            <Button
              className="w-full"
              onClick={() => createMutation.mutate()}
              disabled={createMutation.isPending || !createValid}
            >
              {createMutation.isPending ? 'Creating…' : 'Create'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      {/* ── Edit dialog ────────────────────────────────────────────────── */}
      <Dialog open={editTarget !== null} onOpenChange={open => { if (!open) setEditTarget(null) }}>
        <DialogContent className="max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Edit Student — {editTarget?.fullName}</DialogTitle>
          </DialogHeader>
          <div className="space-y-3 pt-2">
            <Input
              placeholder="Full name"
              value={editForm.fullName}
              onChange={e => setEditForm(f => ({ ...f, fullName: e.target.value }))}
            />
            <Input
              type="email" placeholder="Email"
              value={editForm.email}
              onChange={e => setEditForm(f => ({ ...f, email: e.target.value }))}
            />
            <select
              aria-label="Class"
              className="h-9 w-full rounded-md border border-input px-3 text-sm bg-background"
              value={editForm.classId}
              onChange={e => {
                setEditForm(f => ({ ...f, classId: +e.target.value }))
                setEditPermissions([])
              }}
            >
              <option value={0}>Select class…</option>
              {classes.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>

            {/* ── Subject permissions ──────────────────────────────────── */}
            {editForm.classId > 0 && (
              <div className="space-y-2 pt-1">
                <p className="text-sm font-medium text-gray-700">Subject Access</p>
                {editSubjects.length === 0 ? (
                  <p className="text-sm text-gray-400 border rounded-md px-3 py-2">
                    No subjects in this class yet.
                  </p>
                ) : (
                  <div className="border rounded-md divide-y max-h-44 overflow-y-auto">
                    {editSubjects.map(s => (
                      <label
                        key={s.id}
                        className="flex items-center gap-2 px-3 py-2 text-sm cursor-pointer select-none hover:bg-gray-50"
                      >
                        <input
                          type="checkbox"
                          className="h-4 w-4 accent-gray-700"
                          checked={editPermissions.includes(s.id)}
                          onChange={() => togglePermission(s.id)}
                        />
                        {s.name}
                      </label>
                    ))}
                  </div>
                )}
              </div>
            )}

            <label className="flex items-center gap-2 text-sm cursor-pointer select-none pt-1">
              <input
                type="checkbox"
                className="h-4 w-4"
                checked={editForm.isActive}
                onChange={e => setEditForm(f => ({ ...f, isActive: e.target.checked }))}
              />
              Active (uncheck to deactivate)
            </label>
            <Input
              type="password" placeholder="New password — leave blank to keep current"
              value={editForm.newPassword}
              onChange={e => setEditForm(f => ({ ...f, newPassword: e.target.value }))}
            />
            <Button
              className="w-full"
              onClick={() => updateMutation.mutate()}
              disabled={updateMutation.isPending || !editValid}
            >
              {updateMutation.isPending ? 'Saving…' : 'Save Changes'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}
