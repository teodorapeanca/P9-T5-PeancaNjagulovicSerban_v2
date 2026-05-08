import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { RegisterRequest } from '../../models/register-request.model';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {

  form: RegisterRequest = {
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
  message = '';
  error = '';

  constructor(private authService: AuthService) {}

  register() {
    this.message = '';
    this.error = '';

    if (!this.form.acceptedTerms) {
      this.error = 'Trebuie să accepți termenii și condițiile.';
      return;
    }

    if (this.form.password !== this.confirmPassword) {
      this.error = 'Parolele nu coincid.';
      return;
    }

    this.authService.register(this.form).subscribe({
      next: () => {
        this.message = 'Cont creat cu succes! A fost generată notificarea de confirmare.';
        this.error = '';
      },
      error: (err) => {
        this.error =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Eroare la înregistrare. Verifică datele introduse.';

        this.message = '';
      }
    });
  }
}