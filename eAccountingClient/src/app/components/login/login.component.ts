import { Component } from '@angular/core';
import { SharedModule } from '../../modules/shared.module';
import { LoginModel } from '../../models/login.model';
import { HttpService } from '../../services/http.service';
import { LoginResponseModel } from '../../models/login.response.model';
import { Router } from '@angular/router';
import { SwalService } from '../../services/swal.service';
import { DemoService } from '../../services/demo.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent {
  model: LoginModel = new LoginModel();
  email: string = '';
  isLoading: boolean = false;
  isDemoLoading: boolean = false;
  confirmMailOpen = false;

  constructor(
    private http: HttpService,
    private router: Router,
    private swal: SwalService,
    private demo: DemoService
  ) {}

  startDemo() {
    this.isDemoLoading = true;

    this.demo.start().subscribe({
      next: () => {
        this.isDemoLoading = false;
        this.router.navigateByUrl('/');
      },
      error: () => {
        this.isDemoLoading = false;
        this.swal.callToast(
          'Şu anda tüm demo oturumları dolu, birazdan tekrar deneyin.',
          'error'
        );
      },
    });
  }

  signIn() {
    this.isLoading = true;
    this.http.post<LoginResponseModel>(
      'Auth/Login',
      this.model,
      (res) => {
        // Önceki ziyaretten kalan demo işaretini temizler.
        this.demo.clear();
        localStorage.setItem('accessToken', res.accessToken);
        this.router.navigateByUrl('/');
      },
      () => (this.isLoading = false)
    );
  }

  sendConfirmEmail() {
    this.http.post<string>(
      'Auth/SendConfirmEmail',
      { email: this.email },
      (res) => {
        this.swal.callToast(res, 'info');
        this.confirmMailOpen = false;
        this.email = '';
      },
      () => {
        this.confirmMailOpen = false;
        this.email = '';
      }
    );
  }
}
