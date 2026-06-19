import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Skeleton } from '@/components/ui/skeleton'
import { ArrowLeft, Plus, Pencil, Trash2, BookOpen, List, UploadCloud, FileText, Download } from 'lucide-react'
import toast from 'react-hot-toast'
import type { SubjectDto, ChapterDto } from '@/types'

export default function ClassDetailPage() {
  const { id } = useParams<{ id: string }>()
  const classId = Number(id)
  const navigate = useNavigate()
  const qc = useQueryClient()

  const [subjectDialog, setSubjectDialog] = useState(false)
  const [chapterDialog, setChapterDialog] = useState(false)
  const [editingSubject, setEditingSubject] = useState<SubjectDto | null>(null)
  const [editingChapter, setEditingChapter] = useState<ChapterDto | null>(null)
  const [activeSubjectId, setActiveSubjectId] = useState<number | null>(null)
  const [subjectForm, setSubjectForm] = useState({ name: '', description: '' })
  const [chapterForm, setChapterForm] = useState({ title: '', orderIndex: 0 })
  const [pdfChapter, setPdfChapter] = useState<ChapterDto | null>(null)
  const [pdfUrl, setPdfUrl] = useState<string | null>(null)
  const [pdfLoading, setPdfLoading] = useState(false)

  useEffect(() => {
    if (!pdfChapter) {
      if (pdfUrl) { URL.revokeObjectURL(pdfUrl); setPdfUrl(null) }
      return
    }
    let cancelled = false
    setPdfLoading(true)
    api.get(`/admin/chapters/${pdfChapter.id}/pdf`, { responseType: 'blob' })
      .then(r => { if (!cancelled) setPdfUrl(URL.createObjectURL(r.data)) })
      .catch(() => { if (!cancelled) { toast.error('Could not load PDF.'); setPdfChapter(null) } })
      .finally(() => { if (!cancelled) setPdfLoading(false) })
    return () => { cancelled = true }
  }, [pdfChapter])

  const { data: subjects, isLoading: subjectsLoading } = useQuery<SubjectDto[]>({
    queryKey: ['admin-subjects', classId],
    queryFn: () => api.get(`/admin/classes/${classId}/subjects`).then(r => r.data),
  })

  const { data: chapters } = useQuery<ChapterDto[]>({
    queryKey: ['admin-chapters', activeSubjectId],
    queryFn: () => api.get(`/admin/subjects/${activeSubjectId}/chapters`).then(r => r.data),
    enabled: !!activeSubjectId,
  })

  const saveSubject = useMutation({
    mutationFn: () => editingSubject
      ? api.put(`/admin/subjects/${editingSubject.id}`, { ...subjectForm, isActive: true })
      : api.post(`/admin/classes/${classId}/subjects`, subjectForm),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-subjects', classId] }); toast.success('Subject saved.'); setSubjectDialog(false) },
  })

  const deleteSubject = useMutation({
    mutationFn: (id: number) => api.delete(`/admin/subjects/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin-subjects', classId] }),
  })

  const saveChapter = useMutation({
    mutationFn: () => editingChapter
      ? api.put(`/admin/chapters/${editingChapter.id}`, { ...chapterForm, isActive: true })
      : api.post(`/admin/subjects/${activeSubjectId}/chapters`, chapterForm),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-chapters', activeSubjectId] }); toast.success('Chapter saved.'); setChapterDialog(false) },
  })

  const deleteChapter = useMutation({
    mutationFn: (id: number) => api.delete(`/admin/chapters/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin-chapters', activeSubjectId] }),
  })

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-5xl mx-auto">
        <div className="flex items-center gap-3 mb-6">
          <Button variant="ghost" size="icon" onClick={() => navigate('/admin/classes')}><ArrowLeft className="w-5 h-5" /></Button>
          <h1 className="text-2xl font-bold text-gray-900">Class Detail</h1>
          <Button className="ml-auto" size="sm" onClick={() => { setEditingSubject(null); setSubjectForm({ name: '', description: '' }); setSubjectDialog(true) }}>
            <Plus className="w-4 h-4 mr-1" /> Add Subject
          </Button>
        </div>

        <div className="grid md:grid-cols-2 gap-6">
          {/* Subjects */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-base"><BookOpen className="w-4 h-4" /> Subjects</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              {subjectsLoading ? <Skeleton className="h-10" /> : subjects?.map(s => (
                <div key={s.id}
                  className={`flex items-center gap-3 p-3 rounded-lg cursor-pointer border transition-colors ${activeSubjectId === s.id ? 'bg-brand-50 border-brand-200' : 'bg-white hover:bg-gray-50 border-transparent'}`}
                  onClick={() => setActiveSubjectId(s.id)}>
                  <div className="flex-1 min-w-0">
                    <div className="font-medium text-sm truncate">{s.name}</div>
                    {s.description && <div className="text-xs text-gray-400 truncate">{s.description}</div>}
                  </div>
                  <Button variant="ghost" size="icon" className="h-7 w-7 shrink-0" onClick={e => { e.stopPropagation(); navigate(`/admin/materials?classId=${classId}&subjectId=${s.id}`) }}>
                    <UploadCloud className="w-3.5 h-3.5" />
                  </Button>
                  <Button variant="ghost" size="icon" className="h-7 w-7 shrink-0" onClick={e => { e.stopPropagation(); setEditingSubject(s); setSubjectForm({ name: s.name, description: s.description }); setSubjectDialog(true) }}>
                    <Pencil className="w-3.5 h-3.5" />
                  </Button>
                  <Button variant="ghost" size="icon" className="h-7 w-7 shrink-0 text-destructive" onClick={e => { e.stopPropagation(); deleteSubject.mutate(s.id) }}>
                    <Trash2 className="w-3.5 h-3.5" />
                  </Button>
                </div>
              ))}
              {!subjects?.length && !subjectsLoading && <p className="text-sm text-gray-400 text-center py-4">No subjects yet</p>}
            </CardContent>
          </Card>

          {/* Chapters */}
          <Card>
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <CardTitle className="flex items-center gap-2 text-base"><List className="w-4 h-4" /> Chapters</CardTitle>
                {activeSubjectId && (
                  <Button size="sm" variant="outline" onClick={() => { setEditingChapter(null); setChapterForm({ title: '', orderIndex: 0 }); setChapterDialog(true) }}>
                    <Plus className="w-3.5 h-3.5 mr-1" /> Add
                  </Button>
                )}
              </div>
            </CardHeader>
            <CardContent className="space-y-2">
              {!activeSubjectId && <p className="text-sm text-gray-400 text-center py-4">Select a subject to view chapters</p>}
              {chapters?.map(c => (
                <div key={c.id} className="flex items-center gap-3 p-3 rounded-lg bg-white border">
                  <div className="w-6 h-6 rounded-full bg-brand-100 flex items-center justify-center text-xs font-bold text-brand-600">{c.orderIndex + 1}</div>
                  <div className="flex-1 text-sm font-medium truncate">{c.title}</div>
                  {c.hasPdf && (
                    <Button
                      variant="ghost" size="icon"
                      className="h-7 w-7 shrink-0 text-blue-600 hover:text-blue-700 hover:bg-blue-50"
                      title="View PDF"
                      onClick={() => setPdfChapter(c)}>
                      <FileText className="w-3.5 h-3.5" />
                    </Button>
                  )}
                  <Button variant="ghost" size="icon" className="h-7 w-7 shrink-0" onClick={() => { setEditingChapter(c); setChapterForm({ title: c.title, orderIndex: c.orderIndex }); setChapterDialog(true) }}>
                    <Pencil className="w-3.5 h-3.5" />
                  </Button>
                  <Button variant="ghost" size="icon" className="h-7 w-7 shrink-0 text-destructive" onClick={() => deleteChapter.mutate(c.id)}>
                    <Trash2 className="w-3.5 h-3.5" />
                  </Button>
                </div>
              ))}
              {activeSubjectId && !chapters?.length && <p className="text-sm text-gray-400 text-center py-4">No chapters yet</p>}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Subject Dialog */}
      <Dialog open={subjectDialog} onOpenChange={setSubjectDialog}>
        <DialogContent>
          <DialogHeader><DialogTitle>{editingSubject ? 'Edit Subject' : 'New Subject'}</DialogTitle></DialogHeader>
          <div className="space-y-3 pt-2">
            <Input placeholder="Subject name" value={subjectForm.name} onChange={e => setSubjectForm(f => ({ ...f, name: e.target.value }))} />
            <Input placeholder="Description (optional)" value={subjectForm.description} onChange={e => setSubjectForm(f => ({ ...f, description: e.target.value }))} />
            <Button className="w-full" onClick={() => saveSubject.mutate()} disabled={!subjectForm.name}>Save</Button>
          </div>
        </DialogContent>
      </Dialog>

      {/* Chapter Dialog */}
      <Dialog open={chapterDialog} onOpenChange={setChapterDialog}>
        <DialogContent>
          <DialogHeader><DialogTitle>{editingChapter ? 'Edit Chapter' : 'New Chapter'}</DialogTitle></DialogHeader>
          <div className="space-y-3 pt-2">
            <Input placeholder="Chapter title" value={chapterForm.title} onChange={e => setChapterForm(f => ({ ...f, title: e.target.value }))} />
            <Input type="number" placeholder="Order index" min={0} value={chapterForm.orderIndex} onChange={e => setChapterForm(f => ({ ...f, orderIndex: +e.target.value }))} />
            <Button className="w-full" onClick={() => saveChapter.mutate()} disabled={!chapterForm.title}>Save</Button>
          </div>
        </DialogContent>
      </Dialog>

      {/* PDF Viewer Dialog */}
      <Dialog open={!!pdfChapter} onOpenChange={open => { if (!open) setPdfChapter(null) }}>
        <DialogContent className="max-w-5xl w-full p-0 gap-0 overflow-hidden h-[90vh]">
          <div className="flex items-center justify-between px-5 py-3 border-b bg-gray-50 shrink-0">
            <div className="flex items-center gap-2 min-w-0">
              <FileText className="w-4 h-4 text-blue-600 shrink-0" />
              <span className="font-semibold text-sm text-gray-800 truncate">{pdfChapter?.title}</span>
            </div>
            {pdfUrl && (
              <a href={pdfUrl} download={`${pdfChapter?.title}.pdf`} className="mr-8">
                <Button variant="outline" size="sm" className="h-7 text-xs gap-1">
                  <Download className="w-3 h-3" /> Download
                </Button>
              </a>
            )}
          </div>
          <div className="flex-1 bg-gray-100 h-[calc(90vh-53px)]">
            {pdfLoading && (
              <div className="flex items-center justify-center h-full gap-3">
                <div className="w-6 h-6 border-2 border-blue-500 border-t-transparent rounded-full animate-spin" />
                <span className="text-sm text-gray-500">Loading PDF…</span>
              </div>
            )}
            {pdfUrl && !pdfLoading && (
              <iframe
                src={pdfUrl}
                className="w-full h-full border-0"
                title={pdfChapter?.title ?? 'Chapter PDF'}
              />
            )}
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}
