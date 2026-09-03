import { environment } from '../environments/environment';

let resolvedApiUrl = environment.apiUrl;

/**
 * Base URL of the API. A function rather than a constant because a panel-only
 * deployment (no shell, no build step on the server) has to be able to point the
 * client at a different API host by editing assets/config.json.
 */
export const api = (): string => resolvedApiUrl;

/** Applied at startup from assets/config.json; an empty value keeps the built-in default. */
export function setApiUrl(url: string | undefined | null): void {
  const trimmed = url?.trim();
  if (!trimmed) return;

  resolvedApiUrl = trimmed.replace(/\/+$/, '');
}
