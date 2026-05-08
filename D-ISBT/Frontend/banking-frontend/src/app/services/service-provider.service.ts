import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ServiceProviderService {

  private apiUrl = 'https://localhost:7137/api/ServiceProviders';

  constructor(private http: HttpClient) {}

  getProviders() {
    const token = localStorage.getItem('token');

    return this.http.get(this.apiUrl, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }
}