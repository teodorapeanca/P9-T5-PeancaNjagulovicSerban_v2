import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-my-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './my-profile.html',
  styleUrl: './my-profile.css'
})
export class MyProfile implements OnInit {
  user: any = null;
  message = '';
  editMode = false;

  constructor(
    private userService: UserService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadUser();
  }

  loadUser() {
    const userId = Number(localStorage.getItem('userId'));

    console.log('MY PROFILE USER ID:', userId);

    if (!userId) {
      this.message = 'Nu există userId în sesiune. Te rog autentifică-te din nou.';
      this.cdr.detectChanges();
      return;
    }

    this.userService.getUserById(userId).subscribe({
      next: (res: any) => {
        console.log('MY PROFILE RESPONSE:', res);

        this.user = res;
        this.message = '';

        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.log('MY PROFILE ERROR:', err);

        this.user = null;
        this.message =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Eroare la încărcarea datelor personale.';

        this.cdr.detectChanges();
      }
    });
  }

  enableEdit() {
    this.editMode = true;
  }

  cancelEdit() {
    this.editMode = false;
    this.loadUser();
  }

  save() {
    if (!this.user) {
      this.message = 'Nu există date de salvat.';
      return;
    }

    const data = {
      firstName: this.user.firstName,
      lastName: this.user.lastName,
      phone: this.user.phone,
      address: this.user.address
    };

    this.userService.updateUser(this.user.userId, data).subscribe({
      next: () => {
        this.message = 'Datele personale au fost actualizate cu succes.';
        this.editMode = false;
        this.loadUser();
      },
      error: (err: any) => {
        console.log('UPDATE PROFILE ERROR:', err);
        this.message =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Eroare la actualizarea datelor personale.';
        this.cdr.detectChanges();
      }
    });
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}