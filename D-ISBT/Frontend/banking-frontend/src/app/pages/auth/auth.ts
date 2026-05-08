import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { SessionTimeoutService } from '../../services/session-timeout.service';

@Component({
  selector: 'app-auth',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './auth.html',
  styleUrl: './auth.css'
})
export class Auth {
  activeTab: 'login' | 'register' = 'login';

  clientIdentifier = '';
  password = '';

  show2FA = false;
  code = '';
  twoFactorCode = '';
  roles: string[] = [];

  message = '';

  registerForm = {
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    address: '',
    dateOfBirth: '',
    acceptedTerms: false,
    password: ''
  };

  confirmPassword = '';

  showForgot = false;
  forgotEmail = '';
  showReset = false;

  resetForm = {
    clientIdentifier: '',
    token: '',
    newPassword: '',
    confirmNewPassword: ''
  };

  constructor(
    private authService: AuthService,
    private cdr: ChangeDetectorRef,
    private router: Router,
    private sessionTimeoutService: SessionTimeoutService
  ) {}

  setTab(tab: 'login' | 'register') {
    this.activeTab = tab;
    this.message = '';
    this.showForgot = false;
    this.showReset = false;
    this.show2FA = false;
  }

  login() {
    this.message = '';

    this.authService.login({
      clientIdentifier: this.clientIdentifier,
      password: this.password
    }).subscribe({
      next: (res: any) => {
        console.log('LOGIN RESPONSE:', res);

        this.message = res.message || 'Introdu codul 2FA.';
        this.show2FA = true;

        if (res.twoFactorCode) {
          this.twoFactorCode = res.twoFactorCode;
        }

        if (res.roles && Array.isArray(res.roles)) {
          this.roles = res.roles;
          localStorage.setItem('roles', JSON.stringify(res.roles));
        }

        if (res.clientIdentifier) {
          localStorage.setItem('clientIdentifier', res.clientIdentifier);
        } else if (this.clientIdentifier) {
          localStorage.setItem('clientIdentifier', this.clientIdentifier);
        }

        if (res.sessionId) {
          localStorage.setItem('sessionId', res.sessionId);
        }

        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('LOGIN ERROR:', err);

        this.message =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Date de autentificare incorecte.';

        this.cdr.detectChanges();
      }
    });
  }

  verify2FA() {
    this.message = '';

    this.authService.verify2FA({
      clientIdentifier: this.clientIdentifier,
      code: this.code
    }).subscribe({
      next: (res: any) => {
        console.log('2FA RESPONSE:', res);

        if (res.token) {
          localStorage.setItem('token', res.token);

          const decodedUserId = this.getUserIdFromToken(res.token);
          if (decodedUserId) {
            localStorage.setItem('userId', decodedUserId.toString());
          }
        }

        if (res.userId) {
          localStorage.setItem('userId', res.userId.toString());
        } else if (res.id) {
          localStorage.setItem('userId', res.id.toString());
        }

        if (res.clientIdentifier) {
          localStorage.setItem('clientIdentifier', res.clientIdentifier);
        } else if (this.clientIdentifier) {
          localStorage.setItem('clientIdentifier', this.clientIdentifier);
        }

        if (res.sessionId) {
          localStorage.setItem('sessionId', res.sessionId);
        }

        if (res.roles && Array.isArray(res.roles)) {
          this.roles = res.roles;
          localStorage.setItem('roles', JSON.stringify(res.roles));
        }

        this.sessionTimeoutService.startWatching();

        this.message = 'Autentificare reușită!';
        this.show2FA = false;
        this.code = '';
        this.twoFactorCode = '';

        this.cdr.detectChanges();

        const savedRoles = JSON.parse(localStorage.getItem('roles') || '[]');

        if (savedRoles.includes('Administrator')) {
          this.router.navigate(['/admin-access']);
        } else {
          this.router.navigate(['/dashboard']);
        }
      },
      error: (err: any) => {
        console.log('2FA ERROR:', err);
        this.message = 'Cod 2FA invalid.';
        this.cdr.detectChanges();
      }
    });
  }

  private getUserIdFromToken(token: string): number | null {
    try {
      const payloadBase64 = token.split('.')[1];

      if (!payloadBase64) {
        return null;
      }

      const payloadJson = atob(payloadBase64);
      const payload = JSON.parse(payloadJson);

      const possibleUserId =
        payload.userId ||
        payload.UserId ||
        payload.nameid ||
        payload.sub ||
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];

      const userId = Number(possibleUserId);

      return userId > 0 ? userId : null;
    } catch (error) {
      console.log('JWT DECODE ERROR:', error);
      return null;
    }
  }

  register() {
    this.message = '';

    if (!this.registerForm.acceptedTerms) {
      this.message = 'Trebuie să accepți termenii și condițiile.';
      return;
    }

    if (this.registerForm.password !== this.confirmPassword) {
      this.message = 'Parolele nu coincid.';
      return;
    }

    this.authService.register(this.registerForm).subscribe({
      next: (res: any) => {
        console.log('REGISTER RESPONSE:', res);

        this.message = 'Cont creat! Identificatorul tău este: ' + res.clientIdentifier;

        this.clientIdentifier = res.clientIdentifier;
        this.activeTab = 'login';

        this.registerForm = {
          firstName: '',
          lastName: '',
          email: '',
          phone: '',
          address: '',
          dateOfBirth: '',
          acceptedTerms: false,
          password: ''
        };

        this.confirmPassword = '';

        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('REGISTER ERROR:', err);

        this.message =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Eroare la înregistrare.';

        this.cdr.detectChanges();
      }
    });
  }

  forgotPassword() {
    this.message = '';

    if (!this.forgotEmail) {
      this.message = 'Introdu adresa de email.';
      return;
    }

    this.authService.forgotPassword({ email: this.forgotEmail }).subscribe({
      next: (res: any) => {
        console.log('FORGOT RESPONSE:', res);

        this.message =
          typeof res === 'string'
            ? res
            : res?.message || 'Token-ul de resetare a fost generat.';

        this.showReset = true;

        if (res?.clientIdentifier) {
          this.resetForm.clientIdentifier = res.clientIdentifier;
        }

        if (res?.resetToken) {
          this.resetForm.token = res.resetToken;
        }

        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('FORGOT ERROR:', err);

        this.message =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Eroare la resetarea parolei.';

        this.cdr.detectChanges();
      }
    });
  }

  resetPassword() {
    this.message = '';

    if (this.resetForm.newPassword !== this.resetForm.confirmNewPassword) {
      this.message = 'Parolele nu coincid.';
      return;
    }

    this.authService.resetPassword(this.resetForm).subscribe({
      next: (res: any) => {
        console.log('RESET RESPONSE:', res);

        this.message =
          typeof res === 'string'
            ? res
            : res?.message || 'Parola a fost resetată cu succes.';

        this.showForgot = false;
        this.showReset = false;
        this.activeTab = 'login';

        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('RESET ERROR:', err);

        this.message =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Eroare la resetarea parolei.';

        this.cdr.detectChanges();
      }
    });
  }
}