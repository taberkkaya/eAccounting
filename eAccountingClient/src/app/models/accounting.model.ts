/** Cari, ürün ve fatura ekranlarının paylaştığı modeller. */

/** 1 müşteri, 2 tedarikçi, 3 her ikisi. */
export type ContactTypeValue = 1 | 2 | 3;

export class ContactModel {
  id: string = '';
  name: string = '';
  type: ContactTypeValue = 1;
  typeName: string = '';
  taxNumber: string | null = null;
  taxOffice: string | null = null;
  phone: string | null = null;
  email: string | null = null;
  address: string | null = null;
  note: string | null = null;
  currencyName: string = 'TL';
  currencyTypeValue: number = 1;
  debitAmount: number = 0;
  creditAmount: number = 0;
  /** Artı ise cari bize borçlu, eksi ise biz ona borçluyuz. */
  balance: number = 0;
  /** Vadesi geçmiş ve hâlâ kapanmamış fatura tutarı. */
  overdueAmount: number = 0;
}

/** Yeni cari formu; açılış bakiyesi yalnızca oluştururken sorulur. */
export class ContactFormModel {
  id: string = '';
  name: string = '';
  type: ContactTypeValue = 1;
  currencyTypeValue: number = 1;
  taxNumber: string = '';
  taxOffice: string = '';
  phone: string = '';
  email: string = '';
  address: string = '';
  note: string = '';
  openingBalance: number = 0;
}

export class ContactStatementLineModel {
  id: string = '';
  date: string = '';
  description: string = '';
  kind: number = 0;
  kindName: string = '';
  debitAmount: number = 0;
  creditAmount: number = 0;
  runningBalance: number = 0;
  invoiceId: string | null = null;
  invoiceNumber: string | null = null;
  accountId: string | null = null;
  accountName: string | null = null;
}

export class ContactStatementModel {
  contactId: string = '';
  contactName: string = '';
  currencyName: string = 'TL';
  startDate: string | null = null;
  endDate: string | null = null;
  openingBalance: number = 0;
  totalDebit: number = 0;
  totalCredit: number = 0;
  closingBalance: number = 0;
  lines: ContactStatementLineModel[] = [];
}

export class ProductModel {
  id: string = '';
  code: string | null = null;
  name: string = '';
  unit: string = 'Adet';
  isService: boolean = false;
  purchasePrice: number = 0;
  salePrice: number = 0;
  vatRate: number = 20;
  currencyName: string = 'TL';
  currencyTypeValue: number = 1;
  stockQuantity: number = 0;
  criticalStock: number = 0;
  description: string | null = null;
  isBelowCritical: boolean = false;
}

export class ProductFormModel {
  id: string = '';
  name: string = '';
  code: string = '';
  unit: string = 'Adet';
  isService: boolean = false;
  purchasePrice: number = 0;
  salePrice: number = 0;
  vatRate: number = 20;
  currencyTypeValue: number = 1;
  openingStock: number = 0;
  criticalStock: number = 0;
  description: string = '';
}

export class StockTransactionModel {
  id: string = '';
  date: string = '';
  direction: number = 0;
  directionName: string = '';
  quantity: number = 0;
  unitPrice: number = 0;
  description: string = '';
  invoiceId: string | null = null;
  runningQuantity: number = 0;
}

export class InvoiceLineModel {
  id: string = '';
  productId: string | null = null;
  description: string = '';
  unit: string = 'Adet';
  quantity: number = 1;
  unitPrice: number = 0;
  discountRate: number = 0;
  vatRate: number = 20;
  lineTotal: number = 0;
  vatAmount: number = 0;
}

export class InvoiceModel {
  id: string = '';
  /** 1 satış, 2 alış. */
  type: number = 1;
  typeName: string = '';
  number: string = '';
  date: string = '';
  dueDate: string = '';
  contactId: string = '';
  contactName: string = '';
  currencyName: string = 'TL';
  currencyTypeValue: number = 1;
  status: number = 1;
  statusName: string = '';
  subTotal: number = 0;
  discountTotal: number = 0;
  vatTotal: number = 0;
  grandTotal: number = 0;
  paidAmount: number = 0;
  remainingAmount: number = 0;
  isOverdue: boolean = false;
  note: string | null = null;
  lines: InvoiceLineModel[] = [];
}

export class CurrencyAmountModel {
  currencyName: string = 'TL';
  amount: number = 0;
}

export class DueInvoiceModel {
  id: string = '';
  number: string = '';
  type: number = 1;
  contactId: string = '';
  contactName: string = '';
  dueDate: string = '';
  remainingAmount: number = 0;
  currencyName: string = 'TL';
  /** Eksi ise vade geçmiş. */
  daysLeft: number = 0;
}

export class ContactBalanceModel {
  id: string = '';
  name: string = '';
  currencyName: string = 'TL';
  balance: number = 0;
}

export class LowStockModel {
  id: string = '';
  name: string = '';
  unit: string = '';
  stockQuantity: number = 0;
  criticalStock: number = 0;
}

export class DashboardModel {
  cashBalances: CurrencyAmountModel[] = [];
  receivables: CurrencyAmountModel[] = [];
  payables: CurrencyAmountModel[] = [];
  overdueReceivables: CurrencyAmountModel[] = [];
  overduePayables: CurrencyAmountModel[] = [];
  monthSales: CurrencyAmountModel[] = [];
  monthPurchases: CurrencyAmountModel[] = [];
  upcomingInvoices: DueInvoiceModel[] = [];
  topDebtors: ContactBalanceModel[] = [];
  topCreditors: ContactBalanceModel[] = [];
  lowStock: LowStockModel[] = [];
  contactCount: number = 0;
  productCount: number = 0;
  openInvoiceCount: number = 0;
}

export class AgingRowModel {
  contactId: string = '';
  contactName: string = '';
  currencyName: string = 'TL';
  notDue: number = 0;
  days1To30: number = 0;
  days31To60: number = 0;
  days61To90: number = 0;
  over90: number = 0;
  total: number = 0;
}

export class AgingReportModel {
  type: number = 1;
  typeName: string = '';
  asOf: string = '';
  rows: AgingRowModel[] = [];
  totals: AgingRowModel[] = [];
}

export class VatRateRowModel {
  rate: number = 0;
  base: number = 0;
  vat: number = 0;
}

export class VatReportModel {
  startDate: string = '';
  endDate: string = '';
  currencyName: string = 'TL';
  collected: VatRateRowModel[] = [];
  deductible: VatRateRowModel[] = [];
  collectedTotal: number = 0;
  deductibleTotal: number = 0;
  /** Artı ise ödenecek, eksi ise devreden KDV. */
  payable: number = 0;
}

export class CategoryTotalModel {
  name: string = '';
  amount: number = 0;
}

export class MonthlyTotalModel {
  year: number = 0;
  month: number = 0;
  revenue: number = 0;
  cost: number = 0;
}

export class ProfitLossModel {
  startDate: string = '';
  endDate: string = '';
  currencyName: string = 'TL';
  revenue: number = 0;
  cost: number = 0;
  otherExpenses: number = 0;
  profit: number = 0;
  expenseByCategory: CategoryTotalModel[] = [];
  monthly: MonthlyTotalModel[] = [];
}

/** Ekranlarda tekrar eden para birimi listesi. */
export const CurrencyOptions = [
  { value: 1, name: 'TL' },
  { value: 2, name: 'USD' },
  { value: 3, name: 'EURO' },
];

/** Güncel Türk KDV oranları. */
export const VatRates = [0, 1, 10, 20];

export function currencySymbol(name: string): string {
  if (name === 'TL') return '₺';
  if (name === 'USD') return '$';
  if (name === 'EURO' || name === 'EUR') return '€';
  return '';
}
