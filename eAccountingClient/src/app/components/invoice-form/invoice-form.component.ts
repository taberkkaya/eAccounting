import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { SharedModule } from '../../modules/shared.module';
import { HttpService } from '../../services/http.service';
import { AuthService } from '../../services/auth.service';
import { SwalService } from '../../services/swal.service';
import { NoCompanyComponent } from '../ui/no-company/no-company.component';
import { ComboboxComponent, ComboOption } from '../ui/combobox/combobox.component';
import { CashRegisterModel } from '../../models/cashRegister.model';
import { BankModel } from '../../models/bank.model';
import {
  ContactModel,
  InvoiceLineModel,
  InvoiceModel,
  ProductModel,
  VatRates,
  currencySymbol,
} from '../../models/accounting.model';

/** Peşin ödeme için seçilebilecek hesap. */
interface PayAccount {
  id: string;
  name: string;
  kind: string;
  currencyName: string;
}

/** Fatura vadesi için hazır aralıklar; en sık kullanılanlar. */
const DUE_PRESETS = [
  { label: 'Peşin', days: 0 },
  { label: '15 gün', days: 15 },
  { label: '30 gün', days: 30 },
  { label: '60 gün', days: 60 },
];

/**
 * Fatura oluşturma ve düzenleme.
 *
 * Satır toplamları ekranda anında hesaplanıyor ama gönderilmiyor: sunucu kendi
 * hesabını yapıyor. Buradaki hesap kullanıcının ne imzaladığını görmesi için,
 * kaydedilecek rakam için değil.
 */
@Component({
  selector: 'app-invoice-form',
  standalone: true,
  imports: [SharedModule, NoCompanyComponent, ComboboxComponent],
  templateUrl: './invoice-form.component.html',
  styleUrl: './invoice-form.component.css',
  providers: [DatePipe],
})
export class InvoiceFormComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpService);
  private readonly swal = inject(SwalService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly date = inject(DatePipe);

  readonly vatRates = VatRates;
  readonly duePresets = DUE_PRESETS;

  invoiceId: string | null = null;
  type: 1 | 2 = 1;

  contacts: ContactModel[] = [];
  products: ProductModel[] = [];
  accounts: PayAccount[] = [];

  contactId = '';
  number = '';
  invoiceDate = '';
  dueDate = '';
  note = '';
  paidWithAccountId = '';
  lines: InvoiceLineModel[] = [];

  loading = true;
  saving = false;

  ngOnInit(): void {
    if (!this.auth.hasCompany) {
      this.loading = false;
      return;
    }

    this.loadContacts();
    this.loadProducts();
    this.loadAccounts();

    this.route.params.subscribe((params) => {
      this.invoiceId = params['id'] ?? null;

      if (this.invoiceId) this.loadInvoice();
      else this.startNew();
    });
  }

  get isEdit(): boolean {
    return this.invoiceId !== null;
  }

  get title(): string {
    const kind = this.type === 1 ? 'Satış Faturası' : 'Alış Faturası';

    return this.isEdit ? `${kind} Düzenle` : `Yeni ${kind}`;
  }

  /** Cari seçicideki satırlar; ipucu satırında vergi numarası ve bakiye. */
  get contactOptions(): ComboOption[] {
    return this.contacts.map((c) => ({
      value: c.id,
      label: c.name,
      hint: [c.typeName, c.taxNumber, c.currencyName].filter(Boolean).join(' · '),
    }));
  }

  /** Ürün seçicideki satırlar; fiyat ve stok ipucu satırında. */
  get productOptions(): ComboOption[] {
    const price = (p: ProductModel) =>
      (this.type === 1 ? p.salePrice : p.purchasePrice).toLocaleString('tr-TR', {
        minimumFractionDigits: 2,
      });

    return this.products.map((p) => ({
      value: p.id,
      label: p.name,
      hint: p.isService
        ? `Hizmet · ${price(p)} ${p.currencyName}`
        : `${price(p)} ${p.currencyName} · stok ${p.stockQuantity} ${p.unit}`,
    }));
  }

  get contact(): ContactModel | null {
    return this.contacts.find((c) => c.id === this.contactId) ?? null;
  }

  get currencyName(): string {
    return this.contact?.currencyName ?? 'TL';
  }

  get symbol(): string {
    return currencySymbol(this.currencyName);
  }

  /** Peşin ödeme yalnızca carinin para biriminde bir hesaba yapılabilir. */
  get eligibleAccounts(): PayAccount[] {
    return this.accounts.filter((a) => a.currencyName === this.currencyName);
  }

  // --- satır hesabı -------------------------------------------------------

  lineGross(line: InvoiceLineModel): number {
    return round(line.quantity * line.unitPrice);
  }

  lineDiscount(line: InvoiceLineModel): number {
    return round((this.lineGross(line) * line.discountRate) / 100);
  }

  lineNet(line: InvoiceLineModel): number {
    return this.lineGross(line) - this.lineDiscount(line);
  }

  lineVat(line: InvoiceLineModel): number {
    return round((this.lineNet(line) * line.vatRate) / 100);
  }

  lineTotal(line: InvoiceLineModel): number {
    return this.lineNet(line) + this.lineVat(line);
  }

  get subTotal(): number {
    return round(this.lines.reduce((sum, line) => sum + this.lineNet(line), 0));
  }

  get discountTotal(): number {
    return round(this.lines.reduce((sum, line) => sum + this.lineDiscount(line), 0));
  }

  get vatTotal(): number {
    return round(this.lines.reduce((sum, line) => sum + this.lineVat(line), 0));
  }

  get grandTotal(): number {
    return round(this.subTotal + this.vatTotal);
  }

  /** KDV oranı başına matrah; faturanın altındaki döküm. */
  get vatBreakdown(): { rate: number; base: number; vat: number }[] {
    const map = new Map<number, { rate: number; base: number; vat: number }>();

    for (const line of this.lines) {
      const row = map.get(line.vatRate) ?? { rate: line.vatRate, base: 0, vat: 0 };

      row.base += this.lineNet(line);
      row.vat += this.lineVat(line);
      map.set(line.vatRate, row);
    }

    return [...map.values()].sort((a, b) => a.rate - b.rate);
  }

  // --- satır düzenleme ----------------------------------------------------

  addLine(): void {
    const line = new InvoiceLineModel();
    line.id = `new-${Date.now()}-${this.lines.length}`;
    this.lines = [...this.lines, line];
  }

  removeLine(index: number): void {
    this.lines = this.lines.filter((_, i) => i !== index);
  }

  /**
   * Ürün seçilince adı, birimi, KDV'si ve fiyatı dolduruluyor. Satış faturası
   * satış fiyatını, alış faturası alış fiyatını alıyor.
   */
  onProductChange(line: InvoiceLineModel): void {
    const product = this.products.find((p) => p.id === line.productId);
    if (!product) return;

    line.description = product.name;
    line.unit = product.unit;
    line.vatRate = product.vatRate;
    line.unitPrice = this.type === 1 ? product.salePrice : product.purchasePrice;
  }

  setDue(days: number): void {
    if (!this.invoiceDate) return;

    const due = new Date(this.invoiceDate);
    due.setDate(due.getDate() + days);
    this.dueDate = this.format(due);
  }

  // --- kaydetme -----------------------------------------------------------

  get canSave(): boolean {
    return (
      !this.saving &&
      !!this.contactId &&
      this.lines.length > 0 &&
      this.lines.every((l) => l.description.trim() && l.quantity > 0)
    );
  }

  save(): void {
    if (!this.canSave) return;

    this.saving = true;

    const payload = {
      date: this.invoiceDate,
      dueDate: this.dueDate,
      note: this.note.trim() || null,
      lines: this.lines.map((line) => ({
        productId: line.productId || null,
        description: line.description.trim(),
        unit: line.unit || 'Adet',
        quantity: line.quantity,
        unitPrice: line.unitPrice,
        discountRate: line.discountRate,
        vatRate: line.vatRate,
      })),
    };

    if (this.isEdit) {
      this.http.post<string>(
        'Invoices/Update',
        { id: this.invoiceId, ...payload },
        (res) => {
          this.swal.callToast(res);
          this.router.navigate(['/invoices', this.invoiceId]);
        },
        () => (this.saving = false)
      );

      return;
    }

    this.http.post<string>(
      'Invoices/Create',
      {
        type: this.type,
        contactId: this.contactId,
        number: this.number.trim() || null,
        paidWithAccountId: this.paidWithAccountId || null,
        ...payload,
      },
      (res) => {
        this.swal.callToast(res);
        this.router.navigate(['/invoices']);
      },
      () => (this.saving = false)
    );
  }

  cancel(): void {
    this.router.navigate(this.isEdit ? ['/invoices', this.invoiceId] : ['/invoices']);
  }

  // --- yükleme ------------------------------------------------------------

  private startNew(): void {
    const params = this.route.snapshot.queryParamMap;
    const queryType = Number(params.get('type'));
    this.type = queryType === 2 ? 2 : 1;

    const today = new Date();
    this.invoiceDate = this.format(today);
    this.dueDate = this.format(today);
    this.lines = [];
    this.loading = false;

    // Cari adresten geldiyse önden seçili gelsin.
    const contactId = params.get('contactId');
    if (contactId) this.contactId = contactId;

    // Kopyalama: aynı müşteriye her ay aynı faturayı kesen biri satırları
    // yeniden yazmasın. Numara ve tarihler yeniden üretiliyor; kopyalanan
    // faturanın tahsilat durumu taşınmıyor.
    const copyFrom = params.get('copyFrom');

    if (copyFrom) {
      this.copyFrom(copyFrom);
      return;
    }

    this.addLine();
    this.loadNextNumber();
  }

  private copyFrom(invoiceId: string): void {
    this.loading = true;

    this.http.post<InvoiceModel>(
      'Invoices/GetById',
      { id: invoiceId },
      (res) => {
        this.type = res.type === 2 ? 2 : 1;
        this.contactId = res.contactId;
        this.note = res.note ?? '';
        this.lines = res.lines.map((line, index) => ({
          ...line,
          id: `copy-${index}`,
        }));

        this.loadNextNumber();
        this.loading = false;
      },
      () => {
        this.addLine();
        this.loadNextNumber();
        this.loading = false;
      }
    );
  }

  private loadInvoice(): void {
    this.http.post<InvoiceModel>(
      'Invoices/GetById',
      { id: this.invoiceId },
      (res) => {
        this.type = res.type === 2 ? 2 : 1;
        this.contactId = res.contactId;
        this.number = res.number;
        this.invoiceDate = res.date;
        this.dueDate = res.dueDate;
        this.note = res.note ?? '';
        this.lines = res.lines.map((line) => ({ ...line }));
        this.loading = false;
      },
      () => (this.loading = false)
    );
  }

  private loadNextNumber(): void {
    this.http.post<string>(
      'Invoices/GetNextNumber',
      { type: this.type },
      (res) => (this.number = res)
    );
  }

  private loadContacts(): void {
    this.http.post<ContactModel[]>('Contacts/GetAll', {}, (res) => (this.contacts = res));
  }

  private loadProducts(): void {
    this.http.post<ProductModel[]>('Products/GetAll', {}, (res) => (this.products = res));
  }

  private loadAccounts(): void {
    this.http.post<CashRegisterModel[]>('CashRegisters/GetAll', {}, (res) => {
      this.accounts = [
        ...this.accounts,
        ...res.map((a) => ({
          id: a.id,
          name: a.name,
          kind: 'Kasa',
          currencyName: a.currencyType.name,
        })),
      ];
    });

    this.http.post<BankModel[]>('Banks/GetAll', {}, (res) => {
      this.accounts = [
        ...this.accounts,
        ...res.map((a) => ({
          id: a.id,
          name: a.name,
          kind: 'Banka',
          currencyName: a.currencyType.name,
        })),
      ];
    });
  }

  private format(value: Date): string {
    return this.date.transform(value, 'yyyy-MM-dd') ?? '';
  }
}

function round(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}
