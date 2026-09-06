import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
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
  ContactStatementLineModel,
  ContactStatementModel,
  currencySymbol,
} from '../../models/accounting.model';

/** Açılış aralığı: bugünden geriye kaç gün. */
const DEFAULT_RANGE_DAYS = 180;

/**
 * Cari ekstresi.
 *
 * Ön muhasebede bu ekran mutabakat belgesidir: karşı tarafa gönderilip "sizde de
 * böyle mi" diye sorulur. Bu yüzden devreden bakiye, dönem toplamları ve kapanış
 * bakiyesi ekranda da, indirilen dosyada da aynı yerde duruyor.
 */
@Component({
  selector: 'app-contact-detail',
  standalone: true,
  imports: [
    SharedModule,
    NoCompanyComponent,
    ActionMenuComponent,
    PaymentDialogComponent,
  ],
  templateUrl: './contact-detail.component.html',
  styleUrl: './contact-detail.component.css',
  providers: [DatePipe],
})
export class ContactDetailComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpService);
  private readonly swal = inject(SwalService);
  private readonly route = inject(ActivatedRoute);
  private readonly date = inject(DatePipe);

  contactId = '';
  contact: ContactModel | null = null;
  statement: ContactStatementModel | null = null;
  loading = true;

  startDate = '';
  endDate = '';

  exporting: 'excel' | 'pdf' | null = null;

  paymentOpen = false;
  paymentDirection: 0 | 1 = 0;

  ngOnInit(): void {
    if (!this.auth.hasCompany) {
      this.loading = false;
      return;
    }

    this.route.params.subscribe((params) => {
      this.contactId = params['id'];

      const today = new Date();
      const from = new Date();
      from.setDate(today.getDate() - DEFAULT_RANGE_DAYS);

      this.startDate = this.format(from);
      this.endDate = this.format(today);

      this.loadContact();
      this.loadStatement();
    });
  }

  loadStatement(): void {
    this.loading = true;

    this.http.post<ContactStatementModel>(
      'Contacts/GetStatement',
      {
        contactId: this.contactId,
        startDate: this.startDate || null,
        endDate: this.endDate || null,
      },
      (res) => {
        this.statement = res;
        this.loading = false;
      },
      () => (this.loading = false)
    );
  }

  /** Ekstrenin başlığındaki bakiye ve gecikme bilgisi carinin kendisinden. */
  private loadContact(): void {
    this.http.post<ContactModel>('Contacts/GetById', { id: this.contactId }, (res) => {
      this.contact = res;
    });
  }

  refresh(): void {
    this.loadContact();
    this.loadStatement();
  }

  export(format: 'excel' | 'pdf'): void {
    this.exporting = format;

    this.http.download(
      'Contacts/ExportStatement',
      {
        contactId: this.contactId,
        startDate: this.startDate || null,
        endDate: this.endDate || null,
        format: format === 'pdf' ? 1 : 0,
      },
      `cari-ekstre.${format === 'pdf' ? 'pdf' : 'xlsx'}`,
      () => (this.exporting = null),
      () => (this.exporting = null)
    );
  }

  openPayment(direction: 0 | 1): void {
    this.paymentDirection = direction;
    this.paymentOpen = true;
  }

  /**
   * Yalnızca elle girilen hareketler silinebilir. Faturadan gelen satır burada
   * silinirse fatura ile ekstre çelişir; o yüzden faturaya yönlendiriyoruz.
   */
  canDelete(line: ContactStatementLineModel): boolean {
    return line.kind !== 1;
  }

  deleteLine(line: ContactStatementLineModel): void {
    this.swal.callSwal(
      'Hareketi sil?',
      `"${line.description}" silinecek. Kasa/banka tarafındaki karşılığı da geri alınır.`,
      () => {
        this.http.post<string>(
          'Payments/DeleteById',
          { contactTransactionId: line.id },
          (res) => {
            this.swal.callToast(res, 'info');
            this.refresh();
          }
        );
      }
    );
  }

  get symbol(): string {
    return currencySymbol(this.statement?.currencyName ?? this.contact?.currencyName ?? 'TL');
  }

  /** Bakiyenin hangi tarafta olduğunu yazıyla söylemek, işareti okumaktan kolay. */
  get balanceLabel(): string {
    const balance = this.contact?.balance ?? 0;

    if (balance > 0) return 'Bize borçlu (alacağımız)';
    if (balance < 0) return 'Biz ona borçluyuz';
    return 'Bakiye kapalı';
  }

  private format(value: Date): string {
    return this.date.transform(value, 'yyyy-MM-dd') ?? '';
  }
}
