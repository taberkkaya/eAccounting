import {
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  Output,
  ViewChild,
  forwardRef,
  inject,
} from '@angular/core';
import { NG_VALUE_ACCESSOR, ControlValueAccessor, FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

/** Listedeki bir seçenek. <c>hint</c> ikinci satırda küçük yazılır. */
export interface ComboOption {
  value: string;
  label: string;
  hint?: string;
}

/**
 * Aranabilir seçim kutusu.
 *
 * Sıradan bir <c>select</c> yirmi kayda kadar iş görüyor; iki yüz cariye
 * çıkınca fare tekerleğiyle isim aramaya dönüşüyor. Bu kutu yazdıkça süzüyor
 * ve klavyeyle gezilebiliyor.
 *
 * Açılan liste <c>position: fixed</c>: fatura satırları yatay kayan bir tablonun
 * içinde ve normal konumlandırılmış bir panel o kutunun kenarında kırpılırdı.
 */
@Component({
  selector: 'app-combobox',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './combobox.component.html',
  styleUrl: './combobox.component.css',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => ComboboxComponent),
      multi: true,
    },
  ],
})
export class ComboboxComponent implements ControlValueAccessor {
  @Input() options: ComboOption[] = [];
  @Input() placeholder = 'Seçin...';

  /** Boş seçeneğin etiketi. Boş bırakılırsa boş seçenek gösterilmez. */
  @Input() emptyLabel = '';

  @Input() disabled = false;

  @Output() valueChange = new EventEmitter<string>();

  @ViewChild('trigger') trigger?: ElementRef<HTMLButtonElement>;
  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;

  private readonly host = inject(ElementRef<HTMLElement>);

  value = '';
  open = false;
  search = '';
  activeIndex = 0;

  top = 0;
  left = 0;
  width = 240;

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  // --- ControlValueAccessor ------------------------------------------------

  writeValue(value: string | null): void {
    this.value = value ?? '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  // --- görüntüleme ---------------------------------------------------------

  get selected(): ComboOption | null {
    return this.options.find((option) => option.value === this.value) ?? null;
  }

  get display(): string {
    if (this.selected) return this.selected.label;

    return this.value ? '' : this.emptyLabel || this.placeholder;
  }

  get isPlaceholder(): boolean {
    return !this.selected;
  }

  /**
   * Aksanlı harfleri ve büyük-küçük farkını yok sayarak süzer. "sirket" yazan
   * "Şirket"i bulabilmeli; Türkçede aksansız yazmak yaygın.
   */
  get filtered(): ComboOption[] {
    const term = normalize(this.search);
    if (!term) return this.options;

    return this.options.filter(
      (option) =>
        normalize(option.label).includes(term) || normalize(option.hint ?? '').includes(term)
    );
  }

  // --- açma / kapama -------------------------------------------------------

  toggle(event: MouseEvent): void {
    event.stopPropagation();
    if (this.disabled) return;

    if (this.open) {
      this.close();
      return;
    }

    this.open = true;
    this.search = '';
    this.activeIndex = Math.max(0, this.filtered.findIndex((o) => o.value === this.value));
    this.place();

    // Panel çizildikten sonra odak arama kutusuna geçsin.
    setTimeout(() => this.searchInput?.nativeElement.focus(), 0);
  }

  close(): void {
    this.open = false;
    this.onTouched();
  }

  pick(option: ComboOption | null): void {
    this.value = option?.value ?? '';
    this.onChange(this.value);
    this.valueChange.emit(this.value);
    this.close();
    this.trigger?.nativeElement.focus();
  }

  onSearchChange(): void {
    this.activeIndex = 0;
  }

  // --- klavye --------------------------------------------------------------

  onTriggerKeydown(event: KeyboardEvent): void {
    if (this.open) return;

    if (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.toggle(new MouseEvent('click'));
    }
  }

  onSearchKeydown(event: KeyboardEvent): void {
    const list = this.filtered;

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.activeIndex = Math.min(this.activeIndex + 1, list.length - 1);
      this.scrollActiveIntoView();
      return;
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.activeIndex = Math.max(this.activeIndex - 1, 0);
      this.scrollActiveIntoView();
      return;
    }

    if (event.key === 'Enter') {
      event.preventDefault();
      if (list[this.activeIndex]) this.pick(list[this.activeIndex]);
      return;
    }

    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
      this.trigger?.nativeElement.focus();
    }
  }

  private scrollActiveIntoView(): void {
    setTimeout(() => {
      this.host.nativeElement.ownerDocument
        ?.querySelector('.combo__option.is-active')
        ?.scrollIntoView({ block: 'nearest' });
    }, 0);
  }

  // --- dış olaylar ---------------------------------------------------------

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.open) return;

    const target = event.target as Node;
    if (this.host.nativeElement.contains(target)) return;
    if (document.querySelector('.combo__panel')?.contains(target)) return;

    this.close();
  }

  // Sabit konumlandırıldığı için sayfa kayınca düğmeden kopuyor; kapatmak
  // yeniden hesaplamaktan daha dürüst.
  @HostListener('window:scroll')
  @HostListener('window:resize')
  onViewportChange(): void {
    if (this.open) this.close();
  }

  private place(): void {
    const button = this.trigger?.nativeElement;
    if (!button) return;

    const rect = button.getBoundingClientRect();
    const estimatedHeight = 300;
    const gap = 4;

    this.width = Math.max(rect.width, 220);
    this.left = Math.max(8, Math.min(rect.left, window.innerWidth - this.width - 8));

    const below = rect.bottom + gap;
    this.top =
      below + estimatedHeight > window.innerHeight && rect.top > estimatedHeight
        ? Math.max(8, rect.top - gap - estimatedHeight)
        : below;
  }
}

/** Karşılaştırma için sadeleştirir: küçük harf, aksansız. */
function normalize(value: string): string {
  return value
    .toLocaleLowerCase('tr')
    .replace(/ı/g, 'i')
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '');
}
