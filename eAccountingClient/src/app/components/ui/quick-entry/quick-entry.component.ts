import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpService } from '../../../services/http.service';
import { SwalService } from '../../../services/swal.service';
import { ModalComponent } from '../modal/modal.component';
import { QuickAccount } from './quick-entry.model';
import { CategoryModel } from '../../../models/category.model';

/** 0 = para girişi, 1 = para çıkışı. Sunucudaki Type alanıyla aynı. */
type Direction = 0 | 1;

/**
 * Ana sayfadan tek ekranda hareket girme.
 *
 * Uygulamanın geri kalanı hesap türüne göre kurulu: önce kasa mı banka mı
 * olduğuna karar verip o sayfaya gitmek, sonra hareket eklemek gerekiyor.
 * Muhasebeci olmayan biri ise paranın yönünü düşünüyor - girdi mi, çıktı mı.
 * Burası o soruyla başlıyor, hesabı ikinci adımda listeden seçtiriyor.
 */
@Component({
  selector: 'app-quick-entry',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent],
  templateUrl: './quick-entry.component.html',
  styleUrl: './quick-entry.component.css',
  providers: [DatePipe],
})
export class QuickEntryComponent {
  @Input() accounts: QuickAccount[] = [];
  @Input() categories: CategoryModel[] = [];
  @Output() saved = new EventEmitter<void>();

  private readonly http = inject(HttpService);
  private readonly swal = inject(SwalService);
  private readonly date = inject(DatePipe);

  open = false;
  direction: Direction = 0;
  saving = false;

  amountText = '';
  accountId = '';
  description = '';
  entryDate = '';
  categoryId = '';

  get title(): string {
    return this.direction === 0 ? 'Para Girişi' : 'Para Çıkışı';
  }

  /** Yalnızca seçilen yöne ait kalemler; gelir girerken gider kalemi çıkmasın. */
  get availableCategories(): CategoryModel[] {
    return this.categories.filter((c) => c.direction === this.direction);
  }

  get selected(): QuickAccount | undefined {
    return this.accounts.find((a) => a.id === this.accountId);
  }

  /** Yazılan tutarın sayı karşılığı; geçersizse 0. */
  get amount(): number {
    return QuickEntryComponent.parseAmount(this.amountText);
  }

  get canSave(): boolean {
    return this.amount > 0 && !!this.accountId && !this.saving;
  }

  start(direction: Direction): void {
    this.direction = direction;
    this.amountText = '';
    this.description = '';
    this.categoryId = '';
    this.entryDate = this.date.transform(new Date(), 'yyyy-MM-dd') ?? '';

    // Tek hesap varsa seçtirmenin anlamı yok.
    this.accountId = this.accounts.length === 1 ? this.accounts[0].id : '';

    this.open = true;
  }

  pick(account: QuickAccount): void {
    this.accountId = account.id;
  }

  save(): void {
    if (!this.canSave) return;

    const account = this.selected;
    if (!account) return;

    this.saving = true;

    const isCash = account.kind === 'Kasa';
    const url = isCash ? 'CashRegisterDetails/Create' : 'BankDetails/Create';

    const body: Record<string, unknown> = {
      date: this.entryDate,
      type: this.direction,
      amount: this.amount,
      // Virman değil, sıradan bir hareket.
      recordType: 0,
      oppositeAmount: 0,
      description: this.description.trim() || this.title,
      categoryId: this.categoryId || null,
    };

    if (isCash) {
      body['cashRegisterId'] = account.id;
      body['oppositeCashRegisterId'] = null;
    } else {
      body['bankId'] = account.id;
      body['oppositeBankId'] = null;
    }

    this.http.post<string>(
      url,
      body,
      () => {
        this.saving = false;
        this.open = false;
        this.swal.callToast(
          `${account.name}: ${this.direction === 0 ? 'giriş' : 'çıkış'} kaydedildi.`
        );
        this.saved.emit();
      },
      () => (this.saving = false)
    );
  }

  /**
   * "1.250,75" ve "1250.75" gibi yazımların ikisini de kabul eder: binlik
   * ayracı atılır, ondalık ayracı noktaya çevrilir.
   */
  private static parseAmount(text: string): number {
    const trimmed = (text ?? '').trim();
    if (!trimmed) return 0;

    const normalized = trimmed.includes(',')
      ? trimmed.replace(/\./g, '').replace(',', '.')
      : trimmed.replace(/\.(?=\d{3}\b)/g, '');

    const value = Number(normalized);

    return Number.isFinite(value) && value > 0 ? value : 0;
  }
}
