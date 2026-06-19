import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Skeleton } from '@/components/ui/skeleton'
import { Badge } from '@/components/ui/badge'
import { ArrowLeft, Plus, Pencil, Trash2, ChevronRight } from 'lucide-react'
import toast from 'react-hot-toast'
import type { ClassDto } from '@/types'

export default function ClassListPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<ClassDto | null>(null)
  const [form, setForm] = useState({ name: '', grade: 1 })

  const { data: classes, isLoading } = useQuery<ClassDto[]>({
    queryKey: ['admin-classes'],
    queryFn: () => api.get('/admin/classes').then(r => r.data),
  })

  const save = useMutation({
    mutationFn: () =>
      editing
        ? api.put(`/admin/classes/${editing.id}`, { ...form, isActive: true })
        : api.post('/admin/classes', form),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-classes'] })
      toast.success(editing ? 'Class updated.' : 'Class created.')
      setOpen(false)
    },
    onError: () => toast.error('Failed to save class.'),
  })

  const del = useMutation({
    mutationFn: (id: number) => api.delete(`/admin/classes/${id}`),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-classes'] }); toast.success('Class deleted.') },
    onError:   () => toast.error('Failed to delete class.'),
  })

  const openCreate = () => { setEditing(null); setForm({ name: '', grade: 1 }); setOpen(true) }
  const openEdit   = (c: ClassDto) => { setEditing(c); setForm({ name: c.name, grade: c.grade }); setOpen(true) }

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-4xl mx-auto">
        <div className="flex items-center gap-3 mb-6">
          <Button variant="ghost" size="icon" onClick={() => navigate('/admin/dashboard')}>
            <ArrowLeft className="w-5 h-5" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Classes</h1>
            <p className="text-sm text-gray-500">{classes?.length ?? 0} classes total</p>
          </div>
          <Button className="ml-auto" onClick={openCreate}>
            <Plus className="w-4 h-4 mr-2" /> New Class
          </Button>
        </div>

        <div className="grid gap-3">
          {isLoading
            ? Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-16 rounded-xl" />)
            : classes?.map((c) => (
              <Card key={c.id} className="hover:shadow-md transition-shadow cursor-pointer" onClick={() => navigate(`/admin/classes/${c.id}`)}>
                <CardContent className="p-4 flex items-center gap-4">
                  <div className="w-10 h-10 rounded-lg bg-brand-100 flex items-center justify-center font-bold font-display text-brand-600">
                    {c.grade}
                  </div>
                  <div className="flex-1">
                    <div className="font-semibold text-gray-900">{c.name}</div>
                    <div className="text-sm text-gray-500">Grade {c.grade}</div>
                  </div>
                  <Badge variant={c.isActive ? 'success' : 'secondary'}>{c.isActive ? 'Active' : 'Inactive'}</Badge>
                  <div className="flex gap-1" onClick={(e) => e.stopPropagation()}>
                    <Button variant="ghost" size="icon" onClick={() => openEdit(c)}>
                      <Pencil className="w-4 h-4" />
                    </Button>
                    <Button variant="ghost" size="icon" className="text-destructive" onClick={() => del.mutate(c.id)}>
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  </div>
                  <ChevronRight className="w-4 h-4 text-gray-400" />
                </CardContent>
              </Card>
            ))}
          {!isLoading && !classes?.length && (
            <Card className="p-12 text-center text-gray-400">
              <p className="text-lg">No classes yet. Create your first class!</p>
            </Card>
          )}
        </div>
      </div>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editing ? 'Edit Class' : 'New Class'}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 pt-2">
            <Input placeholder="Class name (e.g. Class 6)" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} />
            <Input type="number" placeholder="Grade (1-12)" min={1} max={12} value={form.grade} onChange={e => setForm(f => ({ ...f, grade: +e.target.value }))} />
            <Button className="w-full" onClick={() => save.mutate()} disabled={save.isPending || !form.name}>
              {save.isPending ? 'Saving…' : 'Save'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}
