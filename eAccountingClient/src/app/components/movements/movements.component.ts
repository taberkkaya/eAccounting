import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { SharedModule } from '../../modules/shared.module';
import { HttpService } from '../../services/http.service';
import { AuthService } from '../../services/auth.service';
import { MovementModel } from '../../models/movement.model';
import { CategoryModel } from '../../models/category.model';
import { CashRegisterModel } from '../../models/cashRegister.model';
import { BankModel } from '../../models/bank.model';
import { NoCompanyComponent } from '../ui/no-company/no-company.component';

/** Hesap seçicideki bir satır; kasa ve banka aynı listede. */
interface AccountOption {
  id: string;
  name: string;
  kind: string;
}

/** Açılış aralığı: bugünden geriye kaç gün. */
const DEFAULT_RANGE_DAYS = 90;

/** Ekranda en fazla kaç satır çizilir. */
const LIST_LIMIT = 500;

/**
 * Dosyaya en fazla kaç satır girer. Ekrandan yüksek: tabloyu uzatmak sayfayı
 * yavaşlatır ama dosyanın eksik olması işe yaramaz hale getirir.
 */
const EXPORT_LIMIT = 2000;

/**
 * Bütün hesapların hareketleri tek yerde.
 *
 * Kasa ve banka uygulamanın içinde ayrı duruyor - biri eldeki nakit, diğeri
 * IBAN'ı olan bir hesap - ama para hareketine bakarken bu ayrımın kullanıcıya
 * yüklenmesi için sebep yok. Burada hepsi tek listede; hesap türü yalnızca bir
 * sütun ve istenirse bir filtre.
 */
@Component({
  selector: 'app-movements',
  standalone: true,
  imports: [SharedModule, NoCompanyComponent],
  templateUrl: './movements.component.html',
  styleUrl: './movements.component.css',
  providers: [DatePipe],
})
export class MovementsComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpService);
  private readonly date = inject(DatePipe);

  movements: MovementModel[] = [];
  categories: CategoryModel[] = [];
  accounts: AccountOption[] = [];
  loading = true;

  /** Sürmekte olan dışa aktarma; düğmenin beklemesi için. */
  exporting: 'excel' | 'pdf' | null = null;

  /** Şablonda yazdırmak için. */
  readonly listLimit = LIST_LIMIT;

  startDate = '';
  endDate = '';
  direction: string = '';
  accountId = '';
  categoryId = '';
  search = '';

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

    this.loadAccounts();
    this.loadCategories();
    this.getAll();
  }

  getAll(): void {
    this.loading = true;

    this.http.post<MovementModel[]>(
      'Movements/GetAll',
      { ...this.filters(), take: LIST_LIMIT },
      (res) => {
        this.movements = res;
        this.loading = false;
      },
      () => (this.loading = false)
    );
  }

  /**
   * Ekrandaki filtreleri sunucuya gönderip biçimlendirilmiş dosyayı indirir.
   * Dosya sunucuda üretiliyor; böylece Excel ve PDF aynı veriden çıkıyor.
   */
  export(format: 'excel' | 'pdf'): void {
    this.exporting = format;

    this.http.download(
      'Movements/Export',
      {
        ...this.filters(),
        take: EXPORT_LIMIT,
        format: format === 'pdf' ? 1 : 0,
      },
      `hareketler.${format === 'pdf' ? 'pdf' : 'xlsx'}`,
      () => (this.exporting = null),
      () => (this.exporting = null)
    );
  }

  /** Liste ve dosya aynı filtreleri kullansın diye tek yerden. */
  private filters() {
    return {
      startDate: this.startDate || null,
      endDate: this.endDate || null,
      // Boş dize "hepsi" demek; sunucu null bekliyor.
      direction: this.direction === '' ? null : +this.direction,
      accountId: this.accountId || null,
      categoryId: this.categoryId || null,
      search: this.search.trim() || null,
    };
  }

  /** Liste sınıra dayandıysa kullanıcı eksik baktığını bilmeli. */
  get capped(): boolean {
    return this.movements.length >= LIST_LIMIT;
  }

  clear(): void {
    this.direction = '';
    this.accountId = '';
    this.categoryId = '';
    this.search = '';
    this.getAll();
  }

  private loadAccounts(): void {
    this.http.post<CashRegisterModel[]>('CashRegisters/GetAll', {}, (res) => {
      this.accounts = [
        ...this.accounts,
        ...res.map((a) => ({ id: a.id, name: a.name, kind: 'Kasa' })),
      ];
    });

    this.http.post<BankModel[]>('Banks/GetAll', {}, (res) => {
      this.accounts = [
        ...this.accounts,
        ...res.map((a) => ({ id: a.id, name: a.name, kind: 'Banka' })),
      ];
    });
  }

  private loadCategories(): void {
    this.http.post<CategoryModel[]>('Categories/GetAll', {}, (res) => {
      this.categories = res;
    });
  }

  /** Seçili aralıkta girenin ve çıkanın toplamı, para birimi ayrı ayrı. */
  get totals(): { currency: string; deposit: number; withdrawal: number }[] {
    const byCurrency = new Map<string, { currency: string; deposit: number; withdrawal: number }>();

    for (const movement of this.movements) {
      const row = byCurrency.get(movement.currencyName) ?? {
        currency: movement.currencyName,
        deposit: 0,
        withdrawal: 0,
      };

      row.deposit += movement.deposit;
      row.withdrawal += movement.withdrawal;
      byCurrency.set(movement.currencyName, row);
    }

    return [...byCurrency.values()];
  }

  movementLink(movement: MovementModel): string {
    const base = movement.accountKind === 'Kasa' ? 'cash-registers' : 'banks';

    return `/${base}/details/${movement.accountId}`;
  }

  symbolFor(name: string): string {
    if (name === 'TL') return '₺';
    if (name === 'USD') return '$';
    if (name === 'EURO' || name === 'EUR') return '€';
    return '';
  }

  private format(value: Date): string {
    return this.date.transform(value, 'yyyy-MM-dd') ?? '';
  }
}
