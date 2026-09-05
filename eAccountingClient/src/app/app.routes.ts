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
import { CashRegistersComponent } from './components/cash-registers/cash-registers.component';
import { CashRegisterDetailsComponent } from './components/cash-register-details/cash-register-details.component';
import { BanksComponent } from './components/banks/banks.component';
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
        path: 'cash-registers',
        children: [
          {
            path: '',
            component: CashRegistersComponent,
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
            component: BanksComponent,
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
