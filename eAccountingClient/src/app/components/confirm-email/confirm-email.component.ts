import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HttpService } from '../../services/http.service';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [],
  template: `
    <div
      style="height: 90vh; display: flex; align-items:center; justify-content:center; flex-direction:column"
    >
      <h1>{{ response }}</h1>
      <a href="/login">Giriş sayfasına dönmek için tıklayın.</a>
    </div>
  `,
})
export class ConfirmEmailComponent {
  email: string | undefined = '';
  token: string | undefined = '';
  response: string = 'response';
  constructor(private route: ActivatedRoute, private http: HttpService) {
    this.route.params.subscribe((res) => {
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
      }
    );
  }
}
