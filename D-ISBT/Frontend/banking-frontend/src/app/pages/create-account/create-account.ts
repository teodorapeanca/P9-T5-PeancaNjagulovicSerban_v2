import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AccountService } from '../../services/account.service';
import { CreateAccountRequest } from '../../models/create-account-request.model';

@Component({
  selector: 'app-create-account',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './create-account.html',
  styleUrl: './create-account.css'
})
export class CreateAccount {
  message = '';

  newAccount: CreateAccountRequest = {
    currency: 'RON',
    balance: 0,
    dailyLimit: 1000
  };

  constructor(private accountService: AccountService) {}

  createAccount() {
    if (!this.newAccount.currency) {
      this.message = 'Selectează moneda contului.';
      return;
    }

    if (this.newAccount.dailyLimit <= 0) {
      this.message = 'Limita zilnică trebuie să fie mai mare decât 0.';
      return;
    }

    this.accountService.createAccount(this.newAccount).subscribe({
      next: () => {
        this.message = 'Cererea de creare cont a fost trimisă. Așteaptă aprobarea administratorului.';

        this.newAccount = {
          currency: 'RON',
          balance: 0,
          dailyLimit: 1000
        };
      },
      error: (err: any) => {
        console.log('CREATE ACCOUNT ERROR:', err);
        this.message =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Eroare la crearea contului.';
      }
    });
  }
}