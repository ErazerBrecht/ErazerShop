import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { merge, of, Subject } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { AppConfigService } from './app.config';

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
    switchMap(() => this.http.get(`${this.appConfig.config.api}/user/info`, { withCredentials: true }).pipe(catchError(x => of(x.message)))));
  private onBackEndPushed$ = this.backEndPushed$.pipe(
    switchMap(() => this.http.get(`${this.appConfig.config.api}/api/WeatherForecast`, { withCredentials: true }).pipe(catchError(x => of(x.message)))));

  public result$ = merge(this.onFrontEndPushed$, this.onBackEndPushed$);

  constructor(private http: HttpClient, private appConfig: AppConfigService) {
  }

}
