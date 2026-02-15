import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { AuthResponse, UserSession } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiBase = 'https://localhost:7261/api/v1/auth';
  private readonly sessionKey = 'nciems_session';
  private readonly sessionSubject = new BehaviorSubject<UserSession | null>(this.readSession());

  readonly session$ = this.sessionSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  get currentSession(): UserSession | null {
    return this.sessionSubject.value;
  }

  get isAuthenticated(): boolean {
    return !!this.currentSession?.accessToken;
  }

  getAccessToken(): string | null {
    return this.currentSession?.accessToken ?? null;
  }

  getRefreshToken(): string | null {
    return this.currentSession?.refreshToken ?? null;
  }

  hasRole(role: string): boolean {
    return this.currentSession?.roles.includes(role) ?? false;
  }

  login(userName: string, password: string, deviceInfo: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiBase}/login`, { userName, password, deviceInfo })
      .pipe(tap((response) => this.persistFromAuthResponse(response)));
  }

  verifyMfa(userName: string, otpCode: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiBase}/verify-mfa`, { userName, otpCode })
      .pipe(tap((response) => this.persistFromAuthResponse(response)));
  }

  refreshToken(): Observable<AuthResponse> {
    const token = this.getRefreshToken();
    if (!token) {
      throw new Error('No refresh token available.');
    }

    return this.http
      .post<AuthResponse>(`${this.apiBase}/refresh`, { refreshToken: token })
      .pipe(tap((response) => this.persistFromAuthResponse(response)));
  }

  logout(): void {
    localStorage.removeItem(this.sessionKey);
    this.sessionSubject.next(null);
  }

  private persistFromAuthResponse(response: AuthResponse): void {
    if (response.mfaRequired || !response.accessToken || !response.refreshToken) {
      return;
    }

    const session: UserSession = {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      userName: response.userName,
      roles: response.roles,
      accessTokenExpiresAtUtc: response.accessTokenExpiresAtUtc
    };

    localStorage.setItem(this.sessionKey, JSON.stringify(session));
    this.sessionSubject.next(session);
  }

  private readSession(): UserSession | null {
    const raw = localStorage.getItem(this.sessionKey);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as UserSession;
    } catch {
      return null;
    }
  }
}
