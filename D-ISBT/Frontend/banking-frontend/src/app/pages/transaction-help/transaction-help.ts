import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TransactionService } from '../../services/transaction.service';

@Component({
  selector: 'app-transaction-help',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './transaction-help.html',
  styleUrl: './transaction-help.css'
})
export class TransactionHelp implements OnInit {
  helpData: any = null;
  message = '';

  constructor(
    private transactionService: TransactionService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadHelp();
  }

  loadHelp() {
    this.transactionService.getTransactionHelp().subscribe({
      next: (res: any) => {
        this.helpData = res;
        this.message = '';
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('TRANSACTION HELP ERROR:', err);
        this.message = 'Eroare la încărcarea informațiilor Help.';
        this.cdr.detectChanges();
      }
    });
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}