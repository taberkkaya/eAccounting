import { Component, OnInit, inject } from '@angular/core';
import { NgForm } from '@angular/forms';
import { SharedModule } from '../../modules/shared.module';
import { HttpService } from '../../services/http.service';
import { SwalService } from '../../services/swal.service';
import { AuthService } from '../../services/auth.service';
import { CategoryModel } from '../../models/category.model';
import { ModalComponent } from '../ui/modal/modal.component';
import { NoCompanyComponent } from '../ui/no-company/no-company.component';
import { ActionMenuComponent } from '../ui/action-menu/action-menu.component';

/**
 * Gelir ve gider kalemleri. Hareketler bunlarla etiketlenince "bu ay kiraya ne
 * verdim" sorusu tek filtreyle cevaplanabiliyor.
 */
@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [SharedModule, ModalComponent, NoCompanyComponent, ActionMenuComponent],
  templateUrl: './categories.component.html',
})
export class CategoriesComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpService);
  private readonly swal = inject(SwalService);

  categories: CategoryModel[] = [];
  loading = true;

  createOpen = false;
  updateOpen = false;

  createModel: CategoryModel = new CategoryModel();
  updateModel: CategoryModel = new CategoryModel();

  ngOnInit(): void {
    if (!this.auth.hasCompany) {
      this.loading = false;
      return;
    }

    this.getAll();
  }

  get income(): CategoryModel[] {
    return this.categories.filter((c) => c.direction === 0);
  }

  get expense(): CategoryModel[] {
    return this.categories.filter((c) => c.direction === 1);
  }

  getAll(): void {
    this.loading = true;

    this.http.post<CategoryModel[]>(
      'Categories/GetAll',
      {},
      (res) => {
        this.categories = res;
        this.loading = false;
      },
      () => (this.loading = false)
    );
  }

  openCreate(direction: number): void {
    this.createModel = new CategoryModel();
    this.createModel.direction = direction;
    this.createOpen = true;
  }

  openUpdate(model: CategoryModel): void {
    this.updateModel = { ...model };
    this.updateOpen = true;
  }

  create(form: NgForm): void {
    if (!form.valid) return;

    this.http.post<string>(
      'Categories/Create',
      { name: this.createModel.name, direction: +this.createModel.direction },
      (res) => {
        this.swal.callToast(res);
        this.createOpen = false;
        this.getAll();
      }
    );
  }

  update(form: NgForm): void {
    if (!form.valid) return;

    this.http.post<string>(
      'Categories/Update',
      {
        id: this.updateModel.id,
        name: this.updateModel.name,
        direction: +this.updateModel.direction,
      },
      (res) => {
        this.swal.callToast(res, 'info');
        this.updateOpen = false;
        this.getAll();
      }
    );
  }

  deleteById(model: CategoryModel): void {
    this.swal.callSwal(
      'Kalemi sil',
      `"${model.name}" kalemini silmek istediğinize emin misiniz? Geçmiş hareketler etkilenmez.`,
      () => {
        this.http.post<string>('Categories/DeleteById', { id: model.id }, (res) => {
          this.swal.callToast(res, 'info');
          this.getAll();
        });
      }
    );
  }
}
