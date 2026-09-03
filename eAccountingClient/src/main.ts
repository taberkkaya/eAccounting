import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { setApiUrl } from './app/constants';

/**
 * Read before the app boots so every request already knows where the API lives.
 * A missing or unreadable file is not an error: the build-time environment stands.
 */
async function loadRuntimeConfig(): Promise<void> {
  try {
    const response = await fetch('assets/config.json', { cache: 'no-cache' });
    if (!response.ok) return;

    const config = await response.json();
    setApiUrl(config?.apiUrl);
  } catch {
    // Served without a config file, which is the normal case for the container image.
  }
}

loadRuntimeConfig().then(() =>
  bootstrapApplication(AppComponent, appConfig).catch((err) => console.error(err))
);
