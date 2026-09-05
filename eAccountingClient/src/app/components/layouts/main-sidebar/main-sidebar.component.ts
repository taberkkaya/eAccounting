import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MenuModel, Menus } from '../../../menu';
import { MenuPipe } from '../../../pipes/menu.pipe';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-main-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, FormsModule, MenuPipe],
  templateUrl: './main-sidebar.component.html',
  styleUrl: './main-sidebar.component.css',
})
export class MainSidebarComponent {
  @Input() open = false;
  @Output() navigated = new EventEmitter<void>();

  search: string = '';

  constructor(public auth: AuthService) {}

  /**
   * Yönetim menüleri yalnızca yöneticilere görünür. Getter olarak duruyor çünkü
   * isAdmin bilgisi rota koruyucusu token'ı çözdükten sonra doluyor.
   */
  get menus(): MenuModel[] {
    if (this.auth.user.isAdmin) return Menus;

    return Menus.filter((menu) => !menu.showThisMenuJustAdmin);
  }

  /** Ad ve soyadın baş harfleri; profil fotoğrafı yerine. */
  get initials(): string {
    return (this.auth.user.name ?? '')
      .split(' ')
      .filter((part) => part.length > 0)
      .slice(0, 2)
      .map((part) => part[0].toLocaleUpperCase('tr'))
      .join('');
  }
}
