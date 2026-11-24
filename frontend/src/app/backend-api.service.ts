import { HttpClient } from '@angular/common/http'
import { Injectable, inject } from '@angular/core'
import { Observable } from 'rxjs'
import { ChatRequest, ChatResponse, DemoUser } from './models'
import { RuntimeConfigService } from './runtime-config.service'

@Injectable({ providedIn: 'root' })
export class BackendApiService {
  private readonly http = inject(HttpClient)
  private readonly config = inject(RuntimeConfigService)

  getUsers(): Observable<DemoUser[]> {
    return this.http.get<DemoUser[]>(`${this.config.apiBaseUrl}/api/users`)
  }

  sendMessage(request: ChatRequest): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${this.config.apiBaseUrl}/api/chat/messages`, request)
  }
}
