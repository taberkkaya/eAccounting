import { Component } from '@angular/core';
import { SharedModule } from '../../modules/shared.module';
import { CompanyModel } from '../../models/company.model';
import { HttpService } from '../../services/http.service';
import { SwalService } from '../../services/swal.service';
import { NgForm } from '@angular/forms';
import { CompanyPipe } from '../../pipes/company.pipe';

@Component({
  selector: 'app-companies',
  standalone: true,
  imports: [SharedModule, CompanyPipe],
  templateUrl: './companies.component.html',
  styleUrl: './companies.component.css',
})
export class CompaniesComponent {
  companies: CompanyModel[] = [];
  search: string = '';

  createOpen = false;
  updateOpen = false;
  
  /** Veritabanı bölümü varsayılan olarak kapalı; çoğu firma için gerekmiyor. */
  createDbOpen = false;
  updateDbOpen = false;

  createModel: CompanyModel = new CompanyModel();
  updateModel: CompanyModel = new CompanyModel();

  constructor(private http: HttpService, private swal: SwalService) {}

  openCreate() {
    this.createModel = new CompanyModel();
    this.createDbOpen = false;
    this.createOpen = true;
  }

  openUpdate(model: CompanyModel) {
    this.get(model);
    this.updateDbOpen = false;
    this.updateOpen = true;
  }

  ngOnInit(): void {
    this.getAll();
  }

  getAll() {
    this.http.post<CompanyModel[]>('Companies/GetAll', {}, (res) => {
      this.companies = res;
    });
  }

  create(form: NgForm) {
    if (form.valid) {
      this.http.post<string>('Companies/Create', this.createModel, (res) => {
        this.swal.callToast(res);
        this.createModel = new CompanyModel();
        this.createOpen = false;
        this.getAll();
      });
    }
  }

  deleteById(model: CompanyModel) {
    this.swal.callSwal(
      'Firmayı sil',
      `${model.name} kaydını silmek istediğinize emin misiniz?`,
      () => {
        this.http.post<string>(
          'Companies/DeleteById',
          { id: model.id },
          (res) => {
            this.getAll();
            this.swal.callToast(res, 'info');
          }
        );
      }
    );
  }

  get(model: CompanyModel) {
    this.updateModel = { ...model };
  }

  update(form: NgForm) {
    if (form.valid) {
      this.http.post<string>('Companies/Update', this.updateModel, (res) => {
        this.swal.callToast(res, 'info');
        this.updateOpen = false;
        this.getAll();
      });
    }
  }

  migrateAll() {
    this.swal.callSwal(
      'Migrate All Company Db',
      'Do you want to update all company databases?',
      () => {
        this.http.post<string>('Companies/MigrateAll', {}, (res) => {
          this.swal.callToast(res);
        });
      },
      'Migrate'
    );
  }
}
