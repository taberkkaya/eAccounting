import {
  Component,
  ElementRef,
  HostListener,
  Input,
  ViewChild,
  inject,
} from '@angular/core';

/**
 * Tablo satırlarındaki işlemler için üç nokta menüsü.
 *
 * Açılan kutu sabit konumlandırılıyor (position: fixed) ve koordinatları düğmeden
 * hesaplanıyor: tablolar yatay kayabildiği için normal bir açılır kutu taşma
 * kutusunun içinde kırpılır ve yarısı görünmez olurdu. Ekranın kenarına yakınsa
 * yukarı ya da sola dönüyor.
 */
@Component({
  selector: 'app-action-menu',
  standalone: true,
  templateUrl: './action-menu.component.html',
  styleUrl: './action-menu.component.css',
})
export class ActionMenuComponent {
  /** Ekran okuyucular ve fare üstü ipucu için. */
  @Input() label = 'İşlemler';

  @ViewChild('trigger') trigger?: ElementRef<HTMLButtonElement>;

  private readonly host = inject(ElementRef<HTMLElement>);

  open = false;
  top = 0;
  left = 0;

  toggle(event: MouseEvent): void {
    event.stopPropagation();

    if (this.open) {
      this.open = false;
      return;
    }

    this.open = true;
    this.place();
  }

  /** Menüdeki bir şeye tıklanınca kapansın. */
  onPanelClick(): void {
    this.open = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.open) return;
    if (this.host.nativeElement.contains(event.target as Node)) return;

    this.open = false;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.open = false;
  }

  // Sabit konumlandırıldığı için sayfa kayınca düğmeden kopar; kapatmak
  // yeniden hesaplamaktan daha dürüst.
  @HostListener('window:scroll')
  @HostListener('window:resize')
  onViewportChange(): void {
    this.open = false;
  }

  private place(): void {
    const button = this.trigger?.nativeElement;
    if (!button) return;

    const rect = button.getBoundingClientRect();
    const width = 190;
    const estimatedHeight = 150;
    const gap = 6;

    // Sağ kenara hizalı; taşarsa içeri çekiliyor.
    this.left = Math.max(8, Math.min(rect.right - width, window.innerWidth - width - 8));

    const below = rect.bottom + gap;
    this.top =
      below + estimatedHeight > window.innerHeight
        ? Math.max(8, rect.top - gap - estimatedHeight)
        : below;
  }
}
