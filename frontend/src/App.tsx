import { FormEvent, KeyboardEvent, useEffect, useRef, useState } from 'react'

type DemoUser = {
  id: string
  name: string
  role: 'Student' | 'Coordinator'
  programId: string | null
  programIds: string[]
}

type ChatMessage = { role: 'assistant' | 'user'; content: string; sources?: string[] }
type ApiError = { message?: string; detail?: string; error?: string; title?: string }

const API_BASE = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000').replace(/\/$/, '')

async function readError(response: Response) {
  try {
    const body = (await response.json()) as ApiError
    return body.message || body.detail || body.title || body.error || `Error ${response.status}`
  } catch {
    return `Error ${response.status}`
  }
}

export default function App() {
  const [users, setUsers] = useState<DemoUser[]>([])
  const [selectedId, setSelectedId] = useState('')
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [draft, setDraft] = useState('')
  const [loading, setLoading] = useState(true)
  const [sending, setSending] = useState(false)
  const [error, setError] = useState('')
  const endRef = useRef<HTMLDivElement>(null)
  const user = users.find((candidate) => candidate.id === selectedId) ?? null

  useEffect(() => {
    fetch(`${API_BASE}/api/users`)
      .then(async (response) => {
        if (!response.ok) throw new Error(await readError(response))
        return (await response.json()) as DemoUser[]
      })
      .then((available) => {
        setUsers(available)
        setSelectedId(available[0]?.id ?? '')
      })
      .catch((reason: Error) => setError(`No se pudo conectar con TutorFlow: ${reason.message}`))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    setMessages(user ? [{ role: 'assistant', content: `Hola, ${user.name}. Pregúntame sobre el reglamento, tus datos académicos o la API.` }] : [])
  }, [selectedId, users])

  useEffect(() => endRef.current?.scrollIntoView({ behavior: 'smooth' }), [messages, sending])

  async function sendMessage(event?: FormEvent) {
    event?.preventDefault()
    const message = draft.trim()
    if (!message || !user || sending) return
    setDraft('')
    setError('')
    setMessages((current) => [...current, { role: 'user', content: message }])
    setSending(true)
    try {
      const response = await fetch(`${API_BASE}/api/chat/messages`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId: user.id, message }),
      })
      if (!response.ok) throw new Error(await readError(response))
      const reply = (await response.json()) as { answer: string; sources: string[] }
      setMessages((current) => [...current, { role: 'assistant', content: reply.answer, sources: reply.sources }])
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo enviar el mensaje.')
    } finally {
      setSending(false)
    }
  }

  function handleComposerKey(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      void sendMessage()
    }
  }

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <Brand />
        <div className="side-divider" />
        <label className="workspace-label" htmlFor="demo-user">PREGUNTAR COMO</label>
        <div className="user-selector">
          <select id="demo-user" value={selectedId} onChange={(event) => { setSelectedId(event.target.value); setError('') }} disabled={loading || sending || users.length === 0}>
            {loading && <option value="">Cargando usuarios…</option>}
            {!loading && users.length === 0 && <option value="">No hay usuarios disponibles</option>}
            {users.map((candidate) => <option key={candidate.id} value={candidate.id}>{candidate.name} · {candidate.role === 'Student' ? 'Estudiante' : 'Coordinador'}</option>)}
          </select>
        </div>
        {user && <p className="selected-user-detail">{user.id}{user.programId ? ` · ${user.programId}` : user.programIds.length ? ` · ${user.programIds.join(', ')}` : ''}</p>}
        <div className="sidebar-bottom">
          <div className="help-card"><div className="help-icon">✦</div><strong>¿Necesitas orientación?</strong><p>TutorFlow aplica los permisos asociados al usuario seleccionado.</p></div>
        </div>
      </aside>
      <section className="chat-area">
        <header className="topbar"><div className="breadcrumb">TutorFlow <span>/</span> <strong>Asistente</strong></div><div className="connection"><span className="status-dot" />{user ? `${user.role === 'Student' ? 'Estudiante' : 'Coordinador'} · ${user.name}` : 'Conectando'}</div></header>
        <div className="chat-content">
          <div className="chat-heading"><div className="assistant-mark">✦</div><div><p className="eyebrow">ASISTENTE ACADÉMICO</p><h1>¿En qué te puedo ayudar?</h1><p>Pregunta sobre el reglamento o la información disponible para el usuario seleccionado.</p></div></div>
          <div className="messages" aria-live="polite">
            {messages.map((message, index) => <article className={`message-row ${message.role}`} key={`${index}-${message.role}`}>
              {message.role === 'assistant' && <div className="message-avatar">✦</div>}
              <div className="message-content"><div className="message-bubble">{message.content.split('\n').map((line, lineIndex, lines) => <span key={lineIndex}>{line}{lineIndex < lines.length - 1 && <br />}</span>)}</div>{message.sources && message.sources.length > 0 && <div className="sources">Fuentes: {message.sources.join(' · ')}</div>}</div>
            </article>)}
            {sending && <article className="message-row assistant"><div className="message-avatar">✦</div><div className="typing"><i /><i /><i /></div></article>}
            <div ref={endRef} />
          </div>
          <div className="composer-wrap">
            {error && <p className="error-banner chat-error" role="alert">{error}</p>}
            <form className="composer" onSubmit={(event) => void sendMessage(event)}>
              <textarea value={draft} onChange={(event) => setDraft(event.target.value)} onKeyDown={handleComposerKey} rows={1} maxLength={2000} placeholder="Escribe tu pregunta…" aria-label="Escribe tu pregunta" />
              <button className="send-button" disabled={!draft.trim() || !user || sending} aria-label="Enviar mensaje" title="Enviar mensaje">↑</button>
            </form>
            <div className="composer-hint"><span>Enter para enviar</span><span>·</span><span>Shift + Enter para nueva línea</span><span className="hint-spacer" /><span>{draft.length}/2000</span></div>
          </div>
        </div>
        <footer className="chat-footer">TutorFlow puede equivocarse. Verifica las decisiones académicas importantes con tu coordinación.</footer>
      </section>
    </main>
  )
}

function Brand() {
  return <div className="brand"><div className="brand-symbol">✳</div><span>Tutor<span>Flow</span></span></div>
}
