import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuditService } from '../../services/audit.service';

@Component({
  selector: 'app-admin-audit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-audit.html',
  styleUrl: './admin-audit.css'
})
export class AdminAudit implements OnInit {
  logs: any[] = [];
  message = '';

  filter = {
    dateFrom: '',
    dateTo: '',
    action: '',
    entityType: '',
    userId: ''
  };

  constructor(
    private auditService: AuditService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadLogs();
  }

  loadLogs() {
    this.auditService.getAuditLogs().subscribe({
      next: (res: any) => {
        this.logs = Array.isArray(res) ? res : [];
        this.message = '';
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('AUDIT ERROR:', err);
        this.message = 'Eroare la încărcarea jurnalelor audit.';
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters() {
    const data = {
      dateFrom: this.filter.dateFrom || null,
      dateTo: this.filter.dateTo || null,
      action: this.filter.action || null,
      entityType: this.filter.entityType || null,
      userId: this.filter.userId ? Number(this.filter.userId) : null
    };

    this.auditService.filterAuditLogs(data).subscribe({
      next: (res: any) => {
        this.logs = Array.isArray(res) ? res : [];
        this.message = '';
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('AUDIT FILTER ERROR:', err);
        this.message = 'Eroare la filtrarea jurnalelor audit.';
        this.cdr.detectChanges();
      }
    });
  }

  resetFilters() {
    this.filter = {
      dateFrom: '',
      dateTo: '',
      action: '',
      entityType: '',
      userId: ''
    };

    this.loadLogs();
  }

  exportCsv() {
    this.auditService.exportAuditLogs().subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');

        link.href = url;
        link.download = 'audit_logs.csv';
        link.click();

        window.URL.revokeObjectURL(url);
      },
      error: (err: any) => {
        console.log('AUDIT EXPORT ERROR:', err);
        this.message = 'Eroare la exportul jurnalelor audit.';
      }
    });
  }

  verifyHash(log: any) {
    this.auditService.verifyHash(log.auditId).subscribe({
      next: (res: any) => {
        log.hashStatus = res?.isValid === true ? 'VALID' : 'INVALID';
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('VERIFY HASH ERROR:', err);
        log.hashStatus = 'EROARE';
        this.cdr.detectChanges();
      }
    });
  }

  goBack() {
    this.router.navigate(['/admin-access']);
  }
}