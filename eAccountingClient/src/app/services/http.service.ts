import {
  HttpClient,
  HttpErrorResponse,
  HttpResponse,
} from '@angular/common/http';
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

  /**
   * Dosya indirir. Uç noktalar token istediği için doğrudan bağlantı verilemiyor;
   * yanıt blob olarak alınıp tarayıcıya geçici bir bağlantıyla sunuluyor.
   */
  download(
    apiUrl: string,
    body: any,
    fallbackFileName: string,
    callBack?: () => void,
    errorCallBack?: () => void
  ) {
    this.http
      .post(`${api()}/${apiUrl}`, body, {
        observe: 'response',
        responseType: 'blob',
      })
      .subscribe({
        next: (res: HttpResponse<Blob>) => {
          if (res.body) {
            this.saveBlob(res.body, this.readFileName(res) ?? fallbackFileName);
          }

          if (callBack) {
            callBack();
          }
        },
        error: async (err: HttpErrorResponse) => {
          // Hata gövdesi de blob olarak geliyor; mesajın okunabilmesi için
          // normal bir yanıt gibi çözülüyor.
          this.error.errorHandler(await this.readBlobError(err));

          if (errorCallBack) {
            errorCallBack();
          }
        },
      });
  }

  private readFileName(res: HttpResponse<Blob>): string | null {
    const disposition = res.headers.get('Content-Disposition');
    if (!disposition) return null;

    const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);

    return match ? decodeURIComponent(match[1]) : null;
  }

  private saveBlob(blob: Blob, fileName: string) {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.href = url;
    link.download = fileName;
    link.click();

    URL.revokeObjectURL(url);
  }

  private async readBlobError(err: HttpErrorResponse): Promise<HttpErrorResponse> {
    if (!(err.error instanceof Blob)) return err;

    try {
      return new HttpErrorResponse({
        error: JSON.parse(await err.error.text()),
        status: err.status,
        statusText: err.statusText,
      });
    } catch {
      return err;
    }
  }
}
