export type UserRole = 'Admin' | 'Student'
export type MessageRole = 0 | 1
export type VectorizationStatus = 0 | 1 | 2 | 3

export interface AuthState {
  token: string | null
  role: UserRole | null
  userId: string | null
  fullName: string | null
}

export interface ClassDto {
  id: number
  name: string
  grade: number
  isActive: boolean
  createdAt: string
}

export interface SubjectDto {
  id: number
  name: string
  description: string
  classId: number
  isActive: boolean
}

export interface ChapterDto {
  id: number
  title: string
  orderIndex: number
  subjectId: number
  isActive: boolean
}

export interface MaterialDto {
  id: string
  originalFileName: string
  fileSizeBytes: number
  status: VectorizationStatus
  uploadedAt: string
  vectorizationError?: string
  classId: number
  subjectId: number
  chapterId?: number
}

export interface ChatMessageDto {
  id: string
  content: string
  role: MessageRole
  sentAt: string
}

export interface UserDto {
  id: string
  email: string
  fullName: string
  role: number
  isActive: boolean
  createdAt: string
  lastLoginAt?: string
}

export const STATUS_LABEL: Record<VectorizationStatus, string> = {
  0: 'Pending',
  1: 'Processing',
  2: 'Completed',
  3: 'Failed',
}
