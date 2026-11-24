import { CommonModule } from '@angular/common'
import { Component, OnInit, inject } from '@angular/core'
import { FormsModule } from '@angular/forms'
import { BackendApiService } from './backend-api.service'
import { ChatResponse, DemoUser } from './models'

interface ChatMessage {
  role: 'assistant' | 'user'
  content: string
  sources?: string[]
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly api = inject(BackendApiService)

  users: DemoUser[] = []
  selectedUserId = ''
  messages: ChatMessage[] = []
  draft = ''
  loadingUsers = true
  sending = false
  error = ''

  get selectedUser(): DemoUser | undefined {
    return this.users.find((user) => user.id === this.selectedUserId)
  }

  ngOnInit(): void {
    this.api.getUsers().subscribe({
      next: (users) => {
        this.users = users
        this.selectedUserId = users[0]?.id ?? ''
        this.updateGreeting()
        this.loadingUsers = false
      },
      error: (error: unknown) => {
        this.error = this.errorMessage(error, 'No se pudo conectar con TutorFlow.')
        this.loadingUsers = false
      },
    })
  }

  selectUser(userId: string): void {
    this.selectedUserId = userId
    this.error = ''
    this.updateGreeting()
  }

  sendMessage(): void {
    const message = this.draft.trim()
    const user = this.selectedUser
    if (!message || !user || this.sending || message.length > 2000) return

    this.draft = ''
    this.error = ''
    this.messages = [...this.messages, { role: 'user', content: message }]
    this.sending = true
    this.api.sendMessage({ userId: user.id, message }).subscribe({
      next: (response: ChatResponse) => {
        this.messages = [...this.messages, {
          role: 'assistant',
          content: response.answer,
          sources: response.sources,
        }]
        this.sending = false
      },
      error: (error: unknown) => {
        this.error = this.errorMessage(error, 'No se pudo enviar el mensaje.')
        this.sending = false
      },
    })
  }

  onComposerKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      this.sendMessage()
    }
  }

  roleLabel(role: DemoUser['role']): string {
    return role === 'Student' ? 'Estudiante' : 'Coordinador'
  }

  private updateGreeting(): void {
    const user = this.selectedUser
    this.messages = user
      ? [{ role: 'assistant', content: `Hola, ${user.name}. Pregúntame sobre el reglamento, tus datos académicos o la API.` }]
      : []
  }

  private errorMessage(error: unknown, fallback: string): string {
    if (typeof error === 'object' && error !== null && 'error' in error) {
      const body = (error as { error?: { message?: string; detail?: string; title?: string } }).error
      if (body?.message || body?.detail || body?.title) return body.message ?? body.detail ?? body.title ?? fallback
    }
    return error instanceof Error ? error.message : fallback
  }
}
