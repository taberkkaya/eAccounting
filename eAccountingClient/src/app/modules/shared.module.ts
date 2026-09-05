import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TrCurrencyPipe } from 'tr-currency';
import { FormValidateDirective } from 'form-validate-angular';
import { BlankComponent } from '../components/blank/blank.component';
import { SectionComponent } from '../components/section/section.component';
import { ModalComponent } from '../components/ui/modal/modal.component';

@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    TrCurrencyPipe,
    FormValidateDirective,
    BlankComponent,
    SectionComponent,
    ModalComponent,
  ],
  exports: [
    CommonModule,
    FormsModule,
    RouterLink,
    TrCurrencyPipe,
    FormValidateDirective,
    BlankComponent,
    SectionComponent,
    ModalComponent,
  ],
})
export class SharedModule {}
