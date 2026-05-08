import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TransactionService } from '../../services/transaction.service';

@Component({
  selector: 'app-admin-transactions',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './admin-transactions.html',
  styleUrl: './admin-transactions.css'
})
export class AdminTransactions implements OnInit {
  transactions: any[] = [];
  filteredTransactions: any[] = [];
  message = '';

  filter = {
    userId: '',
    fromIban: '',
    toIban: '',
    currency: '',
    type: '',
    amlFlag: '',
    dateFrom: '',
    dateTo: '',
    minAmount: '',
    maxAmount: ''
  };

  constructor(private transactionService: TransactionService) {}

  ngOnInit() {
    this.loadTransactions();
  }

  loadTransactions() {
    this.transactionService.getAllTransactions().subscribe({
      next: (res: any) => {
        if (Array.isArray(res)) {
          this.transactions = res;
        } else if (res?.data && Array.isArray(res.data)) {
          this.transactions = res.data;
        } else if (res?.transactions && Array.isArray(res.transactions)) {
          this.transactions = res.transactions;
        } else {
          this.transactions = [];
        }

        this.filteredTransactions = [...this.transactions];
        this.message = '';
      },
      error: (err: any) => {
        console.log('ADMIN TRANSACTIONS ERROR:', err);
        this.transactions = [];
        this.filteredTransactions = [];
        this.message =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Eroare la încărcarea tranzacțiilor.';
      }
    });
  }

  applyFilters() {
    this.filteredTransactions = this.transactions.filter(t => {
      return (
        this.match(t.initiatedByUserId, this.filter.userId) &&
        this.match(t.fromAccountIban || t.fromIban || t.fromAccountId, this.filter.fromIban) &&
        this.match(t.toAccountIban || t.toIban || t.toAccountId, this.filter.toIban) &&
        this.match(t.currency, this.filter.currency) &&
        this.match(t.type, this.filter.type) &&
        this.matchBoolean(t.amlFlag, this.filter.amlFlag) &&
        this.matchDate(t.createdAt, this.filter.dateFrom, this.filter.dateTo) &&
        this.matchMinAmount(t.amount, this.filter.minAmount) &&
        this.matchMaxAmount(t.amount, this.filter.maxAmount)
      );
    });
  }

  resetFilters() {
    this.filter = {
      userId: '',
      fromIban: '',
      toIban: '',
      currency: '',
      type: '',
      amlFlag: '',
      dateFrom: '',
      dateTo: '',
      minAmount: '',
      maxAmount: ''
    };

    this.filteredTransactions = [...this.transactions];
  }

  private match(value: any, filterValue: string): boolean {
    if (!filterValue) return true;

    return String(value ?? '')
      .toLowerCase()
      .includes(filterValue.toLowerCase());
  }

  private matchBoolean(value: boolean, filterValue: string): boolean {
    if (!filterValue) return true;

    if (filterValue === 'true') return value === true;
    if (filterValue === 'false') return value === false;

    return true;
  }

  private matchDate(value: string, from: string, to: string): boolean {
    if (!value) return true;

    const date = new Date(value);

    if (from && date < new Date(from)) return false;
    if (to && date > new Date(to)) return false;

    return true;
  }

  private matchMinAmount(value: any, filterValue: string): boolean {
    if (!filterValue) return true;
    return Number(value) >= Number(filterValue);
  }

  private matchMaxAmount(value: any, filterValue: string): boolean {
    if (!filterValue) return true;
    return Number(value) <= Number(filterValue);
  }
}