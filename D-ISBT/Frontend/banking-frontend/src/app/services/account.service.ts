import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Account } from '../models/account.model';
import { CreateAccountRequest } from '../models/create-account-request.model';

@Injectable({
  providedIn: 'root'
})
export class AccountService {

  private apiUrl = 'https://localhost:7137/api/Accounts';

  constructor(private http: HttpClient) {}

  //  GET conturile clientului
  getMyAccounts() {
    const token = localStorage.getItem('token');

    return this.http.get<Account[]>(`${this.apiUrl}/my`, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }
  getAllAccounts() {
  const token = localStorage.getItem('token');

  return this.http.get(`${this.apiUrl}`, {
    headers: {
      Authorization: `Bearer ${token}`
    }
  });
}

  //  CREATE cont nou
  createAccount(data: CreateAccountRequest) {
    const token = localStorage.getItem('token');

    return this.http.post(`${this.apiUrl}`, data, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  //  GET conturi pending (admin)
  getPendingAccounts() {
    const token = localStorage.getItem('token');

    return this.http.get<Account[]>(`${this.apiUrl}/pending`, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  //  APPROVE cont (admin)
  approveAccount(id: number) {
    const token = localStorage.getItem('token');

    return this.http.put(`${this.apiUrl}/${id}/approve`, {}, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  //  DEACTIVATE cont (admin)
  deactivateAccount(id: number) {
    const token = localStorage.getItem('token');

    return this.http.put(`${this.apiUrl}/${id}/deactivate`, {}, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  //  REACTIVATE cont (admin)
  reactivateAccount(id: number) {
    const token = localStorage.getItem('token');

    return this.http.put(`${this.apiUrl}/${id}/reactivate`, {}, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

}