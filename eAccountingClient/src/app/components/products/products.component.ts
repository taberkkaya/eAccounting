import { Component, OnInit, inject } from '@angular/core';
import { NgForm } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { SharedModule } from '../../modules/shared.module';
import { HttpService } from '../../services/http.service';
import { AuthService } from '../../services/auth.service';
import { SwalService } from '../../services/swal.service';
import { NoCompanyComponent } from '../ui/no-company/no-company.component';
import { ActionMenuComponent } from '../ui/action-menu/action-menu.component';
import {
  CurrencyOptions,
  ProductFormModel,
  ProductModel,
  StockTransactionModel,
  VatRates,
  currencySymbol,
} from '../../models/accounting.model';

/**
 * Ürün ve hizmet kartları.
 *
 * İkisi tek listede çünkü faturaya ikisi de aynı şekilde giriyor. Ayıran tek
 * şey stok: hizmetin miktarı tutulmuyor, o yüzden stok sütunları ve stok
 * düzeltme hizmetlerde kapalı.
 */
@Component({
  selector: 'app-products',
  standalone: true,
  imports: [SharedModule, NoCompanyComponent, ActionMenuComponent],
  templateUrl: './products.component.html',
  styleUrl: './products.component.css',
  providers: [DatePipe],
})
export class ProductsComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpService);
  private readonly swal = inject(SwalService);
  private readonly date = inject(DatePipe);

  readonly currencies = CurrencyOptions;
  readonly vatRates = VatRates;

  products: ProductModel[] = [];
  loading = true;

  kindFilter: '' | 'product' | 'service' = '';
  search = '';
  onlyLowStock = false;

  createOpen = false;
  updateOpen = false;
  createModel = new ProductFormModel();
  updateModel = new ProductFormModel();

  stockOpen = false;
  stockProduct: ProductModel | null = null;
  stockDirection: 0 | 1 = 0;
  stockQuantity = 0;
  stockDate = '';
  stockDescription = '';

  historyOpen = false;
  historyProduct: ProductModel | null = null;
  history: StockTransactionModel[] = [];

  ngOnInit(): void {
    if (!this.auth.hasCompany) {
      this.loading = false;
      return;
    }

    this.getAll();
  }

  getAll(): void {
    this.loading = true;

    this.http.post<ProductModel[]>(
      'Products/GetAll',
      {
        search: this.search.trim() || null,
        isService: this.kindFilter === '' ? null : this.kindFilter === 'service',
        onlyLowStock: this.onlyLowStock,
      },
      (res) => {
        this.products = res;
        this.loading = false;
      },
      () => (this.loading = false)
    );
  }

  setKind(value: '' | 'product' | 'service'): void {
    this.kindFilter = value;
    this.getAll();
  }

  toggleLowStock(): void {
    this.onlyLowStock = !this.onlyLowStock;
    this.getAll();
  }

  get counts(): { all: number; product: number; service: number; low: number } {
    return {
      all: this.products.length,
      product: this.products.filter((p) => !p.isService).length,
      service: this.products.filter((p) => p.isService).length,
      low: this.products.filter((p) => p.isBelowCritical).length,
    };
  }

  /** Eldeki stoğun alış fiyatıyla değeri; "depoda ne kadar param duruyor". */
  get stockValue(): { currency: string; amount: number }[] {
    const map = new Map<string, number>();

    for (const product of this.products) {
      if (product.isService) continue;

      map.set(
        product.currencyName,
        (map.get(product.currencyName) ?? 0) + product.stockQuantity * product.purchasePrice
      );
    }

    return [...map.entries()].map(([currency, amount]) => ({ currency, amount }));
  }

  openCreate(isService: boolean): void {
    this.createModel = new ProductFormModel();
    this.createModel.isService = isService;
    this.createModel.unit = isService ? 'Saat' : 'Adet';
    this.createOpen = true;
  }

  create(form: NgForm): void {
    if (!form.valid) return;

    this.http.post<string>(
      'Products/Create',
      { ...this.createModel, code: this.createModel.code.trim() || null },
      (res) => {
        this.swal.callToast(res);
        this.createOpen = false;
        this.getAll();
      }
    );
  }

  openUpdate(product: ProductModel): void {
    this.updateModel = {
      id: product.id,
      name: product.name,
      code: product.code ?? '',
      unit: product.unit,
      isService: product.isService,
      purchasePrice: product.purchasePrice,
      salePrice: product.salePrice,
      vatRate: product.vatRate,
      currencyTypeValue: product.currencyTypeValue,
      openingStock: 0,
      criticalStock: product.criticalStock,
      description: product.description ?? '',
    };

    this.updateOpen = true;
  }

  update(form: NgForm): void {
    if (!form.valid) return;

    this.http.post<string>(
      'Products/Update',
      { ...this.updateModel, code: this.updateModel.code.trim() || null },
      (res) => {
        this.swal.callToast(res);
        this.updateOpen = false;
        this.getAll();
      }
    );
  }

  deleteById(product: ProductModel): void {
    this.swal.callSwal('Kaydı sil?', `"${product.name}" silinecek.`, () => {
      this.http.post<string>('Products/DeleteById', { id: product.id }, (res) => {
        this.swal.callToast(res, 'info');
        this.getAll();
      });
    });
  }

  openStock(product: ProductModel, direction: 0 | 1): void {
    this.stockProduct = product;
    this.stockDirection = direction;
    this.stockQuantity = 0;
    this.stockDescription = '';
    this.stockDate = this.date.transform(new Date(), 'yyyy-MM-dd') ?? '';
    this.stockOpen = true;
  }

  saveStock(): void {
    if (!this.stockProduct || this.stockQuantity <= 0) return;

    this.http.post<string>(
      'Products/AdjustStock',
      {
        productId: this.stockProduct.id,
        direction: this.stockDirection,
        quantity: this.stockQuantity,
        date: this.stockDate,
        description: this.stockDescription.trim() || null,
      },
      (res) => {
        this.swal.callToast(res);
        this.stockOpen = false;
        this.getAll();
      }
    );
  }

  openHistory(product: ProductModel): void {
    this.historyProduct = product;
    this.history = [];
    this.historyOpen = true;

    this.http.post<StockTransactionModel[]>(
      'Products/GetStockTransactions',
      { productId: product.id },
      (res) => (this.history = res)
    );
  }

  symbolFor(name: string): string {
    return currencySymbol(name);
  }
}
