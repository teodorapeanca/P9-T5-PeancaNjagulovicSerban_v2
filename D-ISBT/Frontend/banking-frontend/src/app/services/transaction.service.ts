import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TransferByIbanRequest } from '../models/transfer-by-iban.model';

@Injectable({
  providedIn: 'root'
})
export class TransactionService {
  private apiUrl = 'https://localhost:7137/api/Transactions';

  constructor(private http: HttpClient) {}
  deposit(data: any) {
  const token = localStorage.getItem('token');

  return this.http.post(`${this.apiUrl}/deposit`, data, {
    headers: { Authorization: `Bearer ${token}` }
  });
}

  withdraw(data: any) {
  const token = localStorage.getItem('token');

  return this.http.post(`${this.apiUrl}/withdraw`, data, {
    headers: { Authorization: `Bearer ${token}` }
  });
}
  transferByIban(data: TransferByIbanRequest) {
    const token = localStorage.getItem('token');

    return this.http.post(`${this.apiUrl}/transfer-by-iban`, data, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  servicePayment(data: any) {
    const token = localStorage.getItem('token');

    return this.http.post(`${this.apiUrl}/service-payment`, data, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }
  repeatTransaction(data: any) {
  const token = localStorage.getItem('token');

  return this.http.post(`${this.apiUrl}/repeat`, data, {
    headers: {
      Authorization: `Bearer ${token}`
    }
  });
  }

  getMyHistory() {
    const token = localStorage.getItem('token');

    return this.http.get(`${this.apiUrl}/my-history`, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }
  
getRetentionPolicy() {
  const token = localStorage.getItem('token');

  return this.http.get(`${this.apiUrl}/retention-policy`, {
    headers: {
      Authorization: `Bearer ${token}`
    }
  });
}

updateRetentionPolicy(days: number) {
  const token = localStorage.getItem('token');

  return this.http.put(`${this.apiUrl}/retention-policy/${days}`, {}, {
    headers: {
      Authorization: `Bearer ${token}`
    }
  });
}

getTransactionHelp() {
  const token = localStorage.getItem('token');

  return this.http.get(`${this.apiUrl}/help`, {
    headers: {
      Authorization: `Bearer ${token}`
    }
  });

}


  getTransactionDetails(id: number) {
    const token = localStorage.getItem('token');

    return this.http.get(`${this.apiUrl}/${id}/details`, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  exportMyHistory() {
    const token = localStorage.getItem('token');

    return this.http.get(`${this.apiUrl}/my-history/export`, {
      headers: {
        Authorization: `Bearer ${token}`
      },
      responseType: 'blob'
    });
  }
  getAllTransactions() {
  const token = localStorage.getItem('token');

  return this.http.get(`${this.apiUrl}`, {
    headers: {
      Authorization: `Bearer ${token}`
    }
  });
}
}