import { Pipe, PipeTransform } from '@angular/core';
import { CashRegisterModel } from '../models/cashRegister.model';

@Pipe({
  name: 'cashRegister',
  standalone: true,
})
export class CashRegisterPipe implements PipeTransform {
  transform(value: CashRegisterModel[], search: string): CashRegisterModel[] {
    if (!search) return value;

    return value.filter((p) =>
      p.name.toLocaleLowerCase().includes(search.toLocaleLowerCase())
    );
  }
}
