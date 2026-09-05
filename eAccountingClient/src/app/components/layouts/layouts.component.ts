import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './navbar/navbar.component';
import { MainSidebarComponent } from './main-sidebar/main-sidebar.component';
import { FooterComponent } from './footer/footer.component';
import { DemoPromptComponent } from '../demo/demo-prompt/demo-prompt.component';
import { DemoService } from '../../services/demo.service';

@Component({
  selector: 'app-layouts',
  standalone: true,
  imports: [
    RouterOutlet,
    NavbarComponent,
    MainSidebarComponent,
    FooterComponent,
    DemoPromptComponent,
  ],
  templateUrl: './layouts.component.html',
  styleUrl: './layouts.component.css',
})
export class LayoutsComponent implements OnInit {
  private readonly demo = inject(DemoService);

  /** Yalnızca dar ekranlarda anlamlı; geniş ekranda kenar çubuğu hep açık. */
  readonly sidebarOpen = signal(false);

  ngOnInit(): void {
    // Sayfa yenilenince token duruyor ama kota bilgisi gitmiş oluyor.
    this.demo.refreshStatus();
  }

  toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }
}
