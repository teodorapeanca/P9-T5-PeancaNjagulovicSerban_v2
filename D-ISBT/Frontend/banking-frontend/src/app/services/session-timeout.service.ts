import { Injectable, NgZone } from '@angular/core';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class SessionTimeoutService {
  private timeoutId: any;
  private readonly timeoutMinutes = 10;
  private readonly timeoutMs = this.timeoutMinutes * 60 * 1000;

  private activityEvents = [
    'mousemove',
    'mousedown',
    'click',
    'scroll',
    'keypress',
    'keydown',
    'touchstart'
  ];

  constructor(
    private router: Router,
    private ngZone: NgZone
  ) {}

  startWatching() {
    this.stopWatching();

    if (!localStorage.getItem('token')) {
      return;
    }

    this.activityEvents.forEach(event => {
      window.addEventListener(event, this.resetTimer);
    });

    this.resetTimer();
  }

  stopWatching() {
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
    }

    this.activityEvents.forEach(event => {
      window.removeEventListener(event, this.resetTimer);
    });
  }

  private resetTimer = () => {
    if (!localStorage.getItem('token')) {
      return;
    }

    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
    }

    this.ngZone.runOutsideAngular(() => {
      this.timeoutId = setTimeout(() => {
        this.ngZone.run(() => {
          this.logoutByInactivity();
        });
      }, this.timeoutMs);
    });
  };

  private logoutByInactivity() {
    localStorage.clear();

    alert('Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.');

    this.router.navigate(['/']);
  }
}