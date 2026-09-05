import { Component } from '@angular/core';
import { SharedModule } from '../../modules/shared.module';
import { CashRegisterModel } from '../../models/cashRegister.model';
import { SwalService } from '../../services/swal.service';
import { HttpService } from '../../services/http.service';
import { NgForm } from '@angular/forms';
import { CashRegisterPipe } from '../../pipes/cash-register.pipe';
import { CurrencyTypes } from '../../models/currencyType.model';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-cash-registers',
  standalone: true,
  imports: [SharedModule, CashRegisterPipe, RouterLink],
  templateUrl: './cash-registers.component.html',
  styleUrl: './cash-registers.component.css',
})
export class CashRegistersComponent {
  cashRegisters: CashRegisterModel[] = [];
  search: string = '';

  currencyTypes = CurrencyTypes;

  createOpen = false;
  updateOpen = false;

  createModel: CashRegisterModel = new CashRegisterModel();
  updateModel: CashRegisterModel = new CashRegisterModel();

  constructor(private http: HttpService, private swal: SwalService) {}

  openCreate() {
    this.createModel = new CashRegisterModel();
    this.createOpen = true;
  }

  openUpdate(model: CashRegisterModel) {
    this.get(model);
    this.updateOpen = true;
  }

  ngOnInit(): void {
    this.getAll();
  }

  getAll() {
    this.http.post<CashRegisterModel[]>('CashRegisters/GetAll', {}, (res) => {
      this.cashRegisters = res;
    });
  }

  create(form: NgForm) {
    if (form.valid) {
      this.http.post<string>(
        'CashRegisters/Create',
        this.createModel,
        (res) => {
          this.swal.callToast(res);
          this.createModel = new CashRegisterModel();
          this.createOpen = false;
          this.getAll();
        }
      );
    }
  }

  deleteById(model: CashRegisterModel) {
    this.swal.callSwal(
      'Kasayı sil',
      `${model.name} kaydını silmek istediğinize emin misiniz?`,
      () => {
        this.http.post<string>(
          'CashRegisters/DeleteById',
          { id: model.id },
          (res) => {
            this.getAll();
            this.swal.callToast(res, 'info');
          }
        );
      }
    );
  }

  get(model: CashRegisterModel) {
    this.updateModel = { ...model };
    this.updateModel.currencyTypeValue = this.updateModel.currencyType.value;
  }

  update(form: NgForm) {
    if (form.valid) {
      this.http.post<string>(
        'CashRegisters/Update',
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
    else if (name === 'EURO') return 'Є';
    else return '';
  }
}
