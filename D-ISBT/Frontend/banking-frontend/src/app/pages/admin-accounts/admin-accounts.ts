import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AccountService } from '../../services/account.service';

@Component({
  selector: 'app-admin-accounts',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './admin-accounts.html',
  styleUrl: './admin-accounts.css'
})
export class AdminAccounts implements OnInit {
  accounts: any[] = [];
  filteredAccounts: any[] = [];
  message = '';

  filter = {
    accountId: '',
    accountUid: '',
    userId: '',
    iban: '',
    currency: '',
    balance: '',
    status: '',
    dailyLimit: ''
  };

  constructor(private accountService: AccountService) {}

  ngOnInit() {
    this.loadAccounts();
  }

  loadAccounts() {
    this.message = '';

    this.accountService.getAllAccounts().subscribe({
      next: (res: any) => {
        console.log('ALL ACCOUNTS RESPONSE:', res);

        if (Array.isArray(res)) {
          this.accounts = res;
        } else if (res?.data && Array.isArray(res.data)) {
          this.accounts = res.data;
        } else if (res?.accounts && Array.isArray(res.accounts)) {
          this.accounts = res.accounts;
        } else if (res?.items && Array.isArray(res.items)) {
          this.accounts = res.items;
        } else {
          this.accounts = [];
        }

        this.filteredAccounts = [...this.accounts];

        if (this.accounts.length === 0) {
          this.message = 'Nu există conturi sau nu ai drepturi de administrator.';
        }
      },
      error: (err: any) => {
        console.log('ALL ACCOUNTS ERROR:', err);
        this.accounts = [];
        this.filteredAccounts = [];
        this.message =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Eroare la încărcarea conturilor.';
      }
    });
  }

  applyFilters() {
    this.filteredAccounts = this.accounts.filter(acc => {
      return (
        this.match(acc.accountId, this.filter.accountId) &&
        this.match(acc.accountUid, this.filter.accountUid) &&
        this.match(acc.userId, this.filter.userId) &&
        this.match(acc.iban, this.filter.iban) &&
        this.match(acc.currency, this.filter.currency) &&
        this.match(acc.balance, this.filter.balance) &&
        this.match(acc.status, this.filter.status) &&
        this.match(acc.dailyLimit, this.filter.dailyLimit)
      );
    });
  }

  resetFilters() {
    this.filter = {
      accountId: '',
      accountUid: '',
      userId: '',
      iban: '',
      currency: '',
      balance: '',
      status: '',
      dailyLimit: ''
    };

    this.filteredAccounts = [...this.accounts];
  }

  approveAccount(account: any) {
    const id = account.accountId ?? account.id;

    this.accountService.approveAccount(id).subscribe({
      next: () => {
        this.message = 'Cont aprobat cu succes.';
        this.loadAccounts();
      },
      error: (err: any) => {
        console.log('APPROVE ERROR:', err);
        this.message = 'Eroare la aprobarea contului.';
      }
    });
  }

  deactivateAccount(account: any) {
    const id = account.accountId ?? account.id;

    this.accountService.deactivateAccount(id).subscribe({
      next: () => {
        this.message = 'Cont dezactivat cu succes.';
        this.loadAccounts();
      },
      error: (err: any) => {
        console.log('DEACTIVATE ERROR:', err);
        this.message = 'Eroare la dezactivarea contului.';
      }
    });
  }

  reactivateAccount(account: any) {
    const id = account.accountId ?? account.id;

    this.accountService.reactivateAccount(id).subscribe({
      next: () => {
        this.message = 'Cont reactivat cu succes.';
        this.loadAccounts();
      },
      error: (err: any) => {
        console.log('REACTIVATE ERROR:', err);
        this.message = 'Eroare la reactivarea contului.';
      }
    });
  }

  private match(value: any, filterValue: string): boolean {
    if (!filterValue) {
      return true;
    }

    return String(value ?? '')
      .toLowerCase()
      .includes(filterValue.toLowerCase());
  }
}