import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { api } from '../constants';
import { ResultModel } from '../models/result.model';
import { ErrorService } from './error.service';

@Injectable({
  providedIn: 'root',
})
export class HttpService {
  // The bearer token is attached by authInterceptor, so nothing here deals with headers.
  constructor(private http: HttpClient, private error: ErrorService) {}

  get<T>(
    apiUrl: string,
    callBack: (res: T) => void,
    errorCallBack?: () => void
  ) {
    this.http.get<ResultModel<T>>(`${api()}/${apiUrl}`).subscribe({
      next: (res) => {
        if (res.data) {
          callBack(res.data);
        }
      },
      error: (err: HttpErrorResponse) => {
        this.error.errorHandler(err);

        if (errorCallBack) {
          errorCallBack();
        }
      },
    });
  }

  post<T>(
    apiUrl: string,
    body: any,
    callBack: (res: T) => void,
    errorCallBack?: () => void
  ) {
    this.http.post<ResultModel<T>>(`${api()}/${apiUrl}`, body).subscribe({
      next: (res) => {
        if (res.data) {
          callBack(res.data);
        }
      },
      error: (err: HttpErrorResponse) => {
        this.error.errorHandler(err);

        if (errorCallBack) {
          errorCallBack();
        }
      },
    });
  }
}
