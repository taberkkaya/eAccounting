import { Component, inject } from '@angular/core';
import { SharedModule } from '../../modules/shared.module';
import { RouterLink } from '@angular/router';
import { DemoService } from '../../services/demo.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [SharedModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent {
  readonly demo = inject(DemoService);

  readonly stack: string[] = [
    '.NET 9 Web API',
    'Angular 18',
    'Clean Architecture',
    'CQRS / MediatR',
    'EF Core',
    'SQL Server',
    'JWT',
    'Veritabanı başına firma',
  ];

  contact(): void {
    this.demo.openContactPage();
  }
}
