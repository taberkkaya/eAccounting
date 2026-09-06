import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { SharedModule } from '../../modules/shared.module';
import { HttpService } from '../../services/http.service';
import { AuthService } from '../../services/auth.service';
import { SwalService } from '../../services/swal.service';
import { NoCompanyComponent } from '../ui/no-company/no-company.component';
import { ActionMenuComponent } from '../ui/action-menu/action-menu.component';
import { PaymentDialogComponent } from '../ui/payment-dialog/payment-dialog.component';
import {
  ContactModel,
  InvoiceModel,
  currencySymbol,
} from '../../models/accounting.model';

/** Açılış aralığı: bugünden geriye kaç gün. */
const DEFAULT_RANGE_DAYS = 90;

/**
 * Satış ve alış faturaları tek listede.
 *
 * Tür bir filtre, ayrı bir sayfa değil: "bu ay ne kestim, ne aldım" sorusu tek
 * ekranda cevaplanmalı. Vadesi geçenler ayrıca işaretli, çünkü listeye bakma
 * sebebi çoğu zaman o.
 */
@Component({
  selector: 'app-invoices',
  standalone: true,
  imports: [
    SharedModule,
    NoCompanyComponent,
    ActionMenuComponent,
    PaymentDialogComponent,
  ],
  templateUrl: './invoices.component.html',
  styleUrl: './invoices.component.css',
  providers: [DatePipe],
})
export class InvoicesComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpService);
  private readonly swal = inject(SwalService);
  private readonly date = inject(DatePipe);

  invoices: InvoiceModel[] = [];
  contacts: ContactModel[] = [];
  loading = true;

  typeFilter: '' | 1 | 2 = '';
  contactId = '';
  statusFilter = '';
  onlyOverdue = false;
  startDate = '';
  endDate = '';
  search = '';

  paymentOpen = false;
  paymentContact: ContactModel | null = null;
  paymentInvoice: InvoiceModel | null = null;
  paymentDirection: 0 | 1 = 0;

  ngOnInit(): void {
    if (!this.auth.hasCompany) {
      this.loading = false;
      return;
    }

    const today = new Date();
    const from = new Date();
    from.setDate(today.getDate() - DEFAULT_RANGE_DAYS);

    this.startDate = this.format(from);
    this.endDate = this.format(today);

    this.loadContacts();
    this.getAll();
  }

  getAll(): void {
    this.loading = true;

    this.http.post<InvoiceModel[]>(
      'Invoices/GetAll',
      {
        type: this.typeFilter === '' ? null : this.typeFilter,
        contactId: this.contactId || null,
        startDate: this.startDate || null,
        endDate: this.endDate || null,
        status: this.statusFilter === '' ? null : +this.statusFilter,
        onlyOverdue: this.onlyOverdue,
        search: this.search.trim() || null,
        take: 500,
      },
      (res) => {
        this.invoices = res;
        this.loading = false;
      },
      () => (this.loading = false)
    );
  }

  setType(value: '' | 1 | 2): void {
    this.typeFilter = value;
    this.getAll();
  }

  toggleOverdue(): void {
    this.onlyOverdue = !this.onlyOverdue;
    this.getAll();
  }

  clear(): void {
    this.typeFilter = '';
    this.contactId = '';
    this.statusFilter = '';
    this.onlyOverdue = false;
    this.search = '';
    this.getAll();
  }

  get counts(): { all: number; sales: number; purchase: number; overdue: number } {
    return {
      all: this.invoices.length,
      sales: this.invoices.filter((p) => p.type === 1).length,
      purchase: this.invoices.filter((p) => p.type === 2).length,
      overdue: this.invoices.filter((p) => p.isOverdue).length,
    };
  }

  /** Listeyle aynı filtrede kesilen ve kalan tutar, para birimi başına. */
  get totals(): { currency: string; total: number; remaining: number }[] {
    const map = new Map<string, { currency: string; total: number; remaining: number }>();

    for (const invoice of this.invoices) {
      const row = map.get(invoice.currencyName) ?? {
        currency: invoice.currencyName,
        total: 0,
        remaining: 0,
      };

      row.total += invoice.grandTotal;
      row.remaining += invoice.remainingAmount;
      map.set(invoice.currencyName, row);
    }

    return [...map.values()];
  }

  /** Tahsilat mı ödeme mi: satış faturasından tahsil edilir, alıştan ödenir. */
  openPayment(invoice: InvoiceModel): void {
    const contact = this.contacts.find((c) => c.id === invoice.contactId);
    if (!contact) return;

    this.paymentContact = contact;
    this.paymentInvoice = invoice;
    this.paymentDirection = invoice.type === 1 ? 0 : 1;
    this.paymentOpen = true;
  }

  deleteById(invoice: InvoiceModel): void {
    this.swal.callSwal(
      'Faturayı sil?',
      `${invoice.number} silinecek. Cari bakiyesi ve stok da geri alınır.`,
      () => {
        this.http.post<string>('Invoices/DeleteById', { id: invoice.id }, (res) => {
          this.swal.callToast(res, 'info');
          this.getAll();
        });
      }
    );
  }

  statusClass(invoice: InvoiceModel): string {
    if (invoice.status === 3) return 'is-paid';
    if (invoice.isOverdue) return 'is-overdue';
    if (invoice.status === 2) return 'is-partial';
    return 'is-open';
  }

  symbolFor(name: string): string {
    return currencySymbol(name);
  }

  private loadContacts(): void {
    this.http.post<ContactModel[]>('Contacts/GetAll', {}, (res) => (this.contacts = res));
  }

  private format(value: Date): string {
    return this.date.transform(value, 'yyyy-MM-dd') ?? '';
  }
}
