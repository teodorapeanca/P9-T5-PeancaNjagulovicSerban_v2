import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AccountService } from '../../services/account.service';
import { TransactionService } from '../../services/transaction.service';
import { TransferByIbanRequest } from '../../models/transfer-by-iban.model';

@Component({
  selector: 'app-external-transfer',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './external-transfer.html',
  styleUrl: './external-transfer.css'
})
export class ExternalTransfer implements OnInit {
  accounts: any[] = [];
  message = '';

  externalTransfer: TransferByIbanRequest = {
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
        console.log('EXTERNAL TRANSFER ACCOUNTS ERROR:', err);
        this.message = 'Eroare la încărcarea conturilor.';
        this.cdr.detectChanges();
      }
    });
  }

  transferToOtherAccount() {
    const userId = Number(localStorage.getItem('userId'));
    this.externalTransfer.initiatedByUserId = userId;

    if (!this.externalTransfer.fromIban || !this.externalTransfer.toIban) {
      this.message = 'Completează IBAN-ul sursă și IBAN-ul destinație.';
      return;
    }

    if (this.externalTransfer.fromIban === this.externalTransfer.toIban) {
      this.message = 'IBAN-ul sursă și IBAN-ul destinație trebuie să fie diferite.';
      return;
    }

    if (this.externalTransfer.amount <= 0) {
      this.message = 'Suma trebuie să fie mai mare decât 0.';
      return;
    }

    this.transactionService.transferByIban(this.externalTransfer).subscribe({
      next: () => {
        this.message = 'Transfer către alt cont realizat cu succes.';

        this.externalTransfer = {
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
        console.log('EXTERNAL TRANSFER ERROR:', err);
        this.message =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Eroare la transferul către alt cont.';
      }
    });
  }
}