import Swal from 'sweetalert2';
import { NotificationService } from './notification-service';

describe('NotificationService', () => {
  afterEach(() => vi.restoreAllMocks());

  it('uses the SADAlumnos SweetAlert confirmation and returns its asynchronous result', async () => {
    const result = { isConfirmed: false, isDenied: false, isDismissed: true };
    const fire = vi.spyOn(Swal, 'fire').mockResolvedValue(result);
    const service = new NotificationService();
    expect(await service.confirm('Confirmar inscripción', 'Desea inscribir a los alumnos seleccionados?',
      'Sí, inscribir', 'Cancelar')).toEqual(result);
    expect(fire).toHaveBeenCalledExactlyOnceWith({
      title: 'Confirmar inscripción', text: 'Desea inscribir a los alumnos seleccionados?',
      icon: 'warning', showCancelButton: true, confirmButtonColor: '#3085d6', cancelButtonColor: '#d33',
      confirmButtonText: 'Sí, inscribir', cancelButtonText: 'Cancelar',
      customClass: { popup: 'notificacion-confirm' },
    });
  });
});
