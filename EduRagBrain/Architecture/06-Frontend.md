---
tags: [architecture, frontend, react, typescript, shadcn, tailwind, sse]
created: 2026-06-18
updated: 2026-06-18
type: architecture
status: stable
aliases: [Frontend Architecture, React, shadcn/ui]
---

# Frontend Architecture

> [[_HOME|← Home]] · [[01-Clean-Architecture|← Clean Architecture]]

## Design Philosophy

The student-facing UI targets **children and teenagers (ages 8–18)**. The design is:
- **Playful but focused** — bright accent colours, rounded cards, friendly typography
- **Distraction-free** — clean layouts without clutter; large tap targets
- **Encouraging** — positive micro-copy, animated feedback on correct answers
- **Accessible** — WCAG AA contrast, keyboard navigable

The admin portal uses a professional, neutral theme suitable for teachers and administrators.

---

## Tech Stack

| Package | Version | Purpose |
|---------|---------|---------|
| React | 18 | UI runtime |
| TypeScript | 5.x | Type safety |
| Vite | 5.x | Build tool / dev server |
| Tailwind CSS | v3 | Utility CSS |
| **shadcn/ui** | latest | Component library (Radix UI primitives) |
| @tanstack/react-query | v5 | Server state, caching, loading states |
| react-router-dom | v6 | Client-side routing |
| axios | latest | HTTP client with interceptors |
| react-hot-toast | latest | Toast notifications |
| react-dropzone | latest | PDF upload drag-and-drop |
| react-markdown | latest | Render LLM markdown responses |
| lucide-react | latest | Icon set (ships with shadcn) |

### shadcn/ui Components Used

| Component | Where |
|-----------|-------|
| `Button` | All CTAs |
| `Card` | Class/subject tiles, practice question cards |
| `Dialog` | Create/edit class/subject/chapter modals |
| `Select` | Class and subject pickers |
| `Input`, `Textarea` | Forms and chat input |
| `Badge` | Vectorization status, role indicator |
| `Progress` | Upload progress bar |
| `Tabs` | Admin dashboard sections |
| `Avatar` | User initials in chat |
| `ScrollArea` | Chat message list |
| `Skeleton` | Loading placeholders |
| `Alert` | Error and info banners |
| `Separator` | Section dividers |
| `Tooltip` | Icon button labels |

---

## Setup Commands

```bash
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install

# Core
npm install axios react-router-dom @tanstack/react-query
npm install react-hot-toast react-dropzone react-markdown

# shadcn/ui (installs Radix UI + Tailwind)
npx shadcn@latest init
# choose: TypeScript, Tailwind CSS, default style: "New York", base color: Violet

# Install shadcn components
npx shadcn@latest add button card dialog select input textarea badge
npx shadcn@latest add progress tabs avatar scroll-area skeleton alert
npx shadcn@latest add separator tooltip

# Icons
npm install lucide-react
```

---

## Route Structure

```
/login                              → <LoginPage />           (public)

/admin/dashboard                    → <AdminDashboard />      [Admin role]
/admin/classes                      → <ClassListPage />
/admin/classes/:id                  → <ClassDetailPage />
/admin/classes/:cid/subjects/:sid/upload → <UploadMaterialPage />
/admin/materials                    → <MaterialListPage />
/admin/users                        → <UserManagementPage />

/student/select                     → <ClassSubjectSelectPage /> [Student role]
/student/chat/:classId/:subjectId   → <ChatPage />
```

---

## Student UI — Design Tokens

```css
/* tailwind.config.ts additions for student portal */
theme: {
  extend: {
    colors: {
      brand: {
        50:  '#f5f3ff',   /* lightest purple — page bg */
        500: '#7c3aed',   /* primary violet — buttons */
        600: '#6d28d9',
      },
      success: '#22c55e',
      warn:    '#f59e0b',
      danger:  '#ef4444',
    },
    fontFamily: {
      display: ['Nunito', 'sans-serif'],   /* headings — rounded, friendly */
      body:    ['Inter',  'sans-serif'],
    },
    borderRadius: { xl: '1rem', '2xl': '1.5rem' },
    boxShadow: {
      card: '0 4px 20px 0 rgba(124,58,237,0.10)',
    },
  },
}
```

---

## Auth Context

```typescript
// src/auth/AuthContext.tsx
interface AuthState {
  token: string | null;       // stored in memory (not localStorage — XSS risk)
  role: 'Admin' | 'Student' | null;
  userId: string | null;
  fullName: string | null;
}
// Refresh token stored in httpOnly cookie
// Axios interceptor adds: Authorization: Bearer {token}
// On 401: clear state → redirect /login
```

---

## Streaming Chat (SSE)

```typescript
// src/student/pages/ChatPage.tsx
const sendMessage = async (userText: string) => {
  setMessages(prev => [
    ...prev,
    { role: 'user',      content: userText },
    { role: 'assistant', content: '' },        // placeholder
  ]);

  const response = await fetch(`/api/chat/sessions/${sessionId}/messages`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
    body: JSON.stringify({ content: userText }),
  });

  const reader  = response.body!.getReader();
  const decoder = new TextDecoder();

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    const text  = decoder.decode(value);
    const lines = text.split('\n').filter(l => l.startsWith('data: '));
    for (const line of lines) {
      const token = line.replace('data: ', '');
      setMessages(prev => {
        const updated = [...prev];
        updated[updated.length - 1] = {
          ...updated[updated.length - 1],
          content: updated[updated.length - 1].content + token,
        };
        return updated;
      });
    }
  }
};
```

---

## Component Map

| Component | Path | Description |
|-----------|------|-------------|
| `<LoginPage />` | auth/pages/LoginPage.tsx | Single page, tab for Admin vs Student |
| `<ClassSubjectSelectPage />` | student/pages/ClassSubjectSelectPage.tsx | Grid of colourful class cards |
| `<ChatPage />` | student/pages/ChatPage.tsx | SSE stream chat + practice mode |
| `<ChatWindow />` | student/components/ChatWindow.tsx | Scrollable message list (markdown) |
| `<ChatInput />` | student/components/ChatInput.tsx | Textarea + send button + "Quiz me" shortcut |
| `<PracticeCard />` | student/components/PracticeCard.tsx | Shows question, collects answer, shows verdict |
| `<ScoreAnimation />` | student/components/ScoreAnimation.tsx | Confetti / star burst on correct answer |
| `<AdminDashboard />` | admin/pages/AdminDashboard.tsx | Stats cards: classes, subjects, materials |
| `<ClassListPage />` | admin/pages/ClassListPage.tsx | Table + create button |
| `<ClassDetailPage />` | admin/pages/ClassDetailPage.tsx | Subjects + chapters tree |
| `<UploadMaterialPage />` | admin/pages/UploadMaterialPage.tsx | Dropzone + metadata form |
| `<MaterialListPage />` | admin/pages/MaterialListPage.tsx | Table with status badges |
| `<UploadZone />` | admin/components/UploadZone.tsx | react-dropzone, PDF-only, progress bar |
| `<MaterialTable />` | admin/components/MaterialTable.tsx | Vectorization status colour coding |
| `<ClassTree />` | admin/components/ClassTree.tsx | Collapsible Class → Subject → Chapter |
| `<ProtectedRoute />` | shared/components/ProtectedRoute.tsx | Role guard, redirects if unauthorised |
| `<StatusBadge />` | shared/components/StatusBadge.tsx | Pending/Processing/Completed/Failed |

---

## Student UX Principles

1. **Class selection page** — Large rounded cards with subject emoji icons and gradient backgrounds per grade band (K-4: warm, 5-8: cool, 9-12: dark)
2. **Chat bubbles** — Student messages: violet right-aligned bubble. AI messages: white card with avatar, soft shadow
3. **Practice cards** — Card flip animation on answer reveal; green tick / red cross icon; star rating on score
4. **Loading states** — Animated typing indicator (three bouncing dots) while AI streams
5. **Empty states** — Friendly illustrated placeholder when no study materials uploaded yet

---

## Related Docs

- [[07-AI-Pipeline]] — SSE token source
- [[../System/02-API-Reference]] — endpoints consumed
- [[../User/02-Student-Guide]] — what students see
- [[../User/01-Admin-Guide]] — admin portal walkthrough
