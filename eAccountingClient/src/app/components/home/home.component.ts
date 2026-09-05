import { Component, inject } from '@angular/core';
import { SharedModule } from '../../modules/shared.module';
import { DemoService } from '../../services/demo.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent {
  readonly demo = inject(DemoService);
  readonly auth = inject(AuthService);

  readonly stack: string[] = [
    '.NET 9 Web API',
    'Angular 18',
    'Clean Architecture',
    'CQRS / MediatR',
    'EF Core',
    'SQL Server',
    'JWT',
    'Docker',
  ];

  contact(): void {
    this.demo.openContactPage();
  }
}
