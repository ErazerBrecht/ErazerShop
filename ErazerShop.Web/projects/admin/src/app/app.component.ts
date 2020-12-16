import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { merge, Subject } from 'rxjs';
import { switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  public title = 'ErazerShop.Admin';
  public frontEndPushed$ = new Subject<void>();
  public backEndPushed$ = new Subject<void>();

  private onFrontEndPushed$ = this.frontEndPushed$.pipe(
    switchMap(() => this.http.get('https://localhost:9999/user/info')));
  private onBackEndPushed$ = this.backEndPushed$.pipe(
    switchMap(() => this.http.get('https://localhost:9999/api/WeatherForecast')));

  public result$ = merge(this.onFrontEndPushed$, this.onBackEndPushed$);

  constructor(private http: HttpClient) {
  }

}
