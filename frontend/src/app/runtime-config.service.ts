import { Injectable } from '@angular/core'

interface RuntimeConfig {
  apiBaseUrl: string
}

@Injectable({ providedIn: 'root' })
export class RuntimeConfigService {
  private config: RuntimeConfig = { apiBaseUrl: 'http://localhost:5000' }

  get apiBaseUrl(): string {
    return this.config.apiBaseUrl.replace(/\/$/, '')
  }

  async load(): Promise<void> {
    try {
      const response = await fetch('/config.json', { cache: 'no-store' })
      if (!response.ok) return
      const config = await response.json() as Partial<RuntimeConfig>
      if (typeof config.apiBaseUrl === 'string' && config.apiBaseUrl.trim()) {
        this.config = { apiBaseUrl: config.apiBaseUrl.trim() }
      }
    } catch {
      // Local development can use the editable src/config.json default when no runtime server exists.
    }
  }
}
