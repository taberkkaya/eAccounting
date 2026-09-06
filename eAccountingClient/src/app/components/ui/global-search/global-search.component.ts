import { Component, ElementRef, HostListener, ViewChild, inject } from '@angular/core';
import { Router } from '@angular/router';
import { SharedModule } from '../../../modules/shared.module';
import { HttpService } from '../../../services/http.service';
import { AuthService } from '../../../services/auth.service';

export interface SearchHit {
  kind: 'contact' | 'product' | 'invoice' | 'cash' | 'bank';
  id: string;
  title: string;
  hint: string | null;
  meta: string | null;
}

/** Sonuç türünden ikon ve adres. */
const KINDS: Record<SearchHit['kind'], { icon: string; label: string; link: (id: string) => string }> =
  {
    contact: { icon: 'fas fa-address-book', label: 'Cari', link: (id) => `/contacts/${id}` },
    product: { icon: 'fas fa-box', label: 'Ürün', link: () => '/products' },
    invoice: { icon: 'fas fa-file-invoice', label: 'Fatura', link: (id) => `/invoices/${id}` },
    cash: { icon: 'fas fa-cash-register', label: 'Kasa', link: (id) => `/cash-registers/details/${id}` },
    bank: { icon: 'fas fa-university', label: 'Banka', link: (id) => `/banks/details/${id}` },
  };

/** İstek atmadan önce beklenen süre; her tuşta sunucuya gitmemek için. */
const DEBOUNCE_MS = 220;

/**
 * Üst çubuktaki tek arama kutusu.
 *
 * Kayıtlar cari, ürün, fatura ve hesap ekranlarına dağılmış durumda; bir ismi
 * ararken önce hangi ekranda olduğuna karar vermek gerekiyordu. Burası o kararı
 * ortadan kaldırıyor: yaz, çıkanı seç, oraya git.
 */
@Component({
  selector: 'app-global-search',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './global-search.component.html',
  styleUrl: './global-search.component.css',
})
export class GlobalSearchComponent {
  private readonly http = inject(HttpService);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);

  @ViewChild('input') input?: ElementRef<HTMLInputElement>;

  private readonly host = inject(ElementRef<HTMLElement>);

  term = '';
  hits: SearchHit[] = [];
  open = false;
  loading = false;
  activeIndex = 0;

  private timer: ReturnType<typeof setTimeout> | null = null;

  /** Mac'te ⌘K, diğerlerinde Ctrl+K; ipucu metni de ona göre. */
  readonly shortcut = navigator.platform.toLowerCase().includes('mac') ? '⌘K' : 'Ctrl K';

  onInput(): void {
    if (this.timer) clearTimeout(this.timer);

    const term = this.term.trim();

    if (term.length < 2) {
      this.hits = [];
      this.open = term.length > 0;
      this.loading = false;
      return;
    }

    this.open = true;
    this.loading = true;
    this.timer = setTimeout(() => this.run(term), DEBOUNCE_MS);
  }

  private run(term: string): void {
    this.http.post<{ hits: SearchHit[] }>(
      'Search/Global',
      { term, take: 12 },
      (res) => {
        // Yazmaya devam edildiyse geç gelen cevabı yazma.
        if (this.term.trim() !== term) return;

        this.hits = res.hits ?? [];
        this.activeIndex = 0;
        this.loading = false;
      },
      () => (this.loading = false)
    );
  }

  go(hit: SearchHit): void {
    this.router.navigateByUrl(KINDS[hit.kind].link(hit.id));
    this.close();
  }

  close(): void {
    this.open = false;
    this.term = '';
    this.hits = [];
    this.input?.nativeElement.blur();
  }

  iconOf(kind: SearchHit['kind']): string {
    return KINDS[kind]?.icon ?? 'fas fa-circle';
  }

  labelOf(kind: SearchHit['kind']): string {
    return KINDS[kind]?.label ?? '';
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
      return;
    }

    if (!this.open || this.hits.length === 0) return;

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.activeIndex = (this.activeIndex + 1) % this.hits.length;
      return;
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.activeIndex = (this.activeIndex - 1 + this.hits.length) % this.hits.length;
      return;
    }

    if (event.key === 'Enter') {
      event.preventDefault();
      if (this.hits[this.activeIndex]) this.go(this.hits[this.activeIndex]);
    }
  }

  /** Ctrl+K / ⌘K her yerden aramaya odaklanır. */
  @HostListener('document:keydown', ['$event'])
  onShortcut(event: KeyboardEvent): void {
    if (event.key !== 'k' && event.key !== 'K') return;
    if (!event.ctrlKey && !event.metaKey) return;

    event.preventDefault();
    this.input?.nativeElement.focus();
    this.input?.nativeElement.select();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.open) return;
    if (this.host.nativeElement.contains(event.target as Node)) return;

    this.open = false;
  }
}
