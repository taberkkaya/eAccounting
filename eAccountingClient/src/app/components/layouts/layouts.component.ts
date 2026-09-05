import { Component, HostListener, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './navbar/navbar.component';
import { MainSidebarComponent } from './main-sidebar/main-sidebar.component';
import { FooterComponent } from './footer/footer.component';
import { DemoPromptComponent } from '../demo/demo-prompt/demo-prompt.component';
import { DemoService } from '../../services/demo.service';

/** Kenar çubuğunun çekmeceye dönüştüğü genişlik; styles.css ile aynı olmalı. */
const MOBILE_BREAKPOINT = 900;

const COLLAPSED_KEY = 'sidebarCollapsed';

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

  /** Dar ekranda çekmecenin açık olup olmadığı. */
  readonly sidebarOpen = signal(false);

  /** Geniş ekranda çubuğun ikon şeridine inip inmediği. */
  readonly sidebarCollapsed = signal(this.readCollapsed());

  ngOnInit(): void {
    // Sayfa yenilenince token duruyor ama kota bilgisi gitmiş oluyor.
    this.demo.refreshStatus();
  }

  /**
   * Tek bir düğme iki işi görüyor: dar ekranda çekmeceyi açıp kapatıyor, geniş
   * ekranda çubuğu daraltıyor.
   */
  toggleSidebar(): void {
    if (this.isMobile()) {
      this.sidebarOpen.update((open) => !open);
      return;
    }

    this.sidebarCollapsed.update((collapsed) => {
      const next = !collapsed;
      this.writeCollapsed(next);
      return next;
    });
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeSidebar();
  }

  /** Ekran genişleyince açık kalmış çekmece içeriğin üstünde asılı kalmasın. */
  @HostListener('window:resize')
  onResize(): void {
    if (!this.isMobile()) this.closeSidebar();
  }

  private isMobile(): boolean {
    return window.matchMedia(`(max-width: ${MOBILE_BREAKPOINT}px)`).matches;
  }

  private readCollapsed(): boolean {
    return localStorage.getItem(COLLAPSED_KEY) === 'true';
  }

  private writeCollapsed(collapsed: boolean): void {
    localStorage.setItem(COLLAPSED_KEY, String(collapsed));
  }
}
