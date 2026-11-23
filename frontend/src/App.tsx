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
const SESSION_KEY = 'tutorflow.session'

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
  const [user, setUser] = useState<DemoUser | null>(null)
  const [token, setToken] = useState('')
  const [selectedId, setSelectedId] = useState('')
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [draft, setDraft] = useState('')
  const [loading, setLoading] = useState(false)
  const [sending, setSending] = useState(false)
  const [error, setError] = useState('')
  const endRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    fetch(`${API_BASE}/api/auth/users`)
      .then(async (response) => {
        if (!response.ok) throw new Error(await readError(response))
        return (await response.json()) as DemoUser[]
      })
      .then((available) => {
        setUsers(available)
        setSelectedId(available[0]?.id ?? '')
        const saved = localStorage.getItem(SESSION_KEY)
        if (saved) {
          try {
            const session = JSON.parse(saved) as { token: string; user: DemoUser }
            if (session.token && session.user) {
              setToken(session.token)
              setUser(session.user)
              setMessages([{ role: 'assistant', content: `Hola, ${session.user.name}. Soy TutorFlow. ¿Qué te gustaría consultar?` }])
            }
          } catch {
            localStorage.removeItem(SESSION_KEY)
          }
        }
      })
      .catch((reason: Error) => setError(`No se pudo conectar con TutorFlow: ${reason.message}`))
  }, [])

  useEffect(() => endRef.current?.scrollIntoView({ behavior: 'smooth' }), [messages, sending])

  async function login(event: FormEvent) {
    event.preventDefault()
    setError('')
    setLoading(true)
    try {
      const response = await fetch(`${API_BASE}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId: selectedId }),
      })
      if (!response.ok) throw new Error(await readError(response))
      const session = (await response.json()) as { token: string; user: DemoUser }
      localStorage.setItem(SESSION_KEY, JSON.stringify(session))
      setToken(session.token)
      setUser(session.user)
      setMessages([{ role: 'assistant', content: `Hola, ${session.user.name}. Soy TutorFlow. Pregúntame sobre el reglamento, tus datos académicos o la API.` }])
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo iniciar sesión.')
    } finally {
      setLoading(false)
    }
  }

  async function logout() {
    try {
      await fetch(`${API_BASE}/api/auth/logout`, { method: 'POST', headers: { Authorization: `Bearer ${token}` } })
    } finally {
      localStorage.removeItem(SESSION_KEY)
      setToken('')
      setUser(null)
      setMessages([])
      setError('')
    }
  }

  async function sendMessage(event?: FormEvent) {
    event?.preventDefault()
    const message = draft.trim()
    if (!message || !token || sending) return
    setDraft('')
    setError('')
    setMessages((current) => [...current, { role: 'user', content: message }])
    setSending(true)
    try {
      const response = await fetch(`${API_BASE}/api/chat/messages`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({ message }),
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

  if (!user) {
    return (
      <main className="login-shell">
        <div className="login-art" aria-hidden="true">
          <div className="art-orbit orbit-one" /><div className="art-orbit orbit-two" />
          <div className="art-note"><span>✦</span><p>Aprender con claridad<br />empieza con una pregunta.</p><small>Tu espacio académico, en un solo lugar.</small></div>
          <div className="art-pill pill-a">Reglamento</div><div className="art-pill pill-b">Tu progreso</div>
        </div>
        <section className="login-panel">
          <Brand />
          <div className="login-copy">
            <p className="eyebrow">ASISTENTE ACADÉMICO</p>
            <h1>Tu camino académico,<br /><span>más claro.</span></h1>
            <p>Consulta el reglamento, tus notas y la información de tu programa conversando con TutorFlow.</p>
          </div>
          <form className="login-form" onSubmit={login}>
            <label htmlFor="demo-user">Ingresa como usuario de prueba</label>
            <div className="select-wrap">
              <select id="demo-user" value={selectedId} onChange={(event) => setSelectedId(event.target.value)} disabled={users.length === 0}>
                {users.length === 0 && <option value="">Conectando con el backend…</option>}
                {users.map((candidate) => <option key={candidate.id} value={candidate.id}>{candidate.name} · {candidate.role === 'Student' ? 'Estudiante' : 'Coordinador'} · {candidate.id}</option>)}
              </select><span aria-hidden="true">⌄</span>
            </div>
            <button className="primary-button login-button" disabled={loading || users.length === 0}>
              {loading ? 'Creando sesión…' : 'Entrar al chat'} <span aria-hidden="true">→</span>
            </button>
            {error && <p className="error-banner" role="alert">{error}</p>}
            <p className="demo-note"><span className="status-dot" /> Entorno de demostración · Usuarios ficticios</p>
          </form>
          <footer className="login-footer">Corporación Educativa Horizonte <span>·</span> TutorFlow</footer>
        </section>
      </main>
    )
  }

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <Brand />
        <div className="side-divider" />
        <div className="workspace-label">TU ESPACIO</div>
        <div className="nav-item active"><span className="nav-icon">◌</span><span>Asistente académico</span><span className="nav-indicator" /></div>
        <div className="sidebar-bottom">
          <div className="help-card"><div className="help-icon">✦</div><strong>¿Necesitas orientación?</strong><p>TutorFlow te ayuda a encontrar la información que buscas.</p></div>
          <div className="side-divider" />
          <div className="profile-row"><div className="avatar">{user.name.split(' ').map((part) => part[0]).slice(0, 2).join('')}</div><div className="profile-copy"><strong>{user.name}</strong><span>{user.role === 'Student' ? 'Estudiante' : 'Coordinador'}{user.programId ? ` · ${user.programId}` : ''}</span></div><button className="logout-button" onClick={() => void logout()} aria-label="Cerrar sesión" title="Cerrar sesión">↗</button></div>
        </div>
      </aside>
      <section className="chat-area">
        <header className="topbar"><div className="breadcrumb">TutorFlow <span>/</span> <strong>Asistente</strong></div><div className="connection"><span className="status-dot" /> Sesión activa <span className="topbar-separator">·</span> {user.role === 'Student' ? 'Estudiante' : 'Coordinador'}</div></header>
        <div className="chat-content">
          <div className="chat-heading"><div className="assistant-mark">✦</div><div><p className="eyebrow">ASISTENTE ACADÉMICO</p><h1>¿En qué te puedo ayudar?</h1><p>Pregunta sobre el reglamento o la información disponible para tu rol.</p></div></div>
          <div className="messages" aria-live="polite">
            {messages.map((message, index) => <article className={`message-row ${message.role}`} key={`${index}-${message.role}`}>
              {message.role === 'assistant' && <div className="message-avatar">✦</div>}
              <div className="message-content"><div className="message-bubble">{message.content.split('\n').map((line, lineIndex) => <span key={lineIndex}>{line}{lineIndex < message.content.split('\n').length - 1 && <br />}</span>)}</div>{message.sources && message.sources.length > 0 && <div className="sources">Fuentes: {message.sources.join(' · ')}</div>}</div>
            </article>)}
            {sending && <article className="message-row assistant"><div className="message-avatar">✦</div><div className="typing"><i /><i /><i /></div></article>}
            <div ref={endRef} />
          </div>
          <div className="composer-wrap">
            {error && <p className="error-banner chat-error" role="alert">{error}</p>}
            <form className="composer" onSubmit={(event) => void sendMessage(event)}>
              <textarea value={draft} onChange={(event) => setDraft(event.target.value)} onKeyDown={handleComposerKey} rows={1} maxLength={2000} placeholder="Escribe tu pregunta…" aria-label="Escribe tu pregunta" />
              <button className="send-button" disabled={!draft.trim() || sending} aria-label="Enviar mensaje" title="Enviar mensaje">↑</button>
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
