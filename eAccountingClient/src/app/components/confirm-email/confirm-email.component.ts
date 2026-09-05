import { Component } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpService } from '../../services/http.service';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="confirm">
      <img src="assets/ak-logo.png" alt="AK" class="confirm__logo" />
      <h1 class="confirm__title">{{ response }}</h1>
      <a routerLink="/login" class="ak-btn ak-btn--primary">
        Giriş sayfasına dön
      </a>
    </div>
  `,
  styles: [
    `
      .confirm {
        min-height: 100vh;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 20px;
        padding: 24px;
        text-align: center;
      }

      .confirm__logo {
        width: 52px;
        height: 52px;
      }

      .confirm__title {
        max-width: 520px;
        font-size: 1.3rem;
        text-transform: uppercase;
        line-height: 1.3;
      }
    `,
  ],
})
export class ConfirmEmailComponent {
  email: string | undefined = '';
  token: string | undefined = '';
  response: string = 'E-posta doğrulanıyor...';

  constructor(private route: ActivatedRoute, private http: HttpService) {
    this.route.params.subscribe(() => {
      this.email = this.route.snapshot.queryParamMap.get('email')?.toString();
      this.token = this.route.snapshot.queryParamMap.get('token')?.toString();
      this.confirm();
    });
  }

  confirm() {
    this.http.post<string>(
      'Auth/ConfirmEmail',
      { email: this.email, token: this.token },
      (res) => {
        this.response = res;
      },
      () => {
        this.response = 'E-posta doğrulanamadı. Bağlantı geçersiz olabilir.';
      }
    );
  }
}
