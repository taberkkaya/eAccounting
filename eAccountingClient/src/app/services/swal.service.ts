import { Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { FlexiToastIconType, FlexiToastService } from 'flexi-toast';

@Injectable({
  providedIn: 'root'
})
export class SwalService {

  constructor(
    private toast: FlexiToastService
  ) { }

  callToast(text: string = "İşlem başarılı", icon: FlexiToastIconType = "success") {
    this.toast.showToast(this.titleFor(icon), text, icon);
  }

  callSwal(title: string, text: string, callBack: () => void, confirmButtonText: string = "Sil", cancelButtonText: string = "Vazgeç") {
    this.toast.showSwal(title, text, () => callBack(), confirmButtonText, cancelButtonText);
  }

  /** Bildirim başlıkları ikon türünden geliyordu; Türkçe karşılıklarına eşliyoruz. */
  private titleFor(icon: FlexiToastIconType): string {
    switch (icon) {
      case 'error':
        return 'Hata';
      case 'info':
        return 'Bilgi';
      case 'warning':
        return 'Uyarı';
      default:
        return 'Başarılı';
    }
  }
}