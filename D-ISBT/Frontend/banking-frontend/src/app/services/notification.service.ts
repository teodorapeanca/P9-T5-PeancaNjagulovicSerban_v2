import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private apiUrl = 'https://localhost:7137/api/Notifications';

  constructor(private http: HttpClient) {}

  sendMaintenanceNotification(data: any) {
  const token = localStorage.getItem('token');

  return this.http.post(
    `${this.apiUrl}/maintenance`,
    data,
    {
      headers: {
        Authorization: `Bearer ${token}`
      }
    }
  );
}

  getMyNotifications() {
    const token = localStorage.getItem('token');

    return this.http.get(`${this.apiUrl}/my`, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  getMyUnreadNotifications() {
    const token = localStorage.getItem('token');

    return this.http.get(`${this.apiUrl}/my/unread`, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  markAsRead(notificationId: number) {
    const token = localStorage.getItem('token');

    return this.http.put(`${this.apiUrl}/${notificationId}/read`, {}, {
      headers: {
        Authorization: `Bearer ${token}`
      }
      
    });
  }
}