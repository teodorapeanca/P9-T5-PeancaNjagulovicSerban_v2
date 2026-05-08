import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = 'https://localhost:7137/api/Users';

  constructor(private http: HttpClient) {}

  getUsers() {
    const token = localStorage.getItem('token');

    return this.http.get(this.apiUrl, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  getUserById(id: number) {
    const token = localStorage.getItem('token');

    return this.http.get(`${this.apiUrl}/${id}`, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  updateUser(id: number, data: any) {
    const token = localStorage.getItem('token');

    return this.http.put(`${this.apiUrl}/${id}`, data, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  updateNotificationLevel(data: any) {
    const token = localStorage.getItem('token');

    return this.http.put(
      `${this.apiUrl}/notification-level`,
      data,
      {
        headers: {
          Authorization: `Bearer ${token}`
        }
      }
    );
  }

  toggleNotifications(data: any) {
    const token = localStorage.getItem('token');

    return this.http.put(
      `${this.apiUrl}/notifications/toggle`,
      data,
      {
        headers: {
          Authorization: `Bearer ${token}`
        }
      }
    );
  }
}