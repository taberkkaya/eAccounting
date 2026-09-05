import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

/**
 * Kullanıcı hiçbir firmaya bağlı değilken gösterilir. Veriler firma başına ayrı
 * veritabanlarında durduğu için gösterilecek bir şey yok; ekranın boş kalması
 * yerine ne yapılması gerektiği söyleniyor.
 */
@Component({
  selector: 'app-no-company',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="ak-panel">
      <div class="ak-empty">
        <i class="fas fa-city"></i>
        <div class="ak-empty__text">
          Hesabınız henüz bir firmaya bağlı değil. Kasa ve banka kayıtları her
          firmanın kendi veritabanında tutulduğu için görüntülenecek veri yok.
        </div>

        @if (auth.user.isAdmin) {
        <p class="no-company__hint">
          Yönetici olarak bir firma tanımlayıp kendinizi ona ekleyebilirsiniz.
        </p>
        <div class="no-company__actions">
          <a routerLink="/companies" class="ak-btn ak-btn--primary">
            <i class="fas fa-plus"></i>
            Firma Tanımla
          </a>
          <a routerLink="/users" class="ak-btn">
            <i class="fas fa-users"></i>
            Kullanıcıya Firma Ata
          </a>
        </div>
        } @else {
        <p class="no-company__hint">
          Bir yöneticinin sizi bir firmaya eklemesi gerekiyor.
        </p>
        }
      </div>
    </div>
  `,
  styles: [
    `
      .no-company__hint {
        margin: 12px 0 0;
        color: var(--ak-muted);
        font-size: 0.82rem;
      }

      .no-company__actions {
        display: flex;
        justify-content: center;
        gap: 9px;
        flex-wrap: wrap;
        margin-top: 18px;
      }
    `,
  ],
})
export class NoCompanyComponent {
  readonly auth = inject(AuthService);
}
