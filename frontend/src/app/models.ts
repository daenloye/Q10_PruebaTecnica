export type UserRole = 'Student' | 'Coordinator'

export interface DemoUser {
  id: string
  name: string
  role: UserRole
  programId: string | null
  programIds: string[]
}

export interface ChatRequest {
  userId: string
  message: string
}

export interface ChatResponse {
  answer: string
  sources: string[]
  provider: string
}
