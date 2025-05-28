import { CashRegisterModel } from './cashRegister.model';

export class CashRegisterDetailModel {
  id: string = '';
  cashRegisterId: string = '';
  date: string = '';
  type: number = 0;
  depositAmount: number = 0;
  withdrawalAmount: number = 0;
  cashRegisterDetailId: string = '';
  description: string = '';
  amount: number = 0;
  recordType: number = 0;
  oppositeAmount: number = 0;
  oppositeCashRegisterId: string | any = '';
  oppositeCashRegister: CashRegisterModel = new CashRegisterModel();
}
