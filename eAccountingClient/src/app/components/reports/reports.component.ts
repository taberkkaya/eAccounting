import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { SharedModule } from '../../modules/shared.module';
import { HttpService } from '../../services/http.service';
import { AuthService } from '../../services/auth.service';
import { NoCompanyComponent } from '../ui/no-company/no-company.component';
import {
  AgingReportModel,
  ProfitLossModel,
  VatReportModel,
  currencySymbol,
} from '../../models/accounting.model';

type Tab = 'aging' | 'vat' | 'profit';

const MONTH_NAMES = [
  'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
  'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık',
];

/**
 * Muhasebeciye giderken sorulan üç soru: kim ne kadar geciktirdi, bu dönem ne
 * kadar KDV çıktı, kâr ettik mi.
 *
 * Üçü tek ekranda sekmeli duruyor çünkü üçü de aynı anda bakılan şeyler ve her
 * biri için ayrı sayfa açmak menüyü şişirirdi.
 */
@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [SharedModule, NoCompanyComponent],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.css',
  providers: [DatePipe],
})
export class ReportsComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpService);
  private readonly date = inject(DatePipe);

  tab: Tab = 'aging';
  loading = false;

  agingType: 1 | 2 = 1;
  aging: AgingReportModel | null = null;

  startDate = '';
  endDate = '';
  vat: VatReportModel | null = null;
  profit: ProfitLossModel | null = null;

  ngOnInit(): void {
    if (!this.auth.hasCompany) return;

    const today = new Date();
    const yearStart = new Date(today.getFullYear(), 0, 1);

    this.startDate = this.format(yearStart);
    this.endDate = this.format(today);

    this.loadAging();
  }

  setTab(tab: Tab): void {
    this.tab = tab;
    this.load();
  }

  load(): void {
    if (this.tab === 'aging') this.loadAging();
    else if (this.tab === 'vat') this.loadVat();
    else this.loadProfit();
  }

  setAgingType(type: 1 | 2): void {
    this.agingType = type;
    this.loadAging();
  }

  private loadAging(): void {
    this.loading = true;

    this.http.post<AgingReportModel>(
      'Reports/Aging',
      { type: this.agingType },
      (res) => {
        this.aging = res;
        this.loading = false;
      },
      () => (this.loading = false)
    );
  }

  private loadVat(): void {
    this.loading = true;

    this.http.post<VatReportModel>(
      'Reports/Vat',
      { startDate: this.startDate, endDate: this.endDate },
      (res) => {
        this.vat = res;
        this.loading = false;
      },
      () => (this.loading = false)
    );
  }

  private loadProfit(): void {
    this.loading = true;

    this.http.post<ProfitLossModel>(
      'Reports/ProfitLoss',
      { startDate: this.startDate, endDate: this.endDate },
      (res) => {
        this.profit = res;
        this.loading = false;
      },
      () => (this.loading = false)
    );
  }

  /** Gider dağılımındaki çubuk uzunluğu; en büyük kalem tam genişlik. */
  expenseWidth(amount: number): string {
    const max = Math.max(...(this.profit?.expenseByCategory ?? []).map((p) => p.amount), 1);

    return `${Math.max(2, (amount / max) * 100)}%`;
  }

  monthName(month: number): string {
    return MONTH_NAMES[month - 1] ?? '';
  }

  symbolFor(name: string): string {
    return currencySymbol(name);
  }

  print(): void {
    window.print();
  }

  private format(value: Date): string {
    return this.date.transform(value, 'yyyy-MM-dd') ?? '';
  }
}
