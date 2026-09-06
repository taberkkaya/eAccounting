import { Component, OnInit, inject } from '@angular/core';
import { NgForm } from '@angular/forms';
import { SharedModule } from '../../modules/shared.module';
import { HttpService } from '../../services/http.service';
import { SwalService } from '../../services/swal.service';
import { AuthService } from '../../services/auth.service';
import { CategoryModel } from '../../models/category.model';
import { NoCompanyComponent } from '../ui/no-company/no-company.component';
import { ActionMenuComponent } from '../ui/action-menu/action-menu.component';

/**
 * Gelir ve gider kalemleri.
 *
 * Önceden ekran ikiye bölünmüştü: gelir solda, gider sağda, iki ayrı başlıksız
 * tablo. Uygulamanın geri kalanı - cariler, ürünler, faturalar - tek liste artı
 * tür filtresi biçiminde çalışıyor ve ön muhasebe programlarında da alışılmış
 * olan bu. Bölünmüş düzen hem aramayı imkânsız kılıyordu hem de kalem sayısı
 * artınca iki sütun birbirinden bağımsız uzuyordu.
 */
@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [SharedModule, NoCompanyComponent, ActionMenuComponent],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.css',
})
export class CategoriesComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpService);
  private readonly swal = inject(SwalService);

  categories: CategoryModel[] = [];
  loading = true;

  /** '' hepsi, 0 gelir, 1 gider. */
  directionFilter: '' | 0 | 1 = '';
  search = '';

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

  /** Filtre ve arama istemcide: liste kısa, sunucuya gitmeye değmez. */
  get filtered(): CategoryModel[] {
    const term = this.search.trim().toLocaleLowerCase('tr');

    return this.categories.filter(
      (category) =>
        (this.directionFilter === '' || category.direction === this.directionFilter) &&
        (!term || category.name.toLocaleLowerCase('tr').includes(term))
    );
  }

  get counts(): { all: number; income: number; expense: number } {
    return {
      all: this.categories.length,
      income: this.categories.filter((c) => c.direction === 0).length,
      expense: this.categories.filter((c) => c.direction === 1).length,
    };
  }

  openCreate(direction: 0 | 1): void {
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
    // Kaç hareketi etkileyeceğini söylemek, "geçmiş etkilenmez" demekten daha
    // somut: kullanıcı neyin yanında duracağını biliyor.
    const used = model.usageCount
      ? ` ${model.usageCount} harekette kullanılmış; o hareketler kalır, yalnızca etiketi görünmez olur.`
      : '';

    this.swal.callSwal(
      'Kalemi sil?',
      `"${model.name}" silinecek.${used}`,
      () => {
        this.http.post<string>('Categories/DeleteById', { id: model.id }, (res) => {
          this.swal.callToast(res, 'info');
          this.getAll();
        });
      }
    );
  }
}
