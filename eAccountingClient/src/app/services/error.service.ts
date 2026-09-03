import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { SwalService } from './swal.service';

@Injectable({
  providedIn: 'root',
})
export class ErrorService {
  constructor(private swal: SwalService) {}

  errorHandler(err: HttpErrorResponse) {
    // Demo rejections that end a session are surfaced by the demo prompt instead of a
    // toast, so they are not reported twice.
    if (err.error?.demoCode && err.error.demoCode !== 'action_blocked') return;

    switch (err.status) {
      case 0:
        this.swal.callToast('API adresine ulaşılamıyor', 'error');
        break;

      case 401:
        this.swal.callToast('Oturumunuzun süresi doldu', 'error');
        break;

      case 404:
        this.swal.callToast('API adresi bulunamadı', 'error');
        break;

      case 429:
        this.swal.callToast('Çok fazla istek gönderildi, lütfen biraz bekleyin', 'error');
        break;

      default:
        this.swal.callToast(this.readMessage(err), 'error');
        break;
    }
  }

  /** The API returns errorMessages, but older responses used ErrorMessages. */
  private readMessage(err: HttpErrorResponse): string {
    const messages: string[] | undefined =
      err.error?.errorMessages ?? err.error?.ErrorMessages;

    if (messages?.length) return messages.join('\n');

    return err.error?.message ?? 'Beklenmeyen bir hata oluştu';
  }
}
