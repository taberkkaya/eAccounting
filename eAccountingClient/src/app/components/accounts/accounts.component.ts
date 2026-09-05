import { Component, OnInit, inject } from '@angular/core';
import { NgForm } from '@angular/forms';
import { SharedModule } from '../../modules/shared.module';
import { HttpService } from '../../services/http.service';
import { SwalService } from '../../services/swal.service';
import { AuthService } from '../../services/auth.service';
import { CurrencyTypes } from '../../models/currencyType.model';
import { CashRegisterModel } from '../../models/cashRegister.model';
import { BankModel } from '../../models/bank.model';
import { NoCompanyComponent } from '../ui/no-company/no-company.component';
import { AccountEndpoints, AccountKind, AccountModel } from './account.model';
import { ActionMenuComponent } from '../ui/action-menu/action-menu.component';

/**
 * Kasalar ve bankalar tek listede.
 *
 * İkisi de aynı işi görüyor: bir para birimi, bir bakiye, hareketler. Aralarındaki
 * tek gerçek fark bankanın IBAN'ı olması. Bunları ayrı sayfalara bölmek kullanıcıyı
 * her seferinde "bu kasa mı banka mı" sorusuna mecbur bırakıyordu; burada tür
 * eklerken seçilen bir alan ve listede bir filtre.
 *
 * Sunucu tarafı ayrı duruyor - iki tablo, iki uç kümesi - ve çağrılar türe göre
 * yönlendiriliyor.
 */
@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [SharedModule, NoCompanyComponent, ActionMenuComponent],
  templateUrl: './accounts.component.html',
  styleUrl: './accounts.component.css',
})
export class AccountsComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpService);
  private readonly swal = inject(SwalService);

  readonly currencyTypes = CurrencyTypes;
  readonly endpoints = AccountEndpoints;

  accounts: AccountModel[] = [];
  loading = true;
  search = '';
  /** '' hepsi, 'Kasa' ya da 'Banka'. */
  kindFilter: '' | AccountKind = '';

  createOpen = false;
  updateOpen = false;

  createModel: AccountModel = new AccountModel();
  updateModel: AccountModel = new AccountModel();

  ngOnInit(): void {
    if (!this.auth.hasCompany) {
      this.loading = false;
      return;
    }

    this.getAll();
  }

  get filtered(): AccountModel[] {
    const term = this.search.trim().toLocaleLowerCase('tr');

    return this.accounts.filter(
      (account) =>
        (!this.kindFilter || account.kind === this.kindFilter) &&
        (!term ||
          account.name.toLocaleLowerCase('tr').includes(term) ||
          (account.iban ?? '').toLocaleLowerCase('tr').includes(term))
    );
  }

  get counts(): { all: number; cash: number; bank: number } {
    return {
      all: this.accounts.length,
      cash: this.accounts.filter((a) => a.kind === 'Kasa').length,
      bank: this.accounts.filter((a) => a.kind === 'Banka').length,
    };
  }

  getAll(): void {
    this.loading = true;
    this.accounts = [];

    // İki uç ayrı ayrı çağrılıp tek listeye katılıyor.
    let pending = 2;
    const done = () => {
      if (--pending === 0) this.loading = false;
    };

    this.http.post<CashRegisterModel[]>(
      'CashRegisters/GetAll',
      {},
      (res) => {
        this.accounts = [...this.accounts, ...res.map((a) => this.toAccount(a, 'Kasa'))];
        done();
      },
      done
    );

    this.http.post<BankModel[]>(
      'Banks/GetAll',
      {},
      (res) => {
        this.accounts = [...this.accounts, ...res.map((a) => this.toAccount(a, 'Banka'))];
        done();
      },
      done
    );
  }

  openCreate(kind: AccountKind): void {
    this.createModel = new AccountModel();
    this.createModel.kind = kind;
    this.createOpen = true;
  }

  openUpdate(account: AccountModel): void {
    this.updateModel = Object.assign(new AccountModel(), account);
    this.updateModel.currencyTypeValue = account.currencyType.value;
    this.updateOpen = true;
  }

  create(form: NgForm): void {
    if (!form.valid) return;

    this.http.post<string>(
      `${this.endpoints[this.createModel.kind].base}/Create`,
      this.bodyOf(this.createModel, false),
      (res) => {
        this.swal.callToast(res);
        this.createOpen = false;
        this.getAll();
      }
    );
  }

  update(form: NgForm): void {
    if (!form.valid) return;

    this.http.post<string>(
      `${this.endpoints[this.updateModel.kind].base}/Update`,
      this.bodyOf(this.updateModel, true),
      (res) => {
        this.swal.callToast(res, 'info');
        this.updateOpen = false;
        this.getAll();
      }
    );
  }

  deleteById(account: AccountModel): void {
    this.swal.callSwal(
      'Hesabı sil',
      `${account.name} kaydını silmek istediğinize emin misiniz?`,
      () => {
        this.http.post<string>(
          `${this.endpoints[account.kind].base}/DeleteById`,
          { id: account.id },
          (res) => {
            this.swal.callToast(res, 'info');
            this.getAll();
          }
        );
      }
    );
  }

  detailsLink(account: AccountModel): string {
    return `/${this.endpoints[account.kind].detailsPath}/details/${account.id}`;
  }

  symbolFor(name: string): string {
    if (name === 'TL') return '₺';
    if (name === 'USD') return '$';
    if (name === 'EURO' || name === 'EUR') return '€';
    return '';
  }

  private toAccount(source: CashRegisterModel | BankModel, kind: AccountKind): AccountModel {
    const account = new AccountModel();

    account.id = source.id;
    account.kind = kind;
    account.name = source.name;
    account.iban = (source as BankModel).iban ?? '';
    account.depositAmount = source.depositAmount;
    account.withdrawalAmount = source.withdrawalAmount;
    account.currencyType = source.currencyType;
    account.currencyTypeValue = source.currencyType?.value ?? 1;

    return account;
  }

  /** IBAN yalnızca bankaya gönderiliyor; kasada böyle bir alan yok. */
  private bodyOf(account: AccountModel, withId: boolean): Record<string, unknown> {
    const body: Record<string, unknown> = {
      name: account.name,
      currencyTypeValue: +account.currencyTypeValue,
    };

    if (withId) body['id'] = account.id;
    if (account.kind === 'Banka') body['iban'] = account.iban;

    return body;
  }
}
