import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SessionTimeoutService } from './services/session-timeout.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {

  constructor(private sessionTimeoutService: SessionTimeoutService) {}

  ngOnInit() {
    this.sessionTimeoutService.startWatching();
  }
}