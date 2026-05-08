import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private apiUrl = 'https://localhost:7137/api/Auth';

  constructor(private http: HttpClient) {}

  login(data: any) {
    return this.http.post(`${this.apiUrl}/login`, data);
  }

  verify2FA(data: any) {
    return this.http.post(`${this.apiUrl}/verify-2fa`, data);
  }

  register(data: any) {
    return this.http.post(`${this.apiUrl}/register`, data);
  }

  forgotPassword(data: any) {
    return this.http.post(`${this.apiUrl}/forgot-password`, data);
  }

  resetPassword(data: any) {
    return this.http.post(`${this.apiUrl}/reset-password`, data);
  }
}