import { Component, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { SharedModule } from '../../modules/shared.module';
import { LoginModel } from '../../models/login.model';
import { HttpService } from '../../services/http.service';
import { LoginResponseModel } from '../../models/login.response.model';
import { Router } from '@angular/router';
import { SwalService } from '../../services/swal.service';
import { DemoService } from '../../services/demo.service';
import { ModalComponent } from '../ui/modal/modal.component';

/** Demoya giriş akışının hangi adımında olduğumuz. */
type DemoStep = 'email' | 'code';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [SharedModule, ModalComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent implements OnInit {
  model: LoginModel = new LoginModel();
  email: string = '';
  isLoading: boolean = false;
  isDemoLoading: boolean = false;
  confirmMailOpen = false;

  /** Sunucu doğrulama istemiyorsa (mail yapılandırılmamışsa) akış tek tıka iner. */
  demoNeedsVerification = false;

  demoOpen = false;
  demoStep: DemoStep = 'email';
  demoEmail = '';
  demoCode = '';
  demoNote = '';

  constructor(
    private http: HttpService,
    private router: Router,
    private swal: SwalService,
    private demo: DemoService
  ) {}

  ngOnInit(): void {
    this.demo.config().subscribe({
      next: (res) => {
        this.demoNeedsVerification = res.data?.emailVerificationRequired ?? false;
      },
      // Yapılandırma okunamazsa doğrudan başlatmayı dener; sunucu gerekiyorsa
      // zaten reddeder ve sebebini söyler.
      error: () => (this.demoNeedsVerification = false),
    });
  }

  // --- demo akışı ---------------------------------------------------------

  startDemo() {
    if (this.demoNeedsVerification) {
      this.demoStep = 'email';
      this.demoCode = '';
      this.demoNote = '';
      this.demoOpen = true;
      return;
    }

    this.runDemoStart();
  }

  sendDemoCode() {
    if (!this.demoEmail.trim()) {
      this.swal.callToast('E-posta adresinizi yazın.', 'error');
      return;
    }

    this.isDemoLoading = true;

    this.demo.requestCode(this.demoEmail).subscribe({
      next: (res) => {
        this.isDemoLoading = false;
        this.demoNote = res.data ?? '';
        this.demoStep = 'code';
      },
      error: (err: HttpErrorResponse) => {
        this.isDemoLoading = false;
        this.swal.callToast(this.demoError(err), 'error');
      },
    });
  }

  verifyAndStart() {
    if (!this.demoCode.trim()) {
      this.swal.callToast('Mailinize gelen kodu yazın.', 'error');
      return;
    }

    this.runDemoStart(this.demoEmail, this.demoCode);
  }

  backToEmail() {
    this.demoStep = 'email';
    this.demoCode = '';
    this.demoNote = '';
  }

  private runDemoStart(email = '', code = '') {
    this.isDemoLoading = true;

    this.demo.start(email, code).subscribe({
      next: () => {
        this.isDemoLoading = false;
        this.demoOpen = false;
        this.router.navigateByUrl('/');
      },
      error: (err: HttpErrorResponse) => {
        this.isDemoLoading = false;
        this.swal.callToast(this.demoError(err), 'error');
      },
    });
  }

  /**
   * Demo açılmadığında sebebi söyler. Eskiden her hata "tüm oturumlar dolu"
   * diye görünüyordu; sunucuya hiç ulaşılamadığında bile öyle yazdığı için
   * demonun tek kişilik olduğu izlenimi veriyordu.
   */
  private demoError(err: HttpErrorResponse): string {
    if (err.status === 0)
      return 'Sunucuya ulaşılamadı. Bağlantınızı kontrol edip tekrar deneyin.';

    if (err.status === 404) return 'Demo şu anda kapalı.';

    if (err.status === 429)
      return 'Çok fazla kod istediniz. Bir süre sonra tekrar deneyin.';

    // Diğer durumlarda sunucunun kendi açıklaması daha isabetli: kod hatalı
    // olabilir, ortam hazırlanıyor olabilir ya da gerçekten boş alan kalmamış
    // olabilir.
    const messages: string[] | undefined = err.error?.errorMessages;
    if (messages?.length) return messages.join(' ');

    return 'Demo başlatılamadı, birazdan tekrar deneyin.';
  }

  // --- normal giriş -------------------------------------------------------

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
