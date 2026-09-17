export interface DemoStatusModel {
  sessionId: string;
  companyName: string;
  writesUsed: number;
  writeLimit: number;
  /** Writes after which the visitor is invited to get in touch, without ending the session. */
  nudgeAfterWrites: number;
  expiresAt: string;
  secondsRemaining: number;
  contactUrl: string;
  isActive: boolean;
  endReason: string | null;
}

export interface DemoStartModel {
  accessToken: string;
  status: DemoStatusModel;
}

/** Girişten önce e-posta doğrulaması istenip istenmediği. */
export interface DemoConfigModel {
  enabled: boolean;
  emailVerificationRequired: boolean;
}

/**
 * Kod isteğinin sonucu. alreadyVerified true ise kod gönderilmedi ve gerekmiyor:
 * adres yakın zamanda doğrulanmış, kod adımı atlanır.
 */
export interface DemoCodeResultModel {
  message: string;
  alreadyVerified: boolean;
}

/** Why the demo prompt is on screen. */
export type DemoPromptKind = 'nudge' | 'ended';

/** Sent by the API in the error body of a rejected demo request. */
export type DemoErrorCode = 'session_ended' | 'write_limit' | 'action_blocked';
