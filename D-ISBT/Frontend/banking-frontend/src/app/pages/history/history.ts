import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { TransactionService } from '../../services/transaction.service';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './history.html',
  styleUrl: './history.css'
})
export class History implements OnInit {
  transactions: any[] = [];
  filteredTransactions: any[] = [];

  fromIbans: string[] = [];
  toIbans: string[] = [];

  loading = false;
  message = '';

  filter = {
    fromIban: '',
    toIban: '',
    dateFrom: '',
    dateTo: '',
    type: '',
    minAmount: '',
    maxAmount: ''
  };

  constructor(
    private transactionService: TransactionService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadHistory();
  }

  loadHistory() {
    if (this.loading) {
      return;
    }

    this.loading = true;
    this.message = 'Se încarcă istoricul...';

    this.transactions = [];
    this.filteredTransactions = [];
    this.fromIbans = [];
    this.toIbans = [];

    this.cdr.detectChanges();

    this.transactionService.getMyHistory().subscribe({
      next: (res: any) => {
        const baseTransactions: any[] = Array.isArray(res)
          ? res
          : res?.data && Array.isArray(res.data)
            ? res.data
            : res?.transactions && Array.isArray(res.transactions)
              ? res.transactions
              : [];

        if (baseTransactions.length === 0) {
          this.transactions = [];
          this.filteredTransactions = [];
          this.message = 'Nu există tranzacții.';
          this.loading = false;
          this.cdr.detectChanges();
          return;
        }

        const detailRequests = baseTransactions.map((t: any) =>
          this.transactionService.getTransactionDetails(t.transactionId).pipe(
            catchError(() => of(t))
          )
        );

        forkJoin(detailRequests)
          .pipe(
            finalize(() => {
              this.loading = false;
              this.cdr.detectChanges();
            })
          )
          .subscribe({
            next: (details: any) => {
              const result: any[] = Array.isArray(details) ? details : [];

              this.transactions = result;
              this.filteredTransactions = [...result];

              this.fromIbans = [
                ...new Set(result.map((t: any) => t.fromAccountIban).filter((x: any) => x))
              ];

              this.toIbans = [
                ...new Set(result.map((t: any) => t.toAccountIban).filter((x: any) => x))
              ];

              this.message = '';
              this.cdr.detectChanges();
            },
            error: (err: any) => {
              console.log('DETAILS ERROR:', err);

              this.transactions = baseTransactions;
              this.filteredTransactions = [...baseTransactions];
              this.message = '';

              this.cdr.detectChanges();
            }
          });
      },
      error: (err: any) => {
        console.log('HISTORY ERROR:', err);

        this.transactions = [];
        this.filteredTransactions = [];
        this.message = 'Eroare la încărcarea istoricului.';
        this.loading = false;

        this.cdr.detectChanges();
      }
    });
  }

  applyFilters() {
    this.filteredTransactions = this.transactions.filter((t: any) => {
      const transactionDate = new Date(t.createdAt);

      const matchesFromIban =
        !this.filter.fromIban || t.fromAccountIban === this.filter.fromIban;

      const matchesToIban =
        !this.filter.toIban || t.toAccountIban === this.filter.toIban;

      const matchesDateFrom =
        !this.filter.dateFrom || transactionDate >= new Date(this.filter.dateFrom);

      const matchesDateTo =
        !this.filter.dateTo || transactionDate <= new Date(this.filter.dateTo);

      const matchesType =
        !this.filter.type ||
        t.type?.toLowerCase().includes(this.filter.type.toLowerCase());

      const matchesMinAmount =
        !this.filter.minAmount || Number(t.amount) >= Number(this.filter.minAmount);

      const matchesMaxAmount =
        !this.filter.maxAmount || Number(t.amount) <= Number(this.filter.maxAmount);

      return (
        matchesFromIban &&
        matchesToIban &&
        matchesDateFrom &&
        matchesDateTo &&
        matchesType &&
        matchesMinAmount &&
        matchesMaxAmount
      );
    });

    this.cdr.detectChanges();
  }

  resetFilters() {
    this.filter = {
      fromIban: '',
      toIban: '',
      dateFrom: '',
      dateTo: '',
      type: '',
      minAmount: '',
      maxAmount: ''
    };

    this.filteredTransactions = [...this.transactions];
    this.cdr.detectChanges();
  }

  exportExcel() {
    this.transactionService.exportMyHistory().subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');

        link.href = url;
        link.download = 'istoric_tranzactii.csv';
        link.click();

        window.URL.revokeObjectURL(url);
      },
      error: (err: any) => {
        console.log('EXPORT ERROR:', err);
      }
    });
  }
}