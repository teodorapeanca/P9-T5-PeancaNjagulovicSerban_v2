import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TransactionService } from '../../services/transaction.service';

@Component({
  selector: 'app-repeat-transaction',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './repeat-transaction.html',
  styleUrl: './repeat-transaction.css'
})
export class RepeatTransaction {
  data = {
    transactionId: 0
  };

  message = '';

  constructor(
    private transactionService: TransactionService,
    private router: Router
  ) {}

  repeatTransaction() {
    if (!this.data.transactionId || this.data.transactionId <= 0) {
      this.message = 'Introdu ID-ul tranzacției.';
      return;
    }

    this.transactionService.repeatTransaction(this.data).subscribe({
      next: (res: any) => {
        this.message =
          res?.message || 'Tranzacția a fost repetată cu succes.';
      },
      error: (err: any) => {
        console.log('REPEAT TRANSACTION ERROR:', err);

        this.message =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Eroare la repetarea tranzacției.';
      }
    });
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}