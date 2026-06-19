import { useState, useCallback } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useDropzone } from 'react-dropzone'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Progress } from '@/components/ui/progress'
import { Skeleton } from '@/components/ui/skeleton'
import StatusBadge from '@/shared/components/StatusBadge'
import { ArrowLeft, UploadCloud, Trash2, FileText, RefreshCw } from 'lucide-react'
import toast from 'react-hot-toast'
import type { MaterialDto, ClassDto, SubjectDto, ChapterDto } from '@/types'

export default function MaterialListPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const qc = useQueryClient()
  const [uploadProgress, setUploadProgress] = useState(0)
  const [uploading, setUploading] = useState(false)
  const [selectedClass, setSelectedClass] = useState(searchParams.get('classId') ?? '')
  const [selectedSubject, setSelectedSubject] = useState(searchParams.get('subjectId') ?? '')
  const [selectedChapter, setSelectedChapter] = useState('')

  const { data: classes }   = useQuery<ClassDto[]>({ queryKey: ['admin-classes'], queryFn: () => api.get('/admin/classes').then(r => r.data) })
  const { data: subjects }  = useQuery<SubjectDto[]>({ queryKey: ['admin-subjects', selectedClass], queryFn: () => api.get(`/admin/classes/${selectedClass}/subjects`).then(r => r.data), enabled: !!selectedClass })
  const { data: chapters }  = useQuery<ChapterDto[]>({ queryKey: ['admin-chapters', selectedSubject], queryFn: () => api.get(`/admin/subjects/${selectedSubject}/chapters`).then(r => r.data), enabled: !!selectedSubject })
  const { data: materials, isLoading, refetch } = useQuery<MaterialDto[]>({ queryKey: ['admin-materials'], queryFn: () => api.get('/admin/materials').then(r => r.data), refetchInterval: 5000 })

  const del = useMutation({
    mutationFn: (id: string) => api.delete(`/admin/materials/${id}`),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-materials'] }); toast.success('Deleted.') },
  })

  const onDrop = useCallback(async (files: File[]) => {
    const file = files[0]
    if (!file) return
    if (!selectedClass || !selectedSubject) { toast.error('Select a class and subject first.'); return }

    const fd = new FormData()
    fd.append('file', file)
    fd.append('classId', selectedClass)
    fd.append('subjectId', selectedSubject)
    if (selectedChapter) fd.append('chapterId', selectedChapter)

    setUploading(true)
    setUploadProgress(0)
    try {
      await api.post('/admin/upload', fd, {
        headers: { 'Content-Type': 'multipart/form-data' },
        onUploadProgress: e => setUploadProgress(Math.round((e.loaded * 100) / (e.total ?? 1))),
      })
      toast.success('Upload successful! Vectorization started.')
      qc.invalidateQueries({ queryKey: ['admin-materials'] })
    } catch {
      toast.error('Upload failed.')
    } finally {
      setUploading(false)
      setUploadProgress(0)
    }
  }, [selectedClass, selectedSubject, selectedChapter, qc])

  const { getRootProps, getInputProps, isDragActive } = useDropzone({ onDrop, accept: { 'application/pdf': ['.pdf'] }, multiple: false })

  const formatSize = (bytes: number) => bytes < 1024 * 1024 ? `${(bytes / 1024).toFixed(0)} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-5xl mx-auto">
        <div className="flex items-center gap-3 mb-6">
          <Button variant="ghost" size="icon" onClick={() => navigate('/admin/dashboard')}><ArrowLeft className="w-5 h-5" /></Button>
          <div><h1 className="text-2xl font-bold">Study Materials</h1><p className="text-sm text-gray-500">{materials?.length ?? 0} files</p></div>
          <Button variant="outline" size="icon" className="ml-auto" onClick={() => refetch()}><RefreshCw className="w-4 h-4" /></Button>
        </div>

        {/* Upload Zone */}
        <Card className="mb-6">
          <CardHeader><CardTitle className="text-base">Upload PDF</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-3 gap-3">
              <select
                className="h-9 rounded-md border border-input px-3 text-sm bg-background"
                value={selectedClass}
                onChange={e => { setSelectedClass(e.target.value); setSelectedSubject(''); setSelectedChapter('') }}
              >
                <option value="">Select Class…</option>
                {classes?.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
              <select
                className="h-9 rounded-md border border-input px-3 text-sm bg-background"
                value={selectedSubject}
                onChange={e => { setSelectedSubject(e.target.value); setSelectedChapter('') }}
                disabled={!selectedClass}
              >
                <option value="">Select Subject…</option>
                {subjects?.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
              <select
                className="h-9 rounded-md border border-input px-3 text-sm bg-background"
                value={selectedChapter}
                onChange={e => setSelectedChapter(e.target.value)}
                disabled={!selectedSubject}
              >
                <option value="">No Chapter (subject-level)</option>
                {chapters?.map(ch => <option key={ch.id} value={ch.id}>{ch.title}</option>)}
              </select>
            </div>
            {selectedSubject && !selectedChapter && (
              <p className="text-xs text-amber-600 bg-amber-50 border border-amber-200 rounded px-3 py-1.5">
                No chapter selected — this PDF will be uploaded at the subject level and won't appear in chapter-scoped student sessions.
              </p>
            )}
            <div
              {...getRootProps()}
              className={`border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-colors ${isDragActive ? 'border-brand-500 bg-brand-50' : 'border-gray-200 hover:border-brand-300 hover:bg-brand-50/50'}`}
            >
              <input {...getInputProps()} />
              <UploadCloud className={`w-10 h-10 mx-auto mb-3 ${isDragActive ? 'text-brand-500' : 'text-gray-400'}`} />
              <p className="font-medium text-gray-700">{isDragActive ? 'Drop it here!' : 'Drag & drop a PDF, or click to browse'}</p>
              <p className="text-xs text-gray-400 mt-1">PDF only · Max 50 MB</p>
            </div>
            {uploading && <Progress value={uploadProgress} className="h-2" />}
          </CardContent>
        </Card>

        {/* Materials Table */}
        <Card>
          <CardContent className="p-0">
            {isLoading
              ? <div className="p-4 space-y-3">{Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-12" />)}</div>
              : materials?.length === 0
                ? <div className="p-12 text-center text-gray-400">No materials uploaded yet.</div>
                : (
                  <div className="divide-y">
                    {materials?.map(m => (
                      <div key={m.id} className="flex items-center gap-4 px-5 py-3">
                        <FileText className="w-5 h-5 text-gray-400 shrink-0" />
                        <div className="flex-1 min-w-0">
                          <div className="text-sm font-medium truncate">{m.originalFileName}</div>
                          <div className="text-xs text-gray-400">
                            {formatSize(m.fileSizeBytes)} · {new Date(m.uploadedAt).toLocaleDateString()}
                            {m.chapterId ? <span className="ml-2 text-violet-500">· Chapter assigned</span> : <span className="ml-2 text-gray-300">· Subject-level</span>}
                          </div>
                        </div>
                        <StatusBadge status={m.status} />
                        <Button variant="ghost" size="icon" className="text-destructive shrink-0" onClick={() => del.mutate(m.id)}>
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      </div>
                    ))}
                  </div>
                )
            }
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
