import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-users.html',
  styleUrl: './admin-users.css'
})
export class AdminUsers implements OnInit {
  users: any[] = [];
  message = '';

  constructor(private userService: UserService) {}

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.message = '';

    this.userService.getUsers().subscribe({
      next: (res: any) => {
        console.log('USERS RESPONSE:', res);

        if (Array.isArray(res)) {
          this.users = res;
        } else if (res?.data && Array.isArray(res.data)) {
          this.users = res.data;
        } else if (res?.users && Array.isArray(res.users)) {
          this.users = res.users;
        } else {
          this.users = [];
        }

        if (this.users.length === 0) {
          this.message = 'Nu există utilizatori.';
        }
      },
      error: (err: any) => {
        console.log('USERS ERROR:', err);

        this.users = [];

        this.message =
          typeof err.error === 'string'
            ? err.error
            : err.error?.message || 'Eroare la încărcarea utilizatorilor.';
      }
    });
  }
}