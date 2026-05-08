import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class AuditService {
  private apiUrl = 'https://localhost:7137/api/Audit';

  constructor(private http: HttpClient) {}

  getAuditLogs() {
    const token = localStorage.getItem('token');

    return this.http.get(this.apiUrl, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  filterAuditLogs(data: any) {
    const token = localStorage.getItem('token');

    return this.http.post(`${this.apiUrl}/filter`, data, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  exportAuditLogs() {
    const token = localStorage.getItem('token');

    return this.http.get(`${this.apiUrl}/export`, {
      headers: {
        Authorization: `Bearer ${token}`
      },
      responseType: 'blob'
    });
  }

  verifyHash(id: number) {
    const token = localStorage.getItem('token');

    return this.http.get(`${this.apiUrl}/${id}/verify-hash`, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }
}