import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from './AuthContext'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import toast from 'react-hot-toast'
import { BookOpen, GraduationCap, Loader2, Sparkles } from 'lucide-react'

export default function LoginPage() {
  const { login } = useAuth()
  const navigate   = useNavigate()
  const [loading, setLoading] = useState(false)
  const [form, setForm] = useState({ email: '', password: '' })

  const handleLogin = async (role: 'Admin' | 'Student') => {
    if (!form.email || !form.password) {
      toast.error('Please fill in all fields.')
      return
    }
    setLoading(true)
    try {
      const { data } = await api.post('/auth/login', form)
      if (data.role !== role) {
        toast.error(`This account is not a ${role} account.`)
        return
      }
      login(data.token, data.role, data.userId, data.fullName)
      toast.success(`Welcome back, ${data.fullName}!`)
      navigate(role === 'Admin' ? '/admin/dashboard' : '/student/select')
    } catch {
      toast.error('Invalid email or password.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-brand-50 via-purple-50 to-indigo-100 p-4">
      {/* Decorative blobs */}
      <div className="absolute top-20 left-20 w-72 h-72 bg-brand-200 rounded-full mix-blend-multiply filter blur-2xl opacity-30 animate-pulse" />
      <div className="absolute bottom-20 right-20 w-72 h-72 bg-indigo-200 rounded-full mix-blend-multiply filter blur-2xl opacity-30 animate-pulse" />

      <div className="relative z-10 w-full max-w-md">
        {/* Logo / Brand */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-brand-500 rounded-2xl shadow-lg mb-4">
            <Sparkles className="w-8 h-8 text-white" />
          </div>
          <h1 className="font-display text-4xl font-extrabold text-brand-700 tracking-tight">EduRAG</h1>
          <p className="text-muted-foreground mt-1 font-display">Your AI-Powered Learning Companion</p>
        </div>

        <Card className="shadow-card border-brand-100">
          <CardHeader className="pb-2">
            <CardTitle className="font-display text-xl text-center">Sign In</CardTitle>
            <CardDescription className="text-center">Choose your role to get started</CardDescription>
          </CardHeader>
          <CardContent>
            <Tabs defaultValue="Student">
              <TabsList className="grid grid-cols-2 w-full mb-6">
                <TabsTrigger value="Student" className="gap-2 font-display font-semibold">
                  <GraduationCap className="w-4 h-4" /> Student
                </TabsTrigger>
                <TabsTrigger value="Admin" className="gap-2 font-display font-semibold">
                  <BookOpen className="w-4 h-4" /> Admin
                </TabsTrigger>
              </TabsList>

              {(['Student', 'Admin'] as const).map((role) => (
                <TabsContent key={role} value={role}>
                  <div className="space-y-4">
                    {role === 'Student' && (
                      <p className="text-sm text-center text-muted-foreground font-display">
                        Ready to learn? Jump in and chat with your study materials! 🚀
                      </p>
                    )}
                    <div className="space-y-3">
                      <Input
                        type="email"
                        placeholder="Email address"
                        value={form.email}
                        onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
                        className="h-11"
                        onKeyDown={(e) => e.key === 'Enter' && handleLogin(role)}
                      />
                      <Input
                        type="password"
                        placeholder="Password"
                        value={form.password}
                        onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
                        className="h-11"
                        onKeyDown={(e) => e.key === 'Enter' && handleLogin(role)}
                      />
                    </div>
                    <Button
                      className="w-full h-11 font-display font-bold text-base"
                      onClick={() => handleLogin(role)}
                      disabled={loading}
                    >
                      {loading ? <Loader2 className="w-4 h-4 animate-spin" /> : `Sign in as ${role}`}
                    </Button>
                  </div>
                </TabsContent>
              ))}
            </Tabs>
          </CardContent>
        </Card>

        <p className="text-center text-xs text-muted-foreground mt-6 font-display">
          Powered by local AI — your data stays private ✨
        </p>
      </div>
    </div>
  )
}
