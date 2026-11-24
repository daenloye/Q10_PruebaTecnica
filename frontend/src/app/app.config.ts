import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core'
import { provideHttpClient } from '@angular/common/http'
import { RuntimeConfigService } from './runtime-config.service'

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideHttpClient(),
    provideAppInitializer(() => inject(RuntimeConfigService).load()),
  ],
}
