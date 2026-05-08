import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NotificationService } from '../../services/notification.service';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './notifications.html',
  styleUrl: './notifications.css'
})
export class Notifications implements OnInit {
  notifications: any[] = [];
  message = '';

  notificationsEnabled = true;
  notificationLevel = 'Low';

  constructor(
    private notificationService: NotificationService,
    private userService: UserService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadUserPreferences();
    this.loadNotifications();
  }

  loadUserPreferences() {
    const userId = Number(localStorage.getItem('userId'));

    this.userService.getUserById(userId).subscribe({
      next: (res: any) => {
        this.notificationsEnabled = res.notificationsEnabled;
        this.notificationLevel = res.notificationLevel || 'Low';
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('USER PREFERENCES ERROR:', err);
      }
    });
  }

  loadNotifications() {
    this.notificationService.getMyNotifications().subscribe({
      next: (res: any) => {
        if (Array.isArray(res)) {
          this.notifications = res;
        } else if (res?.data && Array.isArray(res.data)) {
          this.notifications = res.data;
        } else {
          this.notifications = [];
        }

        this.message = '';
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('NOTIFICATIONS ERROR:', err);
        this.message = 'Eroare la încărcarea notificărilor.';
        this.cdr.detectChanges();
      }
    });
  }

  toggleNotifications() {
    this.userService.toggleNotifications({
      enabled: this.notificationsEnabled
    }).subscribe({
      next: () => {
        this.message = this.notificationsEnabled
          ? 'Notificările au fost activate.'
          : 'Notificările au fost dezactivate.';

        this.loadUserPreferences();
      },
      error: (err: any) => {
        console.log('TOGGLE NOTIFICATIONS ERROR:', err);
        this.message = 'Eroare la modificarea preferinței pentru notificări.';
      }
    });
  }

  updateNotificationLevel() {
    if (!this.notificationsEnabled) {
      return;
    }

    this.userService.updateNotificationLevel({
      notificationLevel: this.notificationLevel
    }).subscribe({
      next: () => {
        this.message = 'Nivelul de notificare a fost actualizat.';
        this.loadUserPreferences();
      },
      error: (err: any) => {
        console.log('NOTIFICATION LEVEL ERROR:', err);
        this.message = 'Eroare la actualizarea nivelului de notificare.';
      }
    });
  }

  markAsRead(notification: any) {
    if (notification.isRead) {
      return;
    }

    this.notificationService.markAsRead(notification.notificationId).subscribe({
      next: () => {
        notification.isRead = true;
      },
      error: (err: any) => {
        console.log('MARK AS READ ERROR:', err);
      }
    });
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}