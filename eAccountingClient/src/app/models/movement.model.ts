export class MovementModel {
  id: string = '';
  accountId: string = '';
  accountName: string = '';
  /** "Kasa" ya da "Banka" — hangi hareket sayfasına gidileceğini belirler. */
  accountKind: string = '';
  currencyName: string = '';
  date: string = '';
  description: string = '';
  deposit: number = 0;
  withdrawal: number = 0;
  isTransfer: boolean = false;
  categoryId: string | null = null;
  categoryName: string | null = null;
}
