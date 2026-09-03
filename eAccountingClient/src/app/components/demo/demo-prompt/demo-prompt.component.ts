import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { DemoService } from '../../../services/demo.service';

/**
 * The contact prompt. It appears twice at most: once as a dismissible invitation part
 * way through the session, and once for real when the session's quota or clock runs out.
 */
@Component({
  selector: 'app-demo-prompt',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './demo-prompt.component.html',
  styleUrl: './demo-prompt.component.css',
})
export class DemoPromptComponent {
  readonly demo = inject(DemoService);

  readonly kind = computed(() => this.demo.prompt());

  readonly title = computed(() =>
    this.kind() === 'ended'
      ? 'Demo oturumunuz sona erdi'
      : 'Buraya kadar geldiğinize göre beğendiniz'
  );

  readonly message = computed(() =>
    this.kind() === 'ended'
      ? 'Bu demo, her ziyaretçiye kendi izole veritabanını veren sınırlı bir oturum çalıştırır. Oturumunuz tamamlandı ve veriler sıfırlandı. Projenin mimarisi, kaynak kodu veya benzer bir çözüm hakkında konuşmak isterseniz bir mesaj bırakın.'
      : 'Bu bir portföy demosu; verileriniz oturum sonunda sıfırlanır. Projenin nasıl kurgulandığını merak ediyorsanız ya da benzer bir iş için görüşmek isterseniz iletişime geçebilirsiniz.'
  );

  contact(): void {
    this.demo.openContactPage();
  }

  startFresh(): void {
    this.demo.reset().subscribe({
      next: () => window.location.reload(),
      error: () => this.demo.exit(),
    });
  }

  dismiss(): void {
    this.demo.dismissPrompt();
  }

  leave(): void {
    this.demo.exit();
  }
}
