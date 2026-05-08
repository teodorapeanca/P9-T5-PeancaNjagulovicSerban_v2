import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin.html',
  styleUrl: './admin.css'
})
export class Admin {

  constructor(private router: Router) {}

  logout() {
  localStorage.clear();
  this.router.navigate(['/']);
  }
}