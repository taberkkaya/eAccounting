import { Component, ElementRef, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { HttpService } from '../../../services/http.service';
import { LoginResponseModel } from '../../../models/login.response.model';
import { FormsModule } from '@angular/forms';
import { DemoBannerComponent } from '../../demo/demo-banner/demo-banner.component';
import { DemoService } from '../../../services/demo.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [FormsModule, DemoBannerComponent],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css',
})
export class NavbarComponent {
  constructor(
    private router: Router,
    public auth: AuthService,
    private http: HttpService,
    private demo: DemoService
  ) {}

  logout() {
    // Hands the sandbox back straight away instead of waiting for it to time out.
    if (this.demo.isDemo) {
      this.demo.exit();
      return;
    }

    localStorage.clear();
    this.router.navigateByUrl('/login');
  }

  changeCompany() {
    this.http.post<LoginResponseModel>(
      'Auth/ChangeCompany',
      {
        companyId: this.auth.user.companyId,
      },
      (res) => {
        localStorage.clear();
        localStorage.setItem('accessToken', res.accessToken);

        document.location.reload();
      }
    );
  }
}
