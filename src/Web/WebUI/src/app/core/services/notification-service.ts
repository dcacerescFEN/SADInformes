import { Injectable } from '@angular/core';
import Swal from 'sweetalert2';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  showSuccess(title: string, message: string) {
    void Swal.fire({
      title,
      text: message,
      showConfirmButton: false,
      showCloseButton: true,
      timer: 3000,
      toast: true,
      position: 'top-end',
      customClass: { popup: 'notificacion-success' },
    });
  }

  showError(title: string, message: string) {
    void Swal.fire({
      title,
      text: message,
      showConfirmButton: false,
      showCloseButton: true,
      timer: 3000,
      toast: true,
      position: 'top-end',
      customClass: { popup: 'notificacion-error' },
    });
  }

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
