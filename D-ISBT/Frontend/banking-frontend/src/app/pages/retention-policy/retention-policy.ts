import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TransactionService } from '../../services/transaction.service';

@Component({
  selector: 'app-retention-policy',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './retention-policy.html',
  styleUrl: './retention-policy.css'
})
export class RetentionPolicy implements OnInit {
  policy: any = null;
  message = '';

  days = 30;
  isAdminMode = false;
  editMode = false;

  constructor(
    private transactionService: TransactionService,
    private route: ActivatedRoute,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.isAdminMode = this.router.url.startsWith('/admin');

    this.route.queryParams.subscribe(params => {
      this.editMode = params['mode'] === 'edit';
    });

    this.loadPolicy();
  }

  loadPolicy() {
    this.transactionService.getRetentionPolicy().subscribe({
      next: (res: any) => {
        this.policy = res;
        this.days = res?.currentRetentionDays || res?.minimumRetentionDays || 30;
        this.message = '';
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('RETENTION POLICY ERROR:', err);
        this.message = 'Eroare la încărcarea politicii de retenție.';
        this.cdr.detectChanges();
      }
    });
  }
setViewMode() {
  this.editMode = false;
}

setEditMode() {
  this.editMode = true;
}

  savePolicy() {
    if (!this.days || this.days <= 0) {
      this.message = 'Introduceți un număr valid de zile.';
      return;
    }

    this.transactionService.updateRetentionPolicy(this.days).subscribe({
      next: (res: any) => {
        this.message =
          res?.message || 'Politica de retenție a fost actualizată cu succes.';
        this.loadPolicy();
      },
      error: (err: any) => {
        console.log('UPDATE RETENTION POLICY ERROR:', err);
        this.message =
          err?.error?.message || 'Eroare la actualizarea politicii de retenție.';
      }
    });
  }

  goBack() {
    if (this.isAdminMode) {
      this.router.navigate(['/admin-access']);
    } else {
      this.router.navigate(['/dashboard']);
    }
  }
}