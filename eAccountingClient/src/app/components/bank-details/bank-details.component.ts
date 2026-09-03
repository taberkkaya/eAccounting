import { Component, ElementRef, ViewChild } from '@angular/core';
import { BankModel } from '../../models/bank.model';
import { HttpService } from '../../services/http.service';
import { SwalService } from '../../services/swal.service';
import { NgForm } from '@angular/forms';
import { BankDetailModel } from '../../models/bankDetail.model';
import { SharedModule } from '../../modules/shared.module';
import { ActivatedRoute } from '@angular/router';
import { BankDetailPipe } from '../../pipes/bank-detail.pipe';
import { DatePipe } from '@angular/common';

/** Opening range for the movement list, in days back from today. */
const DEFAULT_RANGE_DAYS = 90;

@Component({
  selector: 'app-bank-details',
  standalone: true,
  imports: [SharedModule, BankDetailPipe],
  templateUrl: './bank-details.component.html',
  styleUrl: './bank-details.component.css',
  providers: [DatePipe],
})
export class BankDetailsComponent {
  bank: BankModel = new BankModel();
  banks: BankModel[] = [];
  bankId: string = '';
  startDate: string = '';
  endDate: string = '';

  search: string = '';

  @ViewChild('createModalCloseBtn') createModalCloseBtn:
    | ElementRef<HTMLButtonElement>
    | undefined;
  @ViewChild('updateModalCloseBtn') updateModalCloseBtn:
    | ElementRef<HTMLButtonElement>
    | undefined;

  createModel: BankDetailModel = new BankDetailModel();
  updateModel: BankDetailModel = new BankDetailModel();

  constructor(
    private http: HttpService,
    private swal: SwalService,
    private activated: ActivatedRoute,
    private date: DatePipe
  ) {
    this.activated.params.subscribe((res) => {
      this.bankId = res['id'];

      const today = new Date();
      const rangeStart = new Date();
      rangeStart.setDate(today.getDate() - DEFAULT_RANGE_DAYS);

      // Opens on the recent history rather than today alone, so the page is not empty
      // the first time it is visited.
      this.startDate = this.format(rangeStart);
      this.endDate = this.format(today);
      this.createModel.date = this.format(today);
      this.createModel.bankId = this.bankId;

      this.getAll();
      this.getAllBanks();
    });
  }

  getAllBanks() {
    this.http.post<BankModel[]>('Banks/GetAll', {}, (res) => {
      this.banks = res.filter((p) => p.id != this.bankId);
    });
  }

  getAll() {
    this.http.post<BankModel>(
      'BankDetails/GetAll',
      {
        bankId: this.bankId,
        startDate: this.startDate,
        endDate: this.endDate,
      },
      (res) => {
        this.bank = res;
      }
    );
  }

  create(form: NgForm) {
    if (form.valid) {
      this.createModel.amount = +this.createModel.amount;
      this.createModel.oppositeAmount = +this.createModel.oppositeAmount;

      if (this.createModel.recordType === 0) {
        this.createModel.oppositeBankId = null;
      }

      this.http.post<string>('BankDetails/Create', this.createModel, (res) => {
        this.swal.callToast(res);
        this.createModel = new BankDetailModel();
        this.createModel.date = this.format(new Date());
        this.createModel.bankId = this.bankId;
        this.createModalCloseBtn?.nativeElement.click();
        this.getAll();
      });
    }
  }

  deleteById(model: BankDetailModel) {
    this.swal.callSwal(
      'Veriyi Sil?',
      `${model.date} - ${model.description} verisini silmek istiyor musunuz?`,
      () => {
        this.http.post<string>(
          'BankDetails/DeleteById',
          { id: model.id },
          (res) => {
            this.getAll();
            this.swal.callToast(res, 'info');
          }
        );
      }
    );
  }

  get(model: BankDetailModel) {
    this.updateModel = { ...model };
    this.updateModel.amount =
      this.updateModel.depositAmount + this.updateModel.withdrawalAmount;
    this.updateModel.type = this.updateModel.depositAmount > 0 ? 1 : 0;
  }

  update(form: NgForm) {
    if (form.valid) {
      this.http.post<string>('BankDetails/Update', this.updateModel, (res) => {
        this.swal.callToast(res, 'info');
        this.updateModalCloseBtn?.nativeElement.click();
        this.getAll();
      });
    }
  }

  changeCurrencyNameToSymbol(name: string) {
    if (name === 'TL') return '₺';
    else if (name === 'USD') return '$';
    else if (name === 'EURO') return 'Є';
    else return '';
  }

  setOppositeBank() {
    const bank = this.banks.find((p) => p.id === this.createModel.oppositeBankId);

    if (bank) {
      this.createModel.oppositeBank = bank;
    }
  }

  private format(value: Date): string {
    return this.date.transform(value, 'yyyy-MM-dd') ?? '';
  }
}
