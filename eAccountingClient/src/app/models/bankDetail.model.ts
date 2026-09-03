import { BankModel } from './bank.model';

export class BankDetailModel {
  id: string = '';
  bankId: string = '';
  date: string = '';
  type: number = 0;
  depositAmount: number = 0;
  withdrawalAmount: number = 0;
  bankDetailId: string = '';
  description: string = '';
  amount: number = 0;
  recordType: number = 0;
  oppositeAmount: number = 0;
  oppositeBankId: string | any = '';
  oppositeBank: BankModel = new BankModel();
}
