import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './navbar/navbar.component';
import { MainSidebarComponent } from './main-sidebar/main-sidebar.component';
import { FooterComponent } from './footer/footer.component';
import { ControlSidebarComponent } from './control-sidebar/control-sidebar.component';
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
    ControlSidebarComponent,
    DemoPromptComponent,
  ],
  templateUrl: './layouts.component.html',
  styleUrl: './layouts.component.css',
})
export class LayoutsComponent implements OnInit {
  private readonly demo = inject(DemoService);

  ngOnInit(): void {
    // A reload keeps the token, so the quota has to be picked back up from the API
    // rather than assumed from what the banner last showed.
    this.demo.refreshStatus();
  }
}
