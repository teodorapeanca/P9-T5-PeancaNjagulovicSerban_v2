import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AccountService } from '../../services/account.service';

@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './accounts.html',
  styleUrl: './accounts.css'
})
export class Accounts implements OnInit {
  accounts: any[] = [];
  message = '';

  constructor(
    private accountService: AccountService,
    private cdr: ChangeDetectorRef,
    private location: Location
  ) {}

  ngOnInit() {
    this.loadAccounts();
  }

  goBack() {
    this.location.back();
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

        this.message = '';
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('ACCOUNTS ERROR:', err);
        this.message = 'Eroare la încărcarea conturilor.';
        this.cdr.detectChanges();
      }
    });
  }
}