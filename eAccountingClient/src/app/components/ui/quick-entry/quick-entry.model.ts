/** Hızlı hareket ekranında seçilebilen hesap. Kasa ve banka aynı listede durur. */
export interface QuickAccount {
  id: string;
  name: string;
  kind: 'Kasa' | 'Banka';
  currency: string;
  symbol: string;
  balance: number;
}
