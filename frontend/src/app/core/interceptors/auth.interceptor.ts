import { Injectable } from '@angular/core';
import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Observable, catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private readonly authService: AuthService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const accessToken = this.authService.getAccessToken();
    const authRequest = accessToken ? this.attachToken(req, accessToken) : req;

    return next.handle(authRequest).pipe(
      catchError((error: unknown) => {
        if (!(error instanceof HttpErrorResponse) || error.status !== 401 || req.url.includes('/api/v1/auth/')) {
          return throwError(() => error);
        }

        const refreshToken = this.authService.getRefreshToken();
        if (!refreshToken) {
          this.authService.logout();
          return throwError(() => error);
        }

        return this.authService.refreshToken().pipe(
          switchMap((response) => {
            if (!response.accessToken) {
              this.authService.logout();
              return throwError(() => error);
            }

            return next.handle(this.attachToken(req, response.accessToken));
          }),
          catchError((refreshError) => {
            this.authService.logout();
            return throwError(() => refreshError);
          })
        );
      })
    );
  }

  private attachToken(request: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
    return request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }
}
