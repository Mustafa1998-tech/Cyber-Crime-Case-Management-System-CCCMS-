import { Component } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  userName = '';
  password = '';
  deviceInfo = 'Web Client';
  otpCode = '';
  mfaRequired = false;
  loading = false;
  error = '';

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  submit(): void {
    if (this.mfaRequired && !/^\d{6}$/.test(this.otpCode.trim())) {
      this.error = 'Enter the latest 6-digit OTP code.';
      return;
    }

    this.loading = true;
    this.error = '';

    if (this.mfaRequired) {
      this.authService.verifyMfa(this.userName, this.otpCode).subscribe({
        next: () => this.finishLogin(),
        error: (error: unknown) => this.handleError(error)
      });
      return;
    }

    this.authService.login(this.userName, this.password, this.deviceInfo).subscribe({
      next: (response) => {
        this.loading = false;
        this.mfaRequired = response.mfaRequired;
        if (!response.mfaRequired) {
          this.finishLogin();
        }
      },
      error: (error: unknown) => this.handleError(error)
    });
  }

  private finishLogin(): void {
    this.loading = false;
    void this.router.navigateByUrl('/dashboard');
  }

  private handleError(error: unknown): void {
    this.loading = false;

    if (error instanceof HttpErrorResponse && error.error?.detail) {
      this.error = error.error.detail as string;
      return;
    }

    this.error = 'Authentication failed.';
  }
}
