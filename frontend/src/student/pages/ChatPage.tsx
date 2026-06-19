import { useState, useEffect, useRef, useCallback } from 'react'
import { useParams, useLocation, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import ReactMarkdown from 'react-markdown'
import api from '@/lib/api'
import { useAuth } from '@/auth/AuthContext'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Skeleton } from '@/components/ui/skeleton'
import TypingIndicator from '../components/TypingIndicator'
import { ArrowLeft, Send, Zap, BookOpenCheck, Sparkles } from 'lucide-react'
import toast from 'react-hot-toast'
import type { ChatMessageDto } from '@/types'

interface Message {
  id: string
  role: 'user' | 'assistant'
  content: string
  streaming?: boolean
  thinking?: boolean
}

export default function ChatPage() {
  const { classId, subjectId } = useParams<{ classId: string; subjectId: string }>()
  const location  = useLocation()
  const navigate  = useNavigate()
  const { token } = useAuth()

  type LocationState = { className?: string; subjectName?: string; chapterIds?: number[]; chapterTitles?: string[] }
  const state         = (location.state as LocationState) ?? {}
  const className     = state.className   ?? `Class ${classId}`
  const subjectName   = state.subjectName ?? 'Subject'
  const chapterIds    = state.chapterIds  ?? []
  const chapterTitles = state.chapterTitles ?? []

  const [sessionId, setSessionId] = useState<string | null>(null)
  const [messages, setMessages]   = useState<Message[]>([])
  const [input, setInput]         = useState('')
  const [streaming, setStreaming]  = useState(false)
  const messagesEndRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    api.post('/chat/sessions', { classId: Number(classId), subjectId: Number(subjectId), chapterIds })
      .then(r => setSessionId(r.data.sessionId))
      .catch(() => toast.error('Failed to start chat session.'))
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [classId, subjectId])

  const { data: history, isLoading: historyLoading } = useQuery<ChatMessageDto[]>({
    queryKey: ['chat-messages', sessionId],
    queryFn: () => api.get(`/chat/sessions/${sessionId}/messages`).then(r => r.data),
    enabled: !!sessionId,
  })

  useEffect(() => {
    if (history) {
      setMessages(history.map(m => ({ id: m.id, role: m.role === 0 ? 'user' : 'assistant', content: m.content })))
    }
  }, [history])

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const sendMessage = useCallback(async (text: string) => {
    if (!text.trim() || !sessionId || streaming) return
    setInput('')
    setStreaming(true)

    const userMsg: Message = { id: `u-${Date.now()}`, role: 'user', content: text }
    const aiMsg:   Message = { id: `a-${Date.now()}`, role: 'assistant', content: '', streaming: true, thinking: true }
    setMessages(prev => [...prev, userMsg, aiMsg])

    try {
      const response = await fetch(`/api/chat/sessions/${sessionId}/messages`, {
        method:  'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        body:    JSON.stringify({ content: text }),
      })

      if (!response.ok) {
        let errMsg = 'Failed to send message.'
        try {
          const errBody = await response.json() as { message?: string }
          if (errBody.message) errMsg = errBody.message
        } catch { /* ignore parse error */ }
        toast.error(errMsg, { duration: 5000 })
        setMessages(prev => prev.slice(0, -2))
        return
      }

      const reader  = response.body!.getReader()
      const decoder = new TextDecoder()
      let   buffer  = ''
      let   eventType = ''

      while (true) {
        const { done, value } = await reader.read()
        if (done) break
        buffer += decoder.decode(value, { stream: true })
        const lines = buffer.split('\n')
        buffer = lines.pop() ?? ''           // hold incomplete trailing line
        for (const line of lines) {
          if (line.startsWith('event: ')) { eventType = line.slice(7).trim(); continue }
          if (line === '') { eventType = ''; continue }
          if (!line.startsWith('data: ')) continue
          if (eventType === 'error') {
            try {
              const { error } = JSON.parse(line.slice(6)) as { error: string }
              toast.error(`AI error: ${error}`, { duration: 8000 })
            } catch { toast.error('AI service error.') }
            setMessages(prev => prev.slice(0, -2))
            return
          }
          try {
            const tok: string = JSON.parse(line.slice(6))
            setMessages(prev => {
              const updated = [...prev]
              const last    = updated[updated.length - 1]
              updated[updated.length - 1] = { ...last, content: last.content + tok, thinking: false }
              return updated
            })
          } catch { /* skip SSE comments and malformed lines */ }
        }
      }

      setMessages(prev => {
        const updated = [...prev]
        updated[updated.length - 1] = { ...updated[updated.length - 1], streaming: false, thinking: false }
        return updated
      })
    } catch {
      toast.error('Connection error.')
    } finally {
      setStreaming(false)
    }
  }, [sessionId, streaming, token])

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(input) }
  }

  return (
    <div className="h-screen flex flex-col bg-gradient-to-br from-violet-100 via-purple-50 to-pink-50 overflow-hidden">

      {/* Decorative background blobs */}
      <div className="fixed inset-0 pointer-events-none overflow-hidden">
        <div className="absolute -top-32 -left-32 w-72 h-72 bg-brand-200 rounded-full opacity-25 blur-3xl" />
        <div className="absolute -bottom-32 -right-32 w-72 h-72 bg-pink-200 rounded-full opacity-25 blur-3xl" />
        <div className="absolute top-1/3 right-10 w-40 h-40 bg-yellow-100 rounded-full opacity-30 blur-2xl" />
      </div>

      {/* ── Header ─────────────────────────────────────────── */}
      <header className="relative z-10 bg-white/80 backdrop-blur-md border-b-2 border-brand-200 px-4 sm:px-6 py-3 flex items-center gap-3 shadow-sm shrink-0">
        <Button variant="ghost" size="icon" className="shrink-0 hover:bg-brand-50" onClick={() => navigate('/student/select')}>
          <ArrowLeft className="w-5 h-5 text-brand-600" />
        </Button>

        <div className="w-10 h-10 bg-gradient-to-br from-brand-400 to-purple-600 rounded-2xl flex items-center justify-center shadow-md shrink-0 text-xl">
          🤖
        </div>

        <div className="flex-1 min-w-0">
          <h1 className="font-display font-extrabold text-gray-900 leading-tight truncate text-base sm:text-lg">
            {subjectName}
          </h1>
          <p className="text-xs text-brand-500 font-display font-semibold leading-none">
            {className} · AI Tutor
            {chapterTitles.length > 0 && (
              <span className="text-purple-500">
                {' · '}{chapterTitles.length === 1 ? chapterTitles[0] : `${chapterTitles.length} chapters`}
              </span>
            )}
          </p>
        </div>

        <div className="shrink-0 flex items-center gap-1 bg-brand-50 border border-brand-200 rounded-full px-3 py-1">
          <span className="w-2 h-2 bg-green-400 rounded-full animate-pulse" />
          <span className="text-xs font-display font-bold text-brand-600">Online</span>
        </div>
      </header>

      {/* ── Chat Frame ─────────────────────────────────────── */}
      <div className="flex-1 overflow-hidden relative z-10 flex flex-col p-3 sm:p-4 gap-3">
        <div className="flex-1 flex flex-col rounded-3xl border-2 border-brand-200 shadow-2xl overflow-hidden bg-white/75 backdrop-blur-sm">

          {/* Stars decoration strip */}
          <div className="flex items-center justify-center gap-3 py-2 bg-gradient-to-r from-brand-50 via-purple-50 to-pink-50 border-b border-brand-100">
            {['⭐', '🌟', '✨', '🌟', '⭐'].map((s, i) => (
              <span key={i} className="text-sm opacity-70">{s}</span>
            ))}
          </div>

          {/* Messages area */}
          <div className="flex-1 overflow-y-auto scrollbar-hide px-4 sm:px-6 py-4">

            {/* Welcome state */}
            {!historyLoading && messages.length === 0 && (
              <div className="text-center py-10 animate-fade-in">
                <div className="text-7xl mb-3">🎓</div>
                <h2 className="font-display text-2xl font-extrabold text-gray-800 mb-1">
                  Your AI Tutor is ready!
                </h2>
                <p className="text-gray-500 font-display mb-6 text-sm">
                  Ask me anything about{' '}
                  <span className="text-brand-600 font-bold">{subjectName}</span>
                  {chapterTitles.length > 0 && (
                    <span> · <span className="text-purple-500 font-bold">{chapterTitles.join(', ')}</span></span>
                  )}.
                  I'll answer from your study materials.
                </p>
                <div className="flex flex-wrap gap-2 justify-center">
                  {['📚 Explain the main topic', '📝 Give me a summary', '🧠 Quiz me!'].map(q => (
                    <button
                      type="button"
                      key={q}
                      onClick={() => sendMessage(q.replace(/^[^ ]+ /, ''))}
                      className="px-4 py-2 bg-white border-2 border-brand-200 rounded-full text-sm font-display font-bold text-brand-700 hover:bg-brand-50 hover:border-brand-400 transition-all shadow-sm hover:shadow-md hover:-translate-y-0.5"
                    >
                      {q}
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* Loading skeletons */}
            {historyLoading && (
              <div className="space-y-4 py-4">
                {Array.from({ length: 3 }).map((_, i) => (
                  <Skeleton key={i} className={`h-14 rounded-2xl ${i % 2 ? 'ml-16' : 'mr-16'}`} />
                ))}
              </div>
            )}

            {/* Messages */}
            {messages.map((m) => (
              <div
                key={m.id}
                className={`flex gap-3 mb-4 ${m.role === 'user' ? 'flex-row-reverse' : 'flex-row'} items-end`}
              >
                {/* AI avatar */}
                {m.role === 'assistant' && (
                  <div className="w-9 h-9 rounded-full bg-gradient-to-br from-brand-400 to-purple-600 flex items-center justify-center text-lg shrink-0 shadow-md">
                    🤖
                  </div>
                )}

                {/* Thinking state — show TypingIndicator bubble content inline */}
                {m.role === 'assistant' && m.thinking ? (
                  <div className="bg-gradient-to-r from-brand-50 to-purple-50 border-2 border-brand-200 rounded-3xl rounded-bl-sm px-5 py-3 shadow-sm">
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-display font-bold text-brand-600">Thinking</span>
                      <div className="flex gap-1 items-center">
                        <span className="w-2 h-2 bg-brand-400 rounded-full animate-bounce" />
                        <span className="w-2 h-2 bg-brand-400 rounded-full animate-bounce bounce-delay-1" />
                        <span className="w-2 h-2 bg-brand-400 rounded-full animate-bounce bounce-delay-2" />
                      </div>
                      <span className="text-base animate-spin">✨</span>
                    </div>
                  </div>
                ) : m.role === 'assistant' ? (
                  /* AI response bubble */
                  <div className="max-w-[82%] bg-white border-2 border-brand-100 rounded-3xl rounded-bl-sm px-4 py-3 shadow-md">
                    <div className="prose prose-sm max-w-none text-gray-800 font-display">
                      <ReactMarkdown>{m.content || (m.streaming ? ' ' : '')}</ReactMarkdown>
                      {m.streaming && (
                        <span className="inline-block w-2 h-4 bg-brand-500 ml-0.5 animate-pulse rounded-sm align-middle" />
                      )}
                    </div>
                  </div>
                ) : (
                  /* User bubble */
                  <div className="max-w-[82%] bg-gradient-to-br from-brand-500 to-purple-600 text-white rounded-3xl rounded-br-sm px-4 py-3 shadow-md">
                    <p className="text-sm leading-relaxed font-display font-medium">{m.content}</p>
                  </div>
                )}

                {/* User avatar */}
                {m.role === 'user' && (
                  <div className="w-9 h-9 rounded-full bg-gradient-to-br from-pink-400 to-rose-500 flex items-center justify-center text-lg shrink-0 shadow-md">
                    🧑‍🎓
                  </div>
                )}
              </div>
            ))}

            <div ref={messagesEndRef} />
          </div>

          {/* ── Input area ────────────────────────────────── */}
          <div className="border-t-2 border-brand-100 bg-white/90 px-4 sm:px-5 pt-3 pb-4 shrink-0">

            {/* Quick action buttons */}
            <div className="flex gap-2 mb-3 flex-wrap">
              <button
                type="button"
                onClick={() => sendMessage('Quiz me with 5 practice questions')}
                disabled={streaming || !sessionId}
                className="flex items-center gap-1.5 px-3 py-1.5 bg-amber-50 border-2 border-amber-200 rounded-full text-xs font-display font-bold text-amber-700 hover:bg-amber-100 hover:border-amber-300 transition-all disabled:opacity-40 shadow-sm"
              >
                <Zap className="w-3.5 h-3.5" /> Quiz me!
              </button>
              <button
                type="button"
                onClick={() => sendMessage('Give me a summary of the key points from the study material')}
                disabled={streaming || !sessionId}
                className="flex items-center gap-1.5 px-3 py-1.5 bg-emerald-50 border-2 border-emerald-200 rounded-full text-xs font-display font-bold text-emerald-700 hover:bg-emerald-100 hover:border-emerald-300 transition-all disabled:opacity-40 shadow-sm"
              >
                <BookOpenCheck className="w-3.5 h-3.5" /> Summarize
              </button>
              <button
                type="button"
                onClick={() => sendMessage('Give me 3 important points to remember')}
                disabled={streaming || !sessionId}
                className="flex items-center gap-1.5 px-3 py-1.5 bg-sky-50 border-2 border-sky-200 rounded-full text-xs font-display font-bold text-sky-700 hover:bg-sky-100 hover:border-sky-300 transition-all disabled:opacity-40 shadow-sm"
              >
                <Sparkles className="w-3.5 h-3.5" /> Key points
              </button>
            </div>

            {/* Text input row */}
            <div className="flex gap-2 items-end">
              <Textarea
                value={input}
                onChange={e => setInput(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder="Ask your AI tutor anything… ✏️"
                className="flex-1 resize-none rounded-2xl border-2 border-brand-200 focus-visible:ring-brand-400 focus-visible:border-brand-400 font-display min-h-[48px] max-h-32 text-sm bg-white/90"
                disabled={streaming || !sessionId}
                rows={1}
              />
              <Button
                size="icon"
                className="h-12 w-12 rounded-2xl shrink-0 bg-gradient-to-br from-brand-500 to-purple-600 hover:from-brand-600 hover:to-purple-700 shadow-lg hover:shadow-brand-300/50 transition-all hover:-translate-y-0.5"
                onClick={() => sendMessage(input)}
                disabled={!input.trim() || streaming || !sessionId}
              >
                <Send className="w-5 h-5" />
              </Button>
            </div>

            <p className="text-xs text-center text-gray-400 mt-2 font-display">
              Powered by local AI · your data stays private 🔒
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
