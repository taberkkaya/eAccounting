import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BlankComponent } from '../components/blank/blank.component';
import { SectionComponent } from '../components/section/section.component';
import { FormsModule } from '@angular/forms';
import { TrCurrencyPipe } from 'tr-currency';
import { FlexiGridModule } from 'flexi-grid';
import { FlexiSelectModule } from 'flexi-select';
import { FormValidateDirective } from 'form-validate-angular';
import { SwalService } from '../services/swal.service';

@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    BlankComponent,
    SectionComponent,
    FormsModule,
    TrCurrencyPipe,
    FlexiGridModule,
    FlexiSelectModule,
    FormValidateDirective,
  ],
  exports: [
    CommonModule,
    BlankComponent,
    SectionComponent,
    FormsModule,
    TrCurrencyPipe,
    FlexiGridModule,
    FlexiSelectModule,
    FormValidateDirective,
  ],
})
export class SharedModule {}
