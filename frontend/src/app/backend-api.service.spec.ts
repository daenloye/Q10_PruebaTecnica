import { TestBed } from '@angular/core/testing'
import { provideHttpClient } from '@angular/common/http'
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { BackendApiService } from './backend-api.service'
import { RuntimeConfigService } from './runtime-config.service'

describe('BackendApiService', () => {
  let service: BackendApiService
  let http: HttpTestingController

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    })
    service = TestBed.inject(BackendApiService)
    http = TestBed.inject(HttpTestingController)
    vi.spyOn(TestBed.inject(RuntimeConfigService), 'apiBaseUrl', 'get').mockReturnValue('http://backend.test')
  })

  afterEach(() => http.verify())

  it('loads users and roles from GET /api/users', () => {
    let result: unknown
    service.getUsers().subscribe((users) => result = users)

    const request = http.expectOne('http://backend.test/api/users')
    expect(request.request.method).toBe('GET')
    request.flush([{ id: 'EST-001', name: 'Laura Gómez', role: 'Student', programId: 'ING-SIS', programIds: [] }])
    expect(result).toEqual([{ id: 'EST-001', name: 'Laura Gómez', role: 'Student', programId: 'ING-SIS', programIds: [] }])
  })

  it('posts the selected user and message to POST /api/chat/messages', () => {
    const body = { userId: 'COORD-01', message: 'Consulta de prueba' }
    let result: unknown
    service.sendMessage(body).subscribe((response) => result = response)

    const request = http.expectOne('http://backend.test/api/chat/messages')
    expect(request.request.method).toBe('POST')
    expect(request.request.body).toEqual(body)
    request.flush({ answer: 'Respuesta', sources: ['rules.md'], provider: 'gemini' })
    expect(result).toEqual({ answer: 'Respuesta', sources: ['rules.md'], provider: 'gemini' })
  })
})
