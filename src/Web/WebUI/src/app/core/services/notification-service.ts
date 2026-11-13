import { Injectable } from '@angular/core';
import Swal from 'sweetalert2';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  confirmHtml(title: string, html: string, confirmButtonText: string, cancelButtonText: string) {
    return Swal.fire({
      title,
      html,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33',
      confirmButtonText,
      cancelButtonText,
      customClass: { popup: 'notificacion-confirm' },
    });
  }

  alert(title: string, text: string) {
    return Swal.fire({ title, text, icon: 'warning', confirmButtonText: 'Aceptar' });
  }

  confirm(title: string, message: string, confirmButtonText: string, cancelButtonText: string) {
    return Swal.fire({
      title,
      text: message,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33',
      confirmButtonText,
      cancelButtonText,
      customClass: { popup: 'notificacion-confirm' },
    });
  }
}
