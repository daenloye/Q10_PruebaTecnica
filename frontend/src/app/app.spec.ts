import { ComponentFixture, TestBed } from '@angular/core/testing'
import { of } from 'rxjs'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { BackendApiService } from './backend-api.service'
import { App } from './app'

describe('App', () => {
  const api = {
    getUsers: vi.fn(),
    sendMessage: vi.fn(),
  }
  let fixture: ComponentFixture<App>

  beforeEach(async () => {
    vi.clearAllMocks()
    api.getUsers.mockReturnValue(of([
      { id: 'EST-001', name: 'Laura Gómez', role: 'Student', programId: 'ING-SIS', programIds: [] },
      { id: 'COORD-01', name: 'Marcela Ríos', role: 'Coordinator', programId: null, programIds: ['ING-SIS'] },
    ]))
    api.sendMessage.mockReturnValue(of({ answer: 'La respuesta usa las fuentes disponibles.', sources: ['rules.md'], provider: 'gemini' }))

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [{ provide: BackendApiService, useValue: api }],
    }).compileComponents()
    fixture = TestBed.createComponent(App)
    fixture.detectChanges()
  })

  it('shows the user selector with roles and selects a user', () => {
    const selector = fixture.nativeElement.querySelector('#demo-user') as HTMLSelectElement
    expect(selector.options.length).toBe(2)
    expect(selector.options[0].textContent).toContain('Estudiante')

    selector.value = 'COORD-01'
    selector.dispatchEvent(new Event('change'))
    fixture.detectChanges()
    expect(fixture.nativeElement.querySelector('.connection').textContent).toContain('Coordinador · Marcela Ríos')
  })

  it('sends a chat message as the selected user and renders the response', () => {
    const textarea = fixture.nativeElement.querySelector('textarea') as HTMLTextAreaElement
    textarea.value = '  ¿Cuál es la regla?  '
    textarea.dispatchEvent(new Event('input'))
    fixture.detectChanges()
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
    fixture.detectChanges()

    expect(api.sendMessage).toHaveBeenCalledWith({ userId: 'EST-001', message: '¿Cuál es la regla?' })
    expect(fixture.nativeElement.textContent).toContain('La respuesta usa las fuentes disponibles.')
    expect(fixture.nativeElement.textContent).toContain('rules.md')
  })
})
