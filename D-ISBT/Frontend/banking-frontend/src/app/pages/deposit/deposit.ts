import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TransactionService } from '../../services/transaction.service';

@Component({
  selector: 'app-deposit',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './deposit.html',
  styleUrl: './deposit.css'
})
export class Deposit {

  data = {
    initiatedByUserId: Number(localStorage.getItem('userId')),
    accountId: 0,
    amount: 0,
    currency: 'RON',
    description: ''
  };

  message = '';

  constructor(private transactionService: TransactionService) {}

  submit() {
    this.transactionService.deposit(this.data).subscribe({
      next: () => {
        this.message = 'Depunere realizată cu succes!';
      },
      error: (err) => {
        console.log(err);
        this.message = 'Eroare la depunere.';
      }
    });
  }
}