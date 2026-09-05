import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  Output,
} from '@angular/core';

/**
 * Bootstrap'in modalının yerini alır. Gövde ve alt butonlar içerik projeksiyonuyla
 * gelir; böylece form etiketi kaydet butonunu da kapsayabilir.
 */
@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [],
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.css',
})
export class ModalComponent {
  @Input() open = false;
  @Input() title = '';
  @Input() wide = false;

  @Output() closed = new EventEmitter<void>();

  close(): void {
    this.closed.emit();
  }

  /** Kutunun dışına tıklanınca kapanır, kutunun içine tıklama etkilenmez. */
  onBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.close();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open) this.close();
  }
}
