import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SharedModule } from '../../modules/shared.module';
import { HttpService } from '../../services/http.service';
import { AuthService } from '../../services/auth.service';
import { SwalService } from '../../services/swal.service';
import { NoCompanyComponent } from '../ui/no-company/no-company.component';
import { PaymentDialogComponent } from '../ui/payment-dialog/payment-dialog.component';
import {
  CompanyProfileModel,
  ContactModel,
  InvoiceLineModel,
  InvoiceModel,
  currencySymbol,
} from '../../models/accounting.model';

/**
 * Tek faturanın belge görünümü.
 *
 * Yazdırılabilir olması gerekiyor: ön muhasebede fatura çoğu zaman kâğıda
 * dökülüp müşteriye veriliyor. Ekrandaki araç çubuğu ve menü yazdırmada
 * gizleniyor, belge tek başına kalıyor.
 */
@Component({
  selector: 'app-invoice-detail',
  standalone: true,
  imports: [SharedModule, NoCompanyComponent, PaymentDialogComponent],
  templateUrl: './invoice-detail.component.html',
  styleUrl: './invoice-detail.component.css',
})
export class InvoiceDetailComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpService);
  private readonly swal = inject(SwalService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  invoiceId = '';
  invoice: InvoiceModel | null = null;
  contact: ContactModel | null = null;

  /**
   * Kendi firmamızın künyesi. Belgede yalnızca karşı taraf yazıyordu; bir
   * faturanın kimin kestiğini söylememesi kâğıda basılınca eksik kalıyor.
   */
  company: CompanyProfileModel | null = null;

  loading = true;

  paymentOpen = false;

  ngOnInit(): void {
    if (!this.auth.hasCompany) {
      this.loading = false;
      return;
    }

    this.loadCompany();

    this.route.params.subscribe((params) => {
      this.invoiceId = params['id'];
      this.load();
    });
  }

  private loadCompany(): void {
    this.http.post<CompanyProfileModel>(
      'Companies/GetProfile',
      {},
      (res) => (this.company = res),
      () => (this.company = null)
    );
  }

  load(): void {
    this.loading = true;

    this.http.post<InvoiceModel>(
      'Invoices/GetById',
      { id: this.invoiceId },
      (res) => {
        this.invoice = res;
        this.loading = false;
        this.loadContact(res.contactId);
      },
      () => (this.loading = false)
    );
  }

  get symbol(): string {
    return currencySymbol(this.invoice?.currencyName ?? 'TL');
  }

  /** KDV oranı başına matrah; faturanın altındaki döküm. */
  get vatBreakdown(): { rate: number; base: number; vat: number }[] {
    const map = new Map<number, { rate: number; base: number; vat: number }>();

    for (const line of this.invoice?.lines ?? []) {
      const row = map.get(line.vatRate) ?? { rate: line.vatRate, base: 0, vat: 0 };

      row.base += line.lineTotal;
      row.vat += line.vatAmount;
      map.set(line.vatRate, row);
    }

    return [...map.values()].sort((a, b) => a.rate - b.rate);
  }

  lineTotal(line: InvoiceLineModel): number {
    return line.lineTotal + line.vatAmount;
  }

  print(): void {
    window.print();
  }

  openPayment(): void {
    this.paymentOpen = true;
  }

  get paymentDirection(): 0 | 1 {
    return this.invoice?.type === 1 ? 0 : 1;
  }

  deleteInvoice(): void {
    if (!this.invoice) return;

    this.swal.callSwal(
      'Faturayı sil?',
      `${this.invoice.number} silinecek. Cari bakiyesi ve stok da geri alınır.`,
      () => {
        this.http.post<string>('Invoices/DeleteById', { id: this.invoiceId }, (res) => {
          this.swal.callToast(res, 'info');
          this.router.navigate(['/invoices']);
        });
      }
    );
  }

  statusClass(): string {
    if (!this.invoice) return 'is-open';
    if (this.invoice.status === 3) return 'is-paid';
    if (this.invoice.isOverdue) return 'is-overdue';
    if (this.invoice.status === 2) return 'is-partial';
    return 'is-open';
  }

  private loadContact(contactId: string): void {
    this.http.post<ContactModel>('Contacts/GetById', { id: contactId }, (res) => {
      this.contact = res;
    });
  }
}
