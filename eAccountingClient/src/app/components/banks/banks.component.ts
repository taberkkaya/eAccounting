import { Component, inject } from '@angular/core';
import { SharedModule } from '../../modules/shared.module';
import { BankPipe } from '../../pipes/bank.pipe';
import { CurrencyTypes } from '../../models/currencyType.model';
import { BankModel } from '../../models/bank.model';
import { HttpService } from '../../services/http.service';
import { SwalService } from '../../services/swal.service';
import { NgForm } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { NoCompanyComponent } from '../ui/no-company/no-company.component';

@Component({
  selector: 'app-banks',
  standalone: true,
  imports: [SharedModule, BankPipe, NoCompanyComponent],
  templateUrl: './banks.component.html',
  styleUrl: './banks.component.css',
})
export class BanksComponent {
  readonly auth = inject(AuthService);

  banks: BankModel[] = [];
  search: string = '';

  currencyTypes = CurrencyTypes;

  createOpen = false;
  updateOpen = false;

  createModel: BankModel = new BankModel();
  updateModel: BankModel = new BankModel();

  constructor(private http: HttpService, private swal: SwalService) {}

  openCreate() {
    this.createModel = new BankModel();
    this.createOpen = true;
  }

  openUpdate(model: BankModel) {
    this.get(model);
    this.updateOpen = true;
  }

  ngOnInit(): void {
    this.getAll();
  }

  getAll() {


    // Firma yoksa bağlanılacak veritabanı da yok.


    if (!this.auth.hasCompany) return;

    this.http.post<BankModel[]>('Banks/GetAll', {}, (res) => {
      this.banks = res;
    });
  }

  create(form: NgForm) {
    if (form.valid) {
      this.http.post<string>('Banks/Create', this.createModel, (res) => {
        this.swal.callToast(res);
        this.createModel = new BankModel();
        this.createOpen = false;
        this.getAll();
      });
    }
  }

  deleteById(model: BankModel) {
    this.swal.callSwal(
      'Bankayı sil',
      `${model.name} kaydını silmek istediğinize emin misiniz?`,
      () => {
        this.http.post<string>('Banks/DeleteById', { id: model.id }, (res) => {
          this.getAll();
          this.swal.callToast(res, 'info');
        });
      }
    );
  }

  get(model: BankModel) {
    this.updateModel = { ...model };
    this.updateModel.currencyTypeValue = this.updateModel.currencyType.value;
  }

  update(form: NgForm) {
    if (form.valid) {
      this.http.post<string>('Banks/Update', this.updateModel, (res) => {
        this.swal.callToast(res, 'info');
        this.updateOpen = false;
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
}
