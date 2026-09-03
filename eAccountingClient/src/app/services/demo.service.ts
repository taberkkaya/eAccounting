import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { api } from '../constants';
import {
  DemoErrorCode,
  DemoPromptKind,
  DemoStartModel,
  DemoStatusModel,
} from '../models/demo.model';
import { ResultModel } from '../models/result.model';

const DEMO_FLAG_KEY = 'demoSession';
const TOKEN_KEY = 'accessToken';

@Injectable({ providedIn: 'root' })
export class DemoService {
  readonly status = signal<DemoStatusModel | null>(null);
  readonly prompt = signal<DemoPromptKind | null>(null);
  readonly secondsRemaining = signal(0);
  readonly starting = signal(false);

  readonly writesLeft = computed(() => {
    const status = this.status();
    return status ? Math.max(0, status.writeLimit - status.writesUsed) : 0;
  });

  readonly contactUrl = computed(() => this.status()?.contactUrl ?? 'https://ataberkkaya.com');

  /** Set once per session so the invitation to get in touch is not shown repeatedly. */
  private nudged = false;
  private ticker?: ReturnType<typeof setInterval>;

  constructor(private http: HttpClient, private router: Router) {}

  get isDemo(): boolean {
    return localStorage.getItem(DEMO_FLAG_KEY) === 'true';
  }

  start(): Observable<ResultModel<DemoStartModel>> {
    this.starting.set(true);

    return this.http
      .post<ResultModel<DemoStartModel>>(`${api}/demo/start`, {})
      .pipe(tap({
        next: (res) => {
          this.starting.set(false);
          if (res.data) this.adopt(res.data);
        },
        error: () => this.starting.set(false),
      }));
  }

  /** Wipes the sandbox and hands the visitor a fresh one without leaving the app. */
  reset(): Observable<ResultModel<DemoStartModel>> {
    this.starting.set(true);

    return this.http
      .post<ResultModel<DemoStartModel>>(`${api}/demo/reset`, {})
      .pipe(tap({
        next: (res) => {
          this.starting.set(false);
          if (res.data) this.adopt(res.data);
        },
        error: () => this.starting.set(false),
      }));
  }

  refreshStatus(): void {
    if (!this.isDemo) return;

    this.http.get<ResultModel<DemoStatusModel>>(`${api}/demo/status`).subscribe({
      next: (res) => {
        if (res.data) this.applyStatus(res.data);
      },
      // A dead session is reported through the interceptor already.
      error: () => {},
    });
  }

  /** Called by the interceptor when the API rejects a request for a demo-specific reason. */
  handleError(code: DemoErrorCode): void {
    if (code === 'action_blocked') return;

    this.stopTicker();
    this.prompt.set('ended');
  }

  dismissPrompt(): void {
    this.prompt.set(null);
  }

  openContactPage(): void {
    window.open(this.contactUrl(), '_blank', 'noopener');
  }

  /** Leaves the demo behind and returns to the sign-in screen. */
  exit(): void {
    if (this.isDemo) {
      this.http.post(`${api}/demo/end`, {}).subscribe({ next: () => {}, error: () => {} });
    }

    this.clear();
    this.router.navigateByUrl('/login');
  }

  clear(): void {
    this.stopTicker();
    localStorage.removeItem(DEMO_FLAG_KEY);
    localStorage.removeItem(TOKEN_KEY);
    this.status.set(null);
    this.prompt.set(null);
    this.nudged = false;
  }

  private adopt(start: DemoStartModel): void {
    localStorage.setItem(TOKEN_KEY, start.accessToken);
    localStorage.setItem(DEMO_FLAG_KEY, 'true');
    this.nudged = false;
    this.prompt.set(null);
    this.applyStatus(start.status);
  }

  private applyStatus(status: DemoStatusModel): void {
    this.status.set(status);
    this.secondsRemaining.set(status.secondsRemaining);
    this.startTicker();

    if (!this.nudged && status.writesUsed >= status.nudgeAfterWrites && status.writesUsed < status.writeLimit) {
      this.nudged = true;
      this.prompt.set('nudge');
    }
  }

  private startTicker(): void {
    if (this.ticker) return;

    this.ticker = setInterval(() => {
      const remaining = this.secondsRemaining() - 1;
      this.secondsRemaining.set(Math.max(0, remaining));

      if (remaining <= 0) {
        this.stopTicker();
        this.prompt.set('ended');
      }
    }, 1000);
  }

  private stopTicker(): void {
    if (!this.ticker) return;

    clearInterval(this.ticker);
    this.ticker = undefined;
  }
}
