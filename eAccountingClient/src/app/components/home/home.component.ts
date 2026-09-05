import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { SharedModule } from '../../modules/shared.module';
import { DemoService } from '../../services/demo.service';
import { AuthService } from '../../services/auth.service';
import { HttpService } from '../../services/http.service';
import { CashRegisterModel } from '../../models/cashRegister.model';
import { BankModel } from '../../models/bank.model';
import { NoCompanyComponent } from '../ui/no-company/no-company.component';

type AccountKind = 'Kasa' | 'Banka';

interface AccountRow {
  name: string;
  kind: AccountKind;
  currency: string;
  deposit: number;
  withdrawal: number;
  balance: number;
}

interface CurrencySummary {
  currency: string;
  symbol: string;
  balance: number;
  deposit: number;
  withdrawal: number;
  accountCount: number;
}

/** Halka grafiğin çevresi; dilim uzunlukları buna oranlanıyor. */
const DONUT_CIRCUMFERENCE = 2 * Math.PI * 54;

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [SharedModule, NoCompanyComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent implements OnInit {
  readonly demo = inject(DemoService);
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpService);

  readonly loading = signal(true);
  readonly accounts = signal<AccountRow[]>([]);
  readonly selectedCurrency = signal<string>('');

  readonly donutCircumference = DONUT_CIRCUMFERENCE;

  ngOnInit(): void {
    // Firma yoksa bağlanılacak veritabanı da yok; istek atmak yalnızca hata
    // bildirimi üretirdi.
    if (!this.auth.hasCompany) {
      this.loading.set(false);
      return;
    }

    this.load();
  }

  /** Kasa ve banka listeleri tek bir hesap listesine indirgeniyor. */
  private load(): void {
    let pending = 2;
    let cashRegisters: CashRegisterModel[] = [];
    let banks: BankModel[] = [];

    const done = () => {
      if (--pending > 0) return;

      const rows: AccountRow[] = [
        ...cashRegisters.map((item) => this.toRow(item, 'Kasa')),
        ...banks.map((item) => this.toRow(item, 'Banka')),
      ];

      this.accounts.set(rows);
      this.selectedCurrency.set(this.defaultCurrency(rows));
      this.loading.set(false);
    };

    this.http.post<CashRegisterModel[]>(
      'CashRegisters/GetAll',
      {},
      (res) => {
        cashRegisters = res;
        done();
      },
      done
    );

    this.http.post<BankModel[]>(
      'Banks/GetAll',
      {},
      (res) => {
        banks = res;
        done();
      },
      done
    );
  }

  private toRow(
    item: CashRegisterModel | BankModel,
    kind: AccountKind
  ): AccountRow {
    return {
      name: item.name,
      kind,
      currency: item.currencyType?.name ?? '',
      deposit: item.depositAmount,
      withdrawal: item.withdrawalAmount,
      balance: item.depositAmount - item.withdrawalAmount,
    };
  }

  /** En çok hesabın bulunduğu para birimi açılışta seçili gelir. */
  private defaultCurrency(rows: AccountRow[]): string {
    const counts = new Map<string, number>();
    for (const row of rows) {
      counts.set(row.currency, (counts.get(row.currency) ?? 0) + 1);
    }

    let best = '';
    let bestCount = -1;
    for (const [currency, count] of counts) {
      if (count > bestCount) {
        best = currency;
        bestCount = count;
      }
    }

    return best;
  }

  /** Para birimleri karıştırılamayacağı için özet her biri için ayrı çıkarılıyor. */
  readonly summaries = computed<CurrencySummary[]>(() => {
    const byCurrency = new Map<string, CurrencySummary>();

    for (const row of this.accounts()) {
      const existing = byCurrency.get(row.currency) ?? {
        currency: row.currency,
        symbol: this.symbolFor(row.currency),
        balance: 0,
        deposit: 0,
        withdrawal: 0,
        accountCount: 0,
      };

      existing.balance += row.balance;
      existing.deposit += row.deposit;
      existing.withdrawal += row.withdrawal;
      existing.accountCount += 1;

      byCurrency.set(row.currency, existing);
    }

    return [...byCurrency.values()].sort((a, b) =>
      a.currency.localeCompare(b.currency, 'tr')
    );
  });

  readonly selectedRows = computed(() =>
    this.accounts()
      .filter((row) => row.currency === this.selectedCurrency())
      .sort((a, b) => b.balance - a.balance)
  );

  readonly selectedSymbol = computed(() =>
    this.symbolFor(this.selectedCurrency())
  );

  /** Çubuk uzunlukları en büyük mutlak bakiyeye göre ölçekleniyor. */
  private readonly maxAbsBalance = computed(() => {
    const values = this.selectedRows().map((row) => Math.abs(row.balance));
    return values.length > 0 ? Math.max(...values) : 0;
  });

  barWidth(balance: number): number {
    const max = this.maxAbsBalance();
    if (max === 0) return 0;

    return Math.max(2, (Math.abs(balance) / max) * 100);
  }

  /** Seçili para biriminde kasa ve banka bakiyelerinin payı. */
  readonly split = computed(() => {
    const rows = this.selectedRows();

    const sumOf = (kind: AccountKind) =>
      rows
        .filter((row) => row.kind === kind)
        .reduce((total, row) => total + Math.max(0, row.balance), 0);

    const cash = sumOf('Kasa');
    const bank = sumOf('Banka');
    const total = cash + bank;

    return {
      cash,
      bank,
      total,
      cashShare: total > 0 ? cash / total : 0,
      bankShare: total > 0 ? bank / total : 0,
    };
  });

  /** Halkanın kasa dilimi için dasharray değeri. */
  readonly cashDash = computed(() => {
    const share = this.split().cashShare;
    return `${share * DONUT_CIRCUMFERENCE} ${DONUT_CIRCUMFERENCE}`;
  });

  selectCurrency(currency: string): void {
    this.selectedCurrency.set(currency);
  }

  symbolFor(name: string): string {
    if (name === 'TL') return '₺';
    if (name === 'USD') return '$';
    if (name === 'EURO' || name === 'EUR') return '€';
    return '';
  }

  contact(): void {
    this.demo.openContactPage();
  }
}
