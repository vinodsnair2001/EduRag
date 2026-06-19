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
  hasPdf: boolean
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

export interface StudentClassDto {
  classId: number
  className: string
  grade: number
}

export interface StudentSubjectDto {
  subjectId: number
  subjectName: string
  description: string
}

export interface UserDto {
  id: string
  email: string
  fullName: string
  role: number
  isActive: boolean
  createdAt: string
  lastLoginAt?: string
  classId?: number
}

export interface StudentPermissionDto {
  id: string
  studentId: string
  subjectId: number
  subjectName: string
  grantedAt: string
}

export const STATUS_LABEL: Record<VectorizationStatus, string> = {
  0: 'Pending',
  1: 'Processing',
  2: 'Completed',
  3: 'Failed',
}
