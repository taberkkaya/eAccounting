import { Component } from '@angular/core';
import { CashRegisterModel } from '../../models/cashRegister.model';
import { HttpService } from '../../services/http.service';
import { SwalService } from '../../services/swal.service';
import { NgForm } from '@angular/forms';
import { CashRegisterDetailModel } from '../../models/cashRegisterDetail.model';
import { SharedModule } from '../../modules/shared.module';
import { ActivatedRoute } from '@angular/router';
import { CashRegisterDetailPipe } from '../../pipes/cash-register-detail.pipe';
import { DatePipe } from '@angular/common';

/** Opening range for the movement list, in days back from today. */
const DEFAULT_RANGE_DAYS = 90;

@Component({
  selector: 'app-cash-register-details',
  standalone: true,
  imports: [SharedModule, CashRegisterDetailPipe],
  templateUrl: './cash-register-details.component.html',
  styleUrl: './cash-register-details.component.css',
  providers: [DatePipe],
})
export class CashRegisterDetailsComponent {
  cashRegister: CashRegisterModel = new CashRegisterModel();
  cashRegisters: CashRegisterModel[] = [];
  cashRegisterId: string = '';
  startDate: string = '';
  endDate: string = '';

  search: string = '';

  /** Sürmekte olan dışa aktarma; düğmenin beklemesi için. */
  exporting: 'excel' | 'pdf' | null = null;

  createOpen = false;
  updateOpen = false;

  createModel: CashRegisterDetailModel = new CashRegisterDetailModel();
  updateModel: CashRegisterDetailModel = new CashRegisterDetailModel();

  constructor(
    private http: HttpService,
    private swal: SwalService,
    private activated: ActivatedRoute,
    private date: DatePipe
  ) {
    this.activated.params.subscribe((res) => {
      this.cashRegisterId = res['id'];

      const today = new Date();
      const rangeStart = new Date();
      rangeStart.setDate(today.getDate() - DEFAULT_RANGE_DAYS);

      // Opens on the recent history rather than today alone, so the page is not empty
      // the first time it is visited.
      this.startDate = this.date.transform(rangeStart, 'yyyy-MM-dd') ?? '';
      this.endDate = this.date.transform(today, 'yyyy-MM-dd') ?? '';
      this.createModel.date = this.date.transform(today, 'yyyy-MM-dd') ?? '';
      this.createModel.cashRegisterId = this.cashRegisterId;
      this.getAll();
      this.getAllCashRegisters();
    });
  }

  /**
   * Ekrandaki tarih aralığını sunucuya gönderip biçimlendirilmiş dosyayı indirir.
   * Rapor sunucuda üretiliyor; böylece Excel ve PDF aynı veriden çıkıyor.
   */
  export(format: 'excel' | 'pdf') {
    this.exporting = format;

    this.http.download(
      'CashRegisterDetails/Export',
      {
        cashRegisterId: this.cashRegisterId,
        startDate: this.startDate,
        endDate: this.endDate,
        format: format === 'pdf' ? 1 : 0,
      },
      `kasa-ekstre.${format === 'pdf' ? 'pdf' : 'xlsx'}`,
      () => (this.exporting = null),
      () => (this.exporting = null)
    );
  }

  openCreate() {
    this.createModel = new CashRegisterDetailModel();
    this.createModel.date = this.date.transform(new Date(), 'yyyy-MM-dd') ?? '';
    this.createModel.cashRegisterId = this.cashRegisterId;
    this.createOpen = true;
  }

  openUpdate(model: CashRegisterDetailModel) {
    this.get(model);
    this.updateOpen = true;
  }

  getAllCashRegisters() {
    this.http.post<CashRegisterModel[]>('CashRegisters/GetAll', {}, (res) => {
      this.cashRegisters = res.filter((p) => p.id != this.cashRegisterId);
    });
  }

  getAll() {
    this.http.post<CashRegisterModel>(
      'CashRegisterDetails/GetAll',
      {
        cashRegisterId: this.cashRegisterId,
        startDate: this.startDate,
        endDate: this.endDate,
      },
      (res) => {
        this.cashRegister = res;
      }
    );
  }

  create(form: NgForm) {
    if (form.valid) {
      this.createModel.amount = +this.createModel.amount;
      this.createModel.oppositeAmount = +this.createModel.oppositeAmount;

      if (this.createModel.recordType === 0) {
        this.createModel.oppositeCashRegisterId = null;
      }

      this.http.post<string>(
        'CashRegisterDetails/Create',
        this.createModel,
        (res) => {
          this.swal.callToast(res);
          this.createModel = new CashRegisterDetailModel();
          this.createModel.date =
            this.date.transform(new Date(), 'yyyy-MM-dd') ?? '';
          this.createModel.cashRegisterId = this.cashRegisterId;
          this.createOpen = false;
          this.getAll();
        }
      );
    }
  }

  deleteById(model: CashRegisterDetailModel) {
    this.swal.callSwal(
      'Hareketi sil',
      `${model.date} - ${model.description} kaydını silmek istediğinize emin misiniz?`,
      () => {
        this.http.post<string>(
          'CashRegisterDetails/DeleteById',
          { id: model.id },
          (res) => {
            this.getAll();
            this.swal.callToast(res, 'info');
          }
        );
      }
    );
  }

  get(model: CashRegisterDetailModel) {
    this.updateModel = { ...model };
    this.updateModel.amount =
      this.updateModel.depositAmount + this.updateModel.withdrawalAmount;
    this.updateModel.type = this.updateModel.depositAmount > 0 ? 1 : 0;
  }

  update(form: NgForm) {
    if (form.valid) {
      this.http.post<string>(
        'CashRegisterDetails/Update',
        this.updateModel,
        (res) => {
          this.swal.callToast(res, 'info');
          this.updateOpen = false;
          this.getAll();
        }
      );
    }
  }

  changeCurrencyNameToSymbol(name: string) {
    if (name === 'TL') return '₺';
    else if (name === 'USD') return '$';
    else if (name === 'EURO') return '€';
    else return '';
  }

  setOppositeCashRegister() {
    const cash = this.cashRegisters.find(
      (p) => p.id === this.createModel.oppositeCashRegisterId
    );

    if (cash) {
      this.createModel.oppositeCashRegister = cash;
    }
  }
}
