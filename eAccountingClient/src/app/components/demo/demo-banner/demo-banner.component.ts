import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { DemoService } from '../../../services/demo.service';

/**
 * The quota strip in the navbar. It exists so a visitor is never surprised by the
 * session ending: the remaining operations and time are visible the whole way.
 */
@Component({
  selector: 'app-demo-banner',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './demo-banner.component.html',
  styleUrl: './demo-banner.component.css',
})
export class DemoBannerComponent {
  readonly demo = inject(DemoService);

  readonly isDemo = computed(() => this.demo.status() !== null);

  readonly timeLeft = computed(() => {
    const total = this.demo.secondsRemaining();
    const minutes = Math.floor(total / 60);
    const seconds = total % 60;

    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  });

  /** Turns amber once the quota is nearly spent, so the limit is not a surprise. */
  readonly isRunningOut = computed(() => {
    const status = this.demo.status();
    if (!status) return false;

    return this.demo.writesLeft() <= Math.max(3, Math.round(status.writeLimit * 0.15));
  });

  resetSandbox(): void {
    this.demo.reset().subscribe({
      next: () => window.location.reload(),
      error: () => {},
    });
  }

  contact(): void {
    this.demo.openContactPage();
  }
}
