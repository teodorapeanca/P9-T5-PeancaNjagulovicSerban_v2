import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AccountService } from '../../services/account.service';
import { TransactionService } from '../../services/transaction.service';
import { TransferByIbanRequest } from '../../models/transfer-by-iban.model';

@Component({
  selector: 'app-own-transfer',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './own-transfer.html',
  styleUrl: './own-transfer.css'
})
export class OwnTransfer implements OnInit {
  accounts: any[] = [];
  message = '';

  ownTransfer: TransferByIbanRequest = {
    initiatedByUserId: 0,
    fromIban: '',
    toIban: '',
    amount: 0,
    currency: 'RON',
    description: ''
  };

  constructor(
    private accountService: AccountService,
    private transactionService: TransactionService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadAccounts();
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
        console.log('OWN TRANSFER ACCOUNTS ERROR:', err);
        this.message = 'Eroare la încărcarea conturilor.';
        this.cdr.detectChanges();
      }
    });
  }

  transferOwnAccounts() {
    const userId = Number(localStorage.getItem('userId'));
    this.ownTransfer.initiatedByUserId = userId;

    if (!this.ownTransfer.fromIban || !this.ownTransfer.toIban) {
      this.message = 'Selectează contul sursă și contul destinație.';
      return;
    }

    if (this.ownTransfer.fromIban === this.ownTransfer.toIban) {
      this.message = 'Contul sursă și contul destinație trebuie să fie diferite.';
      return;
    }

    if (this.ownTransfer.amount <= 0) {
      this.message = 'Suma trebuie să fie mai mare decât 0.';
      return;
    }

    this.transactionService.transferByIban(this.ownTransfer).subscribe({
      next: () => {
        this.message = 'Transfer între conturile proprii realizat cu succes.';

        this.ownTransfer = {
          initiatedByUserId: userId,
          fromIban: '',
          toIban: '',
          amount: 0,
          currency: 'RON',
          description: ''
        };

        this.loadAccounts();
      },
      error: (err: any) => {
        console.log('OWN TRANSFER ERROR:', err);
        this.message =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Eroare la transferul între conturile proprii.';
      }
    });
  }
}