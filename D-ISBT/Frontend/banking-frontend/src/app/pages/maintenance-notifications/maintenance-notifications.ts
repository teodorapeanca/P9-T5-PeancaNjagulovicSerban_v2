import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-maintenance-notifications',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './maintenance-notifications.html',
  styleUrl: './maintenance-notifications.css'
})
export class MaintenanceNotifications {

  title = '';
  message = '';

  successMessage = '';
  errorMessage = '';

  loading = false;

  constructor(
    private notificationService: NotificationService,
    private router: Router
  ) {}

  sendNotification() {

    if (!this.title || !this.message) {
      this.errorMessage = 'Completați toate câmpurile.';
      return;
    }

    this.loading = true;

    this.notificationService.sendMaintenanceNotification({
      title: this.title,
      message: this.message
    }).subscribe({
      next: (res: any) => {

        this.successMessage =
          res?.message ||
          'Notificările de mentenanță au fost trimise.';

        this.errorMessage = '';

        this.title = '';
        this.message = '';

        this.loading = false;
      },

      error: (err: any) => {

        console.log('MAINTENANCE NOTIFICATION ERROR:', err);

        this.errorMessage =
          err?.error?.message ||
          'Eroare la trimiterea notificărilor.';

        this.successMessage = '';

        this.loading = false;
      }
    });
  }

  goBack() {
  this.router.navigate(['/admin-access']);
}
}