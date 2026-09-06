import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  inject,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { SharedModule } from '../../../modules/shared.module';
import { HttpService } from '../../../services/http.service';
import { SwalService } from '../../../services/swal.service';
import { CashRegisterModel } from '../../../models/cashRegister.model';
import { BankModel } from '../../../models/bank.model';
import {
  ContactModel,
  InvoiceModel,
  currencySymbol,
} from '../../../models/accounting.model';

/** Ödemenin gireceği hesap; kasa ve banka aynı listede. */
interface PaymentAccount {
  id: string;
  name: string;
  kind: 'Kasa' | 'Banka';
  currencyName: string;
}

/**
 * Tahsilat ve ödeme penceresi.
 *
 * Cari listesinden, cari ekstresinden ve fatura listesinden aynı pencere
 * açılıyor. Üç yerde üç ayrı form olsaydı biri er geç para birimi kontrolünü
 * ya da faturaya bağlamayı atlardı.
 */
@Component({
  selector: 'app-payment-dialog',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './payment-dialog.component.html',
  styleUrl: './payment-dialog.component.css',
  providers: [DatePipe],
})
export class PaymentDialogComponent implements OnChanges {
  private readonly http = inject(HttpService);
  private readonly swal = inject(SwalService);
  private readonly date = inject(DatePipe);

  @Input() open = false;

  /** İşlemin yapılacağı cari. Pencere bunsuz açılmaz. */
  @Input() contact: ContactModel | null = null;

  /** Verilirse tutar faturanın kalanıyla sınırlanır ve faturaya sayılır. */
  @Input() invoice: InvoiceModel | null = null;

  /** 0 tahsilat, 1 ödeme. */
  @Input() direction: 0 | 1 = 0;

  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  accounts: PaymentAccount[] = [];
  accountId = '';
  amountText = '';
  description = '';
  entryDate = '';
  saving = false;

  /** Kaç hesap listesinin dönmesi beklendiği; ön seçim ikisi de gelince yapılıyor. */
  private pendingLoads = 0;

  /**
   * Pencere açılınca kendi kendini hazırlıyor.
   *
   * Bunu ana bileşene bırakmak, üç ayrı ekranın üçünün de aynı çağrıyı
   * hatırlamasına bağlı olurdu; biri unutulunca pencere boş açılıyor.
   */
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open'] && this.open) this.start();
  }

  /** Hesaplar her açılışta tazeleniyor: araya yeni bir kasa eklenmiş olabilir. */
  private start(): void {
    this.entryDate = this.date.transform(new Date(), 'yyyy-MM-dd') ?? '';
    this.accountId = '';
    this.description = '';
    this.amountText = this.invoice
      ? this.invoice.remainingAmount.toFixed(2).replace('.', ',')
      : '';

    this.loadAccounts();
  }

  get title(): string {
    const subject = this.direction === 0 ? 'Tahsilat' : 'Ödeme';

    return this.invoice ? `${subject} - ${this.invoice.number}` : subject;
  }

  /** Carinin para birimiyle uyuşmayan hesap seçilemesin; sunucu da reddediyor. */
  get eligibleAccounts(): PaymentAccount[] {
    if (!this.contact) return [];

    return this.accounts.filter((a) => a.currencyName === this.contact!.currencyName);
  }

  get symbol(): string {
    return currencySymbol(this.contact?.currencyName ?? 'TL');
  }

  get amount(): number {
    return parseTurkishAmount(this.amountText);
  }

  get canSave(): boolean {
    return !this.saving && !!this.accountId && this.amount > 0;
  }

  save(): void {
    if (!this.canSave || !this.contact) return;

    this.saving = true;

    this.http.post<string>(
      'Payments/Create',
      {
        contactId: this.contact.id,
        accountId: this.accountId,
        direction: this.direction,
        date: this.entryDate,
        amount: this.amount,
        description: this.description.trim() || null,
        invoiceId: this.invoice?.id ?? null,
      },
      (res) => {
        this.swal.callToast(res);
        this.saving = false;
        this.saved.emit();
        this.closed.emit();
      },
      () => (this.saving = false)
    );
  }

  private loadAccounts(): void {
    this.accounts = [];
    this.pendingLoads = 2;

    this.http.post<CashRegisterModel[]>('CashRegisters/GetAll', {}, (res) => {
      this.accounts = [
        ...this.accounts,
        ...res.map((a) => ({
          id: a.id,
          name: a.name,
          kind: 'Kasa' as const,
          currencyName: a.currencyType.name,
        })),
      ];

      this.preselect();
    });

    this.http.post<BankModel[]>('Banks/GetAll', {}, (res) => {
      this.accounts = [
        ...this.accounts,
        ...res.map((a) => ({
          id: a.id,
          name: a.name,
          kind: 'Banka' as const,
          currencyName: a.currencyType.name,
        })),
      ];

      this.preselect();
    });
  }

  /**
   * Tek uygun hesap varsa seçmeye zorlamanın anlamı yok. Kasa ve banka ayrı
   * isteklerle geldiği için ikisi de dönmeden karar verilmiyor: yoksa yalnızca
   * kasa gelmişken "tek hesap" sanılıp yanlış hesap seçili kalıyor.
   */
  private preselect(): void {
    if (--this.pendingLoads > 0) return;

    const eligible = this.eligibleAccounts;

    if (eligible.length === 1) this.accountId = eligible[0].id;
  }
}

/**
 * "1.250,50" ve "1250.50" ikisini de okur. Virgül varsa Türkçe biçim kabul
 * edilir; yoksa nokta yalnızca binlik ayracıysa atılır.
 */
export function parseTurkishAmount(value: string): number {
  const trimmed = (value ?? '').trim();
  if (!trimmed) return 0;

  const normalized = trimmed.includes(',')
    ? trimmed.replace(/\./g, '').replace(',', '.')
    : trimmed.replace(/\.(?=\d{3}\b)/g, '');

  const parsed = Number(normalized);

  return Number.isFinite(parsed) ? parsed : 0;
}
