import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import api from '@/lib/api'
import { useAuth } from '@/auth/AuthContext'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { LogOut, Sparkles, BookOpen, ChevronRight, ArrowLeft, CheckSquare, Square, Check } from 'lucide-react'
import toast from 'react-hot-toast'
import type { ClassDto, SubjectDto, ChapterDto } from '@/types'

const SUBJECT_EMOJIS: Record<string, string> = {
  math: '🔢', mathematics: '🔢', science: '🔬', physics: '⚛️', chemistry: '🧪',
  biology: '🌿', english: '📖', history: '🏛️', geography: '🌍', computer: '💻',
  art: '🎨', music: '🎵', physical: '⚽', social: '🤝', default: '📚',
}

const GRADE_GRADIENTS = [
  'from-pink-400 to-rose-500', 'from-orange-400 to-amber-500',
  'from-yellow-400 to-lime-500', 'from-emerald-400 to-teal-500',
  'from-cyan-400 to-blue-500', 'from-blue-400 to-indigo-500',
  'from-violet-400 to-purple-500', 'from-purple-400 to-pink-500',
  'from-rose-400 to-orange-500', 'from-teal-400 to-emerald-500',
  'from-indigo-400 to-violet-500', 'from-amber-400 to-yellow-500',
]

function getSubjectEmoji(name: string) {
  const lower = name.toLowerCase()
  return Object.entries(SUBJECT_EMOJIS).find(([k]) => lower.includes(k))?.[1] ?? SUBJECT_EMOJIS.default
}

type Step = 'class' | 'subject' | 'chapter'

export default function ClassSubjectSelectPage() {
  const { fullName, logout } = useAuth()
  const navigate = useNavigate()

  const [step, setStep] = useState<Step>('class')
  const [selectedClass, setSelectedClass] = useState<ClassDto | null>(null)
  const [selectedSubject, setSelectedSubject] = useState<SubjectDto | null>(null)
  const [selectedChapterIds, setSelectedChapterIds] = useState<number[]>([])

  const { data: classes, isLoading: classesLoading } = useQuery<ClassDto[]>({
    queryKey: ['student-classes'],
    queryFn: () => api.get('/student/classes').then(r => r.data),
  })

  const { data: subjects, isLoading: subjectsLoading } = useQuery<SubjectDto[]>({
    queryKey: ['student-subjects', selectedClass?.id],
    queryFn: () => api.get(`/student/classes/${selectedClass!.id}/subjects`).then(r => r.data),
    enabled: !!selectedClass,
  })

  const { data: chapters, isLoading: chaptersLoading } = useQuery<ChapterDto[]>({
    queryKey: ['student-chapters', selectedSubject?.id],
    queryFn: () => api.get(`/student/subjects/${selectedSubject!.id}/chapters`).then(r => r.data),
    enabled: !!selectedSubject,
  })

  const gradient = selectedClass
    ? GRADE_GRADIENTS[(selectedClass.grade - 1) % GRADE_GRADIENTS.length]
    : 'from-violet-400 to-purple-500'

  const handleClassSelect = (cls: ClassDto) => {
    setSelectedClass(cls)
    setSelectedSubject(null)
    setSelectedChapterIds([])
    setStep('subject')
  }

  const handleSubjectSelect = (subject: SubjectDto) => {
    setSelectedSubject(subject)
    setSelectedChapterIds([])
    setStep('chapter')
  }

  const toggleChapter = (id: number) => {
    setSelectedChapterIds(prev =>
      prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]
    )
  }

  const allSelected = !!chapters?.length && selectedChapterIds.length === chapters.length

  const toggleAll = () => {
    if (allSelected) {
      setSelectedChapterIds([])
    } else {
      setSelectedChapterIds(chapters?.map(c => c.id) ?? [])
    }
  }

  const handleStart = () => {
    if (!selectedClass || !selectedSubject) return
    if (chapters?.length && selectedChapterIds.length === 0) {
      toast.error('Please select at least one chapter.')
      return
    }
    navigate(`/student/chat/${selectedClass.id}/${selectedSubject.id}`, {
      state: {
        className: selectedClass.name,
        subjectName: selectedSubject.name,
        chapterIds: selectedChapterIds,
        chapterTitles: chapters?.filter(c => selectedChapterIds.includes(c.id)).map(c => c.title) ?? [],
      },
    })
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-brand-50 via-purple-50 to-indigo-100">
      {/* Header */}
      <header className="bg-white/80 backdrop-blur-sm border-b border-white/50 px-4 sm:px-6 py-4 flex items-center justify-between sticky top-0 z-10">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 bg-brand-500 rounded-xl flex items-center justify-center shadow-sm">
            <Sparkles className="w-5 h-5 text-white" />
          </div>
          <div>
            <span className="font-display font-extrabold text-brand-700 text-lg">EduRAG</span>
            <p className="text-xs text-gray-500 leading-none mt-0.5">Hi, {fullName?.split(' ')[0]}! 👋</p>
          </div>
        </div>
        <Button variant="ghost" size="sm" className="text-gray-500" onClick={() => { logout(); navigate('/login') }}>
          <LogOut className="w-4 h-4 mr-1" /> Sign out
        </Button>
      </header>

      <main className="p-4 sm:p-6 max-w-4xl mx-auto">

        {/* Step indicator */}
        <div className="flex items-center gap-2 mb-6 pt-2">
          {(['class', 'subject', 'chapter'] as Step[]).map((s, i) => {
            const stepOrder = ['class', 'subject', 'chapter']
            const currentIdx = stepOrder.indexOf(step)
            const thisIdx = i
            return (
              <div key={s} className="flex items-center gap-2">
                <div className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold transition-all ${
                  step === s ? 'bg-brand-500 text-white shadow-md' :
                  currentIdx > thisIdx ? 'bg-brand-200 text-brand-700' : 'bg-gray-200 text-gray-400'
                }`}>
                  {currentIdx > thisIdx ? <Check className="w-3.5 h-3.5" /> : i + 1}
                </div>
                <span className={`text-sm font-display font-semibold hidden sm:block ${step === s ? 'text-brand-700' : 'text-gray-400'}`}>
                  {s === 'class' ? 'Class' : s === 'subject' ? 'Subject' : 'Chapters'}
                </span>
                {i < 2 && <div className="w-8 h-0.5 bg-gray-200 mx-1" />}
              </div>
            )
          })}
        </div>

        {/* ── Step 1: Class ───────────────────────────────── */}
        {step === 'class' && (
          <div className="animate-fade-in">
            <div className="text-center mb-8 pt-2">
              <div className="text-5xl mb-3">🎓</div>
              <h1 className="font-display text-3xl sm:text-4xl font-extrabold text-gray-900 mb-2">
                Select your class
              </h1>
              <p className="text-gray-500 font-display text-lg">Which class are you in?</p>
            </div>
            {classesLoading ? (
              <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-24 rounded-2xl" />)}
              </div>
            ) : classes?.length ? (
              <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {classes.map(cls => {
                  const g = GRADE_GRADIENTS[(cls.grade - 1) % GRADE_GRADIENTS.length]
                  return (
                    <button
                      key={cls.id}
                      type="button"
                      onClick={() => handleClassSelect(cls)}
                      className={`group flex items-center gap-4 p-5 bg-gradient-to-br ${g} rounded-2xl shadow-card hover:shadow-card-hover hover:scale-[1.02] active:scale-[0.98] transition-all duration-200 text-white text-left`}
                    >
                      <div className="text-4xl font-display font-black opacity-90">{cls.grade}</div>
                      <div className="flex-1 min-w-0">
                        <div className="font-display font-bold text-lg leading-tight">{cls.name}</div>
                        <div className="text-sm opacity-80">Grade {cls.grade}</div>
                      </div>
                      <ChevronRight className="w-5 h-5 opacity-70 group-hover:translate-x-1 transition-transform" />
                    </button>
                  )
                })}
              </div>
            ) : (
              <div className="text-center py-16 text-gray-400 font-display bg-white rounded-2xl shadow-sm">
                <div className="text-5xl mb-3">📭</div>
                <p className="text-lg">No classes available yet. Ask your teacher!</p>
              </div>
            )}
          </div>
        )}

        {/* ── Step 2: Subject ─────────────────────────────── */}
        {step === 'subject' && selectedClass && (
          <div className="animate-fade-in">
            <button
              type="button"
              onClick={() => setStep('class')}
              className="flex items-center gap-1 text-brand-600 font-display font-semibold text-sm mb-4 hover:underline"
            >
              <ArrowLeft className="w-4 h-4" /> Back to classes
            </button>

            <div className={`flex items-center gap-4 rounded-2xl bg-gradient-to-br ${gradient} p-5 text-white shadow-lg mb-8`}>
              <div className="text-4xl font-display font-black opacity-90">{selectedClass.grade}</div>
              <div>
                <div className="font-display font-extrabold text-lg leading-tight">{selectedClass.name}</div>
                <div className="text-sm opacity-80">Your class</div>
              </div>
            </div>

            <h2 className="font-display text-xl font-bold text-gray-700 mb-4 flex items-center gap-2">
              <BookOpen className="w-5 h-5 text-brand-500" /> Choose a subject
            </h2>
            {subjectsLoading ? (
              <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-24 rounded-2xl" />)}
              </div>
            ) : subjects?.length ? (
              <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {subjects.map(s => (
                  <button
                    key={s.id}
                    type="button"
                    onClick={() => handleSubjectSelect(s)}
                    className="group flex items-center gap-4 p-5 bg-white rounded-2xl shadow-card hover:shadow-card-hover hover:scale-[1.02] active:scale-[0.98] transition-all duration-200 text-left border border-brand-100 hover:border-brand-300"
                  >
                    <span className="text-4xl group-hover:scale-110 transition-transform duration-200">
                      {getSubjectEmoji(s.name)}
                    </span>
                    <div className="flex-1 min-w-0">
                      <div className="font-display font-bold text-gray-900 truncate">{s.name}</div>
                      {s.description && (
                        <div className="text-sm text-gray-500 truncate mt-0.5">{s.description}</div>
                      )}
                    </div>
                    <ChevronRight className="w-5 h-5 text-brand-400 group-hover:translate-x-1 transition-transform" />
                  </button>
                ))}
              </div>
            ) : (
              <div className="text-center py-16 text-gray-400 font-display bg-white rounded-2xl shadow-sm">
                <div className="text-5xl mb-3">📭</div>
                <p className="text-lg">No subjects in this class yet.</p>
              </div>
            )}
          </div>
        )}

        {/* ── Step 3: Chapters ────────────────────────────── */}
        {step === 'chapter' && selectedSubject && (
          <div className="animate-fade-in">
            <button
              type="button"
              onClick={() => setStep('subject')}
              className="flex items-center gap-1 text-brand-600 font-display font-semibold text-sm mb-4 hover:underline"
            >
              <ArrowLeft className="w-4 h-4" /> Back to subjects
            </button>

            <div className="flex items-center gap-3 mb-6 p-4 bg-white rounded-2xl shadow-sm border border-brand-100">
              <span className="text-3xl">{getSubjectEmoji(selectedSubject.name)}</span>
              <div>
                <div className="font-display font-bold text-gray-900">{selectedSubject.name}</div>
                <div className="text-sm text-gray-500">{selectedClass?.name}</div>
              </div>
            </div>

            <h2 className="font-display text-xl font-bold text-gray-700 mb-1">
              📖 Select chapters to study
            </h2>
            <p className="text-sm text-gray-500 font-display mb-4">
              Your AI tutor will answer only from the selected chapters. Pick as many as you like!
            </p>

            {chaptersLoading ? (
              <div className="space-y-3">
                {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-14 rounded-2xl" />)}
              </div>
            ) : chapters?.length ? (
              <>
                {/* Select All toggle */}
                <button
                  type="button"
                  onClick={toggleAll}
                  className="flex items-center gap-3 w-full p-4 mb-3 bg-brand-50 border-2 border-brand-200 rounded-2xl hover:bg-brand-100 transition-colors text-left"
                >
                  {allSelected
                    ? <CheckSquare className="w-5 h-5 text-brand-600 shrink-0" />
                    : <Square className="w-5 h-5 text-brand-400 shrink-0" />
                  }
                  <span className="font-display font-bold text-brand-700">
                    {allSelected ? 'Deselect all chapters' : 'Select all chapters'}
                  </span>
                  <span className="ml-auto text-xs text-brand-500 font-display">
                    {selectedChapterIds.length}/{chapters.length} selected
                  </span>
                </button>

                {/* Chapter checkboxes */}
                <div className="space-y-2 mb-6">
                  {chapters.map((ch, idx) => {
                    const isSelected = selectedChapterIds.includes(ch.id)
                    return (
                      <button
                        key={ch.id}
                        type="button"
                        onClick={() => toggleChapter(ch.id)}
                        className={`flex items-center gap-3 w-full p-4 rounded-2xl border-2 transition-all text-left ${
                          isSelected
                            ? 'bg-brand-50 border-brand-400 shadow-sm'
                            : 'bg-white border-gray-200 hover:border-brand-200 hover:bg-brand-50/40'
                        }`}
                      >
                        <div className={`w-7 h-7 rounded-lg flex items-center justify-center shrink-0 font-bold text-sm transition-colors ${
                          isSelected ? 'bg-brand-500 text-white' : 'bg-gray-100 text-gray-500'
                        }`}>
                          {isSelected ? <Check className="w-4 h-4" /> : idx + 1}
                        </div>
                        <span className={`font-display font-semibold leading-snug ${isSelected ? 'text-brand-800' : 'text-gray-700'}`}>
                          {ch.title}
                        </span>
                      </button>
                    )
                  })}
                </div>

                {/* Sticky start button */}
                <div className="sticky bottom-4">
                  <Button
                    className="w-full h-14 rounded-2xl text-base font-display font-bold bg-gradient-to-br from-brand-500 to-purple-600 hover:from-brand-600 hover:to-purple-700 shadow-lg transition-all hover:-translate-y-0.5 disabled:opacity-50 disabled:pointer-events-none"
                    onClick={handleStart}
                    disabled={selectedChapterIds.length === 0}
                  >
                    {selectedChapterIds.length === 0
                      ? 'Select at least one chapter ☝️'
                      : `Start studying ${selectedChapterIds.length} chapter${selectedChapterIds.length > 1 ? 's' : ''} 🚀`
                    }
                  </Button>
                </div>
              </>
            ) : (
              /* No chapters defined — allow direct subject-level chat */
              <div className="text-center py-10 bg-white rounded-2xl shadow-sm border border-gray-100">
                <div className="text-5xl mb-3">📄</div>
                <p className="font-display font-bold text-gray-700 text-lg mb-1">No chapters defined</p>
                <p className="text-sm text-gray-400 mb-6">
                  This subject has no chapters yet — you can still chat using all uploaded materials.
                </p>
                <Button
                  className="rounded-2xl font-display font-bold bg-gradient-to-br from-brand-500 to-purple-600 hover:from-brand-600 hover:to-purple-700"
                  onClick={handleStart}
                >
                  Start without chapter filter 🚀
                </Button>
              </div>
            )}
          </div>
        )}
      </main>
    </div>
  )
}
