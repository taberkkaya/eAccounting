import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../services/auth.service';
import { HttpService } from '../../../services/http.service';
import { LoginResponseModel } from '../../../models/login.response.model';
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
  @Input() sidebarCollapsed = false;
  @Output() menuToggled = new EventEmitter<void>();

  constructor(
    private router: Router,
    public auth: AuthService,
    private http: HttpService,
    private demo: DemoService
  ) {}

  logout() {
    // Demo oturumunda sandbox'ı beklemeden iade ediyoruz.
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
