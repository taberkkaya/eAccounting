import { Component, OnInit } from '@angular/core';
import { SharedModule } from '../../modules/shared.module';
import { HttpService } from '../../services/http.service';
import { DemoVisitorModel } from '../../models/demoVisitor.model';

@Component({
  selector: 'app-demo-visitors',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './demo-visitors.component.html',
})
export class DemoVisitorsComponent implements OnInit {
  visitors: DemoVisitorModel[] = [];
  search: string = '';
  loading = true;

  constructor(private http: HttpService) {}

  ngOnInit(): void {
    this.getAll();
  }

  getAll() {
    this.loading = true;

    this.http.post<DemoVisitorModel[]>(
      'DemoVisitors/GetAll',
      {},
      (res) => {
        this.visitors = res;
        this.loading = false;
      },
      () => (this.loading = false)
    );
  }

  /** Adres ya da IP üzerinde arama; liste kısa olduğu için istemcide filtreleniyor. */
  get filtered(): DemoVisitorModel[] {
    const term = this.search.trim().toLocaleLowerCase('tr');
    if (!term) return this.visitors;

    return this.visitors.filter(
      (v) =>
        v.email.toLocaleLowerCase('tr').includes(term) ||
        (v.ipAddress ?? '').includes(term) ||
        (v.country ?? '').toLocaleLowerCase('tr').includes(term) ||
        (v.city ?? '').toLocaleLowerCase('tr').includes(term)
    );
  }

  get verifiedCount(): number {
    return this.visitors.filter((v) => v.isVerified).length;
  }

  get sessionTotal(): number {
    return this.visitors.reduce((total, v) => total + v.sessionCount, 0);
  }
}
