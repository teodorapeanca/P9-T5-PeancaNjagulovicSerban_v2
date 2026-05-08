import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AccountService } from '../../services/account.service';
import { TransactionService } from '../../services/transaction.service';
import { ServiceProviderService } from '../../services/service-provider.service';

@Component({
  selector: 'app-service-payment',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './service-payment.html',
  styleUrl: './service-payment.css'
})
export class ServicePayment implements OnInit {
  accounts: any[] = [];
  providers: any[] = [];
  message = '';

  servicePaymentData = {
    initiatedByUserId: 0,
    fromIban: '',
    providerId: 0,
    amount: 0,
    currency: 'RON',
    description: ''
  };

  constructor(
    private accountService: AccountService,
    private transactionService: TransactionService,
    private providerService: ServiceProviderService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadAccounts();
    this.loadProviders();
  }

  loadAccounts() {
    this.accountService.getMyAccounts().subscribe({
      next: (res: any) => {
        if (Array.isArray(res)) {
          this.accounts = res;
        } else if (res?.data && Array.isArray(res.data)) {
          this.accounts = res.data;
        } else if (res?.accounts && Array.isArray(res.accounts)) {
          this.accounts = res.accounts;
        } else if (res && Object.keys(res).length > 0) {
          this.accounts = [res];
        } else {
          this.accounts = [];
        }

        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('SERVICE PAYMENT ACCOUNTS ERROR:', err);
        this.message = 'Eroare la încărcarea conturilor.';
        this.cdr.detectChanges();
      }
    });
  }

  loadProviders() {
    this.providerService.getProviders().subscribe({
      next: (res: any) => {
        if (Array.isArray(res)) {
          this.providers = res;
        } else if (res?.data && Array.isArray(res.data)) {
          this.providers = res.data;
        } else if (res && Object.keys(res).length > 0) {
          this.providers = [res];
        } else {
          this.providers = [];
        }

        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('PROVIDERS ERROR:', err);
        this.message = 'Eroare la încărcarea furnizorilor.';
        this.cdr.detectChanges();
      }
    });
  }

  payService() {
    const userId = Number(localStorage.getItem('userId'));
    this.servicePaymentData.initiatedByUserId = userId;

    if (!this.servicePaymentData.fromIban) {
      this.message = 'Selectează contul sursă.';
      return;
    }

    if (!this.servicePaymentData.providerId || this.servicePaymentData.providerId <= 0) {
      this.message = 'Selectează furnizorul.';
      return;
    }

    if (this.servicePaymentData.amount <= 0) {
      this.message = 'Suma trebuie să fie mai mare decât 0.';
      return;
    }

    this.transactionService.servicePayment(this.servicePaymentData).subscribe({
      next: () => {
        this.message = 'Plata către furnizor a fost realizată cu succes.';

        this.servicePaymentData = {
          initiatedByUserId: userId,
          fromIban: '',
          providerId: 0,
          amount: 0,
          currency: 'RON',
          description: ''
        };

        this.loadAccounts();
      },
      error: (err: any) => {
        console.log('SERVICE PAYMENT ERROR:', err);
        this.message =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Eroare la plata furnizorului.';
      }
    });
  }
}