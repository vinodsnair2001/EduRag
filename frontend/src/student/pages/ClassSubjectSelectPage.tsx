import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import api from '@/lib/api'
import { useAuth } from '@/auth/AuthContext'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { LogOut, Sparkles, BookOpen, ChevronRight, Star } from 'lucide-react'
import type { ClassDto, SubjectDto } from '@/types'

const SUBJECT_EMOJIS: Record<string, string> = {
  math: '🔢', mathematics: '🔢', science: '🔬', physics: '⚛️', chemistry: '🧪',
  biology: '🌿', english: '📖', history: '🏛️', geography: '🌍', computer: '💻',
  art: '🎨', music: '🎵', physical: '⚽', social: '🤝', default: '📚',
}

const GRADE_GRADIENTS = [
  'from-pink-400 to-rose-500',
  'from-orange-400 to-amber-500',
  'from-yellow-400 to-lime-500',
  'from-emerald-400 to-teal-500',
  'from-cyan-400 to-blue-500',
  'from-blue-400 to-indigo-500',
  'from-violet-400 to-purple-500',
  'from-purple-400 to-pink-500',
  'from-rose-400 to-orange-500',
  'from-teal-400 to-emerald-500',
  'from-indigo-400 to-violet-500',
  'from-amber-400 to-yellow-500',
]

function getSubjectEmoji(name: string) {
  const lower = name.toLowerCase()
  return Object.entries(SUBJECT_EMOJIS).find(([k]) => lower.includes(k))?.[1] ?? SUBJECT_EMOJIS.default
}

export default function ClassSubjectSelectPage() {
  const { fullName, logout } = useAuth()
  const navigate = useNavigate()
  const [selectedClass, setSelectedClass] = useState<ClassDto | null>(null)

  const { data: classes, isLoading: classesLoading } = useQuery<ClassDto[]>({
    queryKey: ['student-classes'],
    queryFn: () => api.get('/student/classes').then(r => r.data),
  })

  const { data: subjects, isLoading: subjectsLoading } = useQuery<SubjectDto[]>({
    queryKey: ['student-subjects', selectedClass?.id],
    queryFn: () => api.get(`/student/classes/${selectedClass!.id}/subjects`).then(r => r.data),
    enabled: !!selectedClass,
  })

  const handleSubjectSelect = (subject: SubjectDto) => {
    navigate(`/student/chat/${selectedClass!.id}/${subject.id}`, {
      state: { className: selectedClass!.name, subjectName: subject.name },
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

      <main className="p-4 sm:p-6 max-w-5xl mx-auto">
        {/* Hero */}
        <div className="text-center mb-10 pt-4 animate-fade-in">
          <div className="text-5xl mb-3">🎓</div>
          <h1 className="font-display text-3xl sm:text-4xl font-extrabold text-gray-900 mb-2">
            What are we learning today?
          </h1>
          <p className="text-gray-500 font-display text-lg">Pick your class and subject to start chatting with your AI tutor!</p>
        </div>

        {/* Class Selection */}
        {!selectedClass && (
          <div className="animate-slide-up">
            <h2 className="font-display text-xl font-bold text-gray-700 mb-4 flex items-center gap-2">
              <BookOpen className="w-5 h-5 text-brand-500" /> Choose your class
            </h2>
            {classesLoading ? (
              <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-28 rounded-2xl" />)}
              </div>
            ) : (
              <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                {classes?.map((c) => {
                  const gradient = GRADE_GRADIENTS[(c.grade - 1) % GRADE_GRADIENTS.length]
                  return (
                    <button
                      key={c.id}
                      onClick={() => setSelectedClass(c)}
                      className={`group relative rounded-2xl bg-gradient-to-br ${gradient} p-5 text-white shadow-lg hover:shadow-xl hover:scale-105 active:scale-95 transition-all duration-200 text-left overflow-hidden`}
                    >
                      <div className="absolute inset-0 bg-white/10 opacity-0 group-hover:opacity-100 transition-opacity rounded-2xl" />
                      <div className="text-4xl font-display font-black mb-2 opacity-90">{c.grade}</div>
                      <div className="font-display font-bold text-sm leading-tight">{c.name}</div>
                      <div className="absolute bottom-3 right-3 opacity-70">
                        <Star className="w-4 h-4" />
                      </div>
                    </button>
                  )
                })}
              </div>
            )}
            {!classesLoading && !classes?.length && (
              <div className="text-center py-16 text-gray-400 font-display">
                <div className="text-5xl mb-3">😔</div>
                <p className="text-lg">No classes available yet. Ask your teacher!</p>
              </div>
            )}
          </div>
        )}

        {/* Subject Selection */}
        {selectedClass && (
          <div className="animate-slide-up">
            <button
              onClick={() => setSelectedClass(null)}
              className="flex items-center gap-2 text-brand-600 font-display font-semibold hover:text-brand-700 transition-colors mb-6"
            >
              <ChevronRight className="w-4 h-4 rotate-180" />
              Back to classes
            </button>

            <div className="flex items-center gap-3 mb-6">
              <div className={`w-12 h-12 rounded-xl bg-gradient-to-br ${GRADE_GRADIENTS[(selectedClass.grade - 1) % GRADE_GRADIENTS.length]} flex items-center justify-center text-white font-display font-black text-xl shadow-md`}>
                {selectedClass.grade}
              </div>
              <div>
                <h2 className="font-display text-2xl font-extrabold text-gray-900">{selectedClass.name}</h2>
                <p className="text-sm text-gray-500">Pick a subject to start learning</p>
              </div>
            </div>

            {subjectsLoading ? (
              <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-24 rounded-2xl" />)}
              </div>
            ) : (
              <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {subjects?.map((s) => (
                  <button
                    key={s.id}
                    onClick={() => handleSubjectSelect(s)}
                    className="group flex items-center gap-4 p-5 bg-white rounded-2xl shadow-card hover:shadow-card-hover hover:scale-[1.02] active:scale-[0.98] transition-all duration-200 text-left border border-brand-100 hover:border-brand-300"
                  >
                    <span className="text-4xl group-hover:scale-110 transition-transform duration-200">
                      {getSubjectEmoji(s.name)}
                    </span>
                    <div className="flex-1 min-w-0">
                      <div className="font-display font-bold text-gray-900 truncate">{s.name}</div>
                      {s.description && <div className="text-sm text-gray-500 truncate mt-0.5">{s.description}</div>}
                    </div>
                    <ChevronRight className="w-5 h-5 text-brand-400 group-hover:translate-x-1 transition-transform" />
                  </button>
                ))}
              </div>
            )}
            {!subjectsLoading && !subjects?.length && (
              <div className="text-center py-16 text-gray-400 font-display">
                <div className="text-5xl mb-3">📭</div>
                <p className="text-lg">No subjects yet for this class.</p>
              </div>
            )}
          </div>
        )}
      </main>
    </div>
  )
}
