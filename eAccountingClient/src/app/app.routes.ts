import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { LayoutsComponent } from './components/layouts/layouts.component';
import { HomeComponent } from './components/home/home.component';
import { Component, inject } from '@angular/core';
import { AuthService } from './services/auth.service';
import { UsersComponent } from './components/users/users.component';
import { ConfirmEmailComponent } from './components/confirm-email/confirm-email.component';
import { CompaniesComponent } from './components/companies/companies.component';
import { DemoVisitorsComponent } from './components/demo-visitors/demo-visitors.component';
import { MovementsComponent } from './components/movements/movements.component';
import { AccountsComponent } from './components/accounts/accounts.component';
import { CategoriesComponent } from './components/categories/categories.component';
import { CashRegisterDetailsComponent } from './components/cash-register-details/cash-register-details.component';
import { BankDetailsComponent } from './components/bank-details/bank-details.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
  },

  {
    path: 'confirm-email',
    component: ConfirmEmailComponent,
  },
  {
    path: '',
    component: LayoutsComponent,
    canActivateChild: [() => inject(AuthService).isAuthenticated()],
    children: [
      {
        path: '',
        component: HomeComponent,
      },
      {
        path: 'users',
        component: UsersComponent,
        canActivate: [() => inject(AuthService).isAdmin()],
      },
      {
        path: 'companies',
        component: CompaniesComponent,
        canActivate: [() => inject(AuthService).isAdmin()],
      },
      {
        path: 'demo-visitors',
        component: DemoVisitorsComponent,
        canActivate: [() => inject(AuthService).isAdmin()],
      },
      {
        path: 'movements',
        component: MovementsComponent,
      },
      {
        path: 'categories',
        component: CategoriesComponent,
      },
      {
        path: 'accounts',
        component: AccountsComponent,
      },
      {
        path: 'contacts',
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./components/contacts/contacts.component').then(
                (m) => m.ContactsComponent
              ),
          },
          {
            path: ':id',
            loadComponent: () =>
              import('./components/contact-detail/contact-detail.component').then(
                (m) => m.ContactDetailComponent
              ),
          },
        ],
      },
      {
        path: 'products',
        loadComponent: () =>
              import('./components/products/products.component').then(
                (m) => m.ProductsComponent
              ),
      },
      {
        // "new" bir kimlik değil; :id'den önce gelmezse fatura formu yerine
        // "new" kimlikli bir fatura aranırdı.
        path: 'invoices',
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./components/invoices/invoices.component').then(
                (m) => m.InvoicesComponent
              ),
          },
          {
            path: 'new',
            loadComponent: () =>
              import('./components/invoice-form/invoice-form.component').then(
                (m) => m.InvoiceFormComponent
              ),
          },
          {
            path: ':id',
            loadComponent: () =>
              import('./components/invoice-detail/invoice-detail.component').then(
                (m) => m.InvoiceDetailComponent
              ),
          },
          {
            path: ':id/edit',
            loadComponent: () =>
              import('./components/invoice-form/invoice-form.component').then(
                (m) => m.InvoiceFormComponent
              ),
          },
        ],
      },
      {
        path: 'reports',
        loadComponent: () =>
              import('./components/reports/reports.component').then(
                (m) => m.ReportsComponent
              ),
      },
      {
        path: 'cash-registers',
        children: [
          {
            // Liste artık tek yerde; eski adres kırılmasın diye yönlendiriliyor.
            path: '',
            redirectTo: '/accounts',
            pathMatch: 'full',
          },
          {
            path: 'details/:id',
            component: CashRegisterDetailsComponent,
          },
        ],
      },
      {
        path: 'banks',
        children: [
          {
            path: '',
            redirectTo: '/accounts',
            pathMatch: 'full',
          },
          {
            path: 'details/:id',
            component: BankDetailsComponent,
          },
        ],
      },
    ],
  },
];
