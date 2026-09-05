import { CurrencyTypeModel } from '../../models/currencyType.model';

/** Kasa ve bankanın ortak hâli. Tür yalnızca bir alan. */
export type AccountKind = 'Kasa' | 'Banka';

export class AccountModel {
  id: string = '';
  kind: AccountKind = 'Kasa';
  name: string = '';
  /** Yalnızca banka için anlamlı. */
  iban: string = '';
  depositAmount: number = 0;
  withdrawalAmount: number = 0;
  currencyType: CurrencyTypeModel = new CurrencyTypeModel();
  currencyTypeValue: number = 1;

  get balance(): number {
    return this.depositAmount - this.withdrawalAmount;
  }
}

/** Bir türün hangi uçlara ve hangi alan adlarına karşılık geldiği. */
export const AccountEndpoints: Record<
  AccountKind,
  { base: string; idField: string; detailsPath: string; icon: string }
> = {
  Kasa: {
    base: 'CashRegisters',
    idField: 'cashRegisterId',
    detailsPath: 'cash-registers',
    icon: 'fas fa-cash-register',
  },
  Banka: {
    base: 'Banks',
    idField: 'bankId',
    detailsPath: 'banks',
    icon: 'fas fa-university',
  },
};
