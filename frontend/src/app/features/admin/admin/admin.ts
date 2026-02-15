import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-admin',
  standalone: false,
  templateUrl: './admin.html',
  styleUrl: './admin.scss'
})
export class Admin {
  message = '';
  form = {
    userName: '',
    email: '',
    password: '',
    mfaEnabled: true,
    roles: ['IntakeOfficer']
  };

  constructor(private readonly http: HttpClient) {}

  createUser(): void {
    this.http.post<number>('https://localhost:7261/api/v1/auth/register', this.form).subscribe({
      next: (id) => {
        this.message = `User created with ID: ${id}`;
        this.form = {
          userName: '',
          email: '',
          password: '',
          mfaEnabled: true,
          roles: ['IntakeOfficer']
        };
      },
      error: () => {
        this.message = 'Failed to create user.';
      }
    });
  }
}
