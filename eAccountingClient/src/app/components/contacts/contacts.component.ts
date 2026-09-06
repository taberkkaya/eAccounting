import { Component, OnInit, inject } from '@angular/core';
import { NgForm } from '@angular/forms';
import { SharedModule } from '../../modules/shared.module';
import { HttpService } from '../../services/http.service';
import { AuthService } from '../../services/auth.service';
import { SwalService } from '../../services/swal.service';
import { NoCompanyComponent } from '../ui/no-company/no-company.component';
import { ActionMenuComponent } from '../ui/action-menu/action-menu.component';
import { PaymentDialogComponent } from '../ui/payment-dialog/payment-dialog.component';
import {
  ContactFormModel,
  ContactModel,
  ContactTypeValue,
  CurrencyOptions,
  currencySymbol,
} from '../../models/accounting.model';

/**
 * Cari hesaplar: müşteriler ve tedarikçiler tek listede.
 *
 * Ayrı iki ekran yapmadık çünkü aynı firma çoğu zaman ikisi birden oluyor ve
 * bakiyesi tek. Tür burada bir filtre; kayıt bölünmüyor.
 */
@Component({
  selector: 'app-contacts',
  standalone: true,
  imports: [
    SharedModule,
    NoCompanyComponent,
    ActionMenuComponent,
    PaymentDialogComponent,
  ],
  templateUrl: './contacts.component.html',
  styleUrl: './contacts.component.css',
})
export class ContactsComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpService);
  private readonly swal = inject(SwalService);

  readonly currencies = CurrencyOptions;

  contacts: ContactModel[] = [];
  loading = true;

  typeFilter: '' | 1 | 2 = '';
  search = '';
  onlyWithBalance = false;

  createOpen = false;
  updateOpen = false;
  createModel = new ContactFormModel();
  updateModel = new ContactFormModel();

  paymentOpen = false;
  paymentContact: ContactModel | null = null;
  paymentDirection: 0 | 1 = 0;

  ngOnInit(): void {
    if (!this.auth.hasCompany) {
      this.loading = false;
      return;
    }

    this.getAll();
  }

  getAll(): void {
    this.loading = true;

    this.http.post<ContactModel[]>(
      'Contacts/GetAll',
      {
        type: this.typeFilter === '' ? null : this.typeFilter,
        search: this.search.trim() || null,
        onlyWithBalance: this.onlyWithBalance,
      },
      (res) => {
        this.contacts = res;
        this.loading = false;
      },
      () => (this.loading = false)
    );
  }

  setType(value: '' | 1 | 2): void {
    this.typeFilter = value;
    this.getAll();
  }

  toggleOnlyWithBalance(): void {
    this.onlyWithBalance = !this.onlyWithBalance;
    this.getAll();
  }

  /** Ekranın üstündeki toplamlar; para birimi karışmasın diye ayrı ayrı. */
  get totals(): { currency: string; receivable: number; payable: number }[] {
    const map = new Map<string, { currency: string; receivable: number; payable: number }>();

    for (const contact of this.contacts) {
      const row = map.get(contact.currencyName) ?? {
        currency: contact.currencyName,
        receivable: 0,
        payable: 0,
      };

      if (contact.balance > 0) row.receivable += contact.balance;
      else row.payable += -contact.balance;

      map.set(contact.currencyName, row);
    }

    return [...map.values()];
  }

  get counts(): { all: number; customer: number; supplier: number } {
    return {
      all: this.contacts.length,
      customer: this.contacts.filter((p) => p.type === 1 || p.type === 3).length,
      supplier: this.contacts.filter((p) => p.type === 2 || p.type === 3).length,
    };
  }

  openCreate(type: ContactTypeValue): void {
    this.createModel = new ContactFormModel();
    this.createModel.type = type;
    this.createOpen = true;
  }

  create(form: NgForm): void {
    if (!form.valid) return;

    this.http.post<string>(
      'Contacts/Create',
      { ...this.createModel, ...this.trimmed(this.createModel) },
      (res) => {
        this.swal.callToast(res);
        this.createOpen = false;
        this.getAll();
      }
    );
  }

  openUpdate(contact: ContactModel): void {
    this.updateModel = {
      id: contact.id,
      name: contact.name,
      type: contact.type,
      currencyTypeValue: contact.currencyTypeValue,
      taxNumber: contact.taxNumber ?? '',
      taxOffice: contact.taxOffice ?? '',
      phone: contact.phone ?? '',
      email: contact.email ?? '',
      address: contact.address ?? '',
      note: contact.note ?? '',
      openingBalance: 0,
    };

    this.updateOpen = true;
  }

  update(form: NgForm): void {
    if (!form.valid) return;

    this.http.post<string>(
      'Contacts/Update',
      { ...this.updateModel, ...this.trimmed(this.updateModel) },
      (res) => {
        this.swal.callToast(res);
        this.updateOpen = false;
        this.getAll();
      }
    );
  }

  deleteById(contact: ContactModel): void {
    this.swal.callSwal(
      'Cariyi sil?',
      `"${contact.name}" silinecek. Ekstresi de görünmez olur.`,
      () => {
        this.http.post<string>('Contacts/DeleteById', { id: contact.id }, (res) => {
          this.swal.callToast(res, 'info');
          this.getAll();
        });
      }
    );
  }

  openPayment(contact: ContactModel, direction: 0 | 1): void {
    this.paymentContact = contact;
    this.paymentDirection = direction;
    this.paymentOpen = true;
  }

  symbolFor(name: string): string {
    return currencySymbol(name);
  }

  /**
   * Boş metin alanları sunucuya null gitsin; boş dize ile null arasındaki farkı
   * veritabanında görmenin bir faydası yok.
   */
  private trimmed(model: ContactFormModel) {
    const blankToNull = (value: string) => (value.trim() ? value.trim() : null);

    return {
      taxNumber: blankToNull(model.taxNumber),
      taxOffice: blankToNull(model.taxOffice),
      phone: blankToNull(model.phone),
      email: blankToNull(model.email),
      address: blankToNull(model.address),
      note: blankToNull(model.note),
    };
  }
}
