import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree {
    const requiredRoles = (route.data['roles'] as string[] | undefined) ?? [];
    if (requiredRoles.length === 0) {
      return true;
    }

    const hasRole = requiredRoles.some((role) => this.authService.hasRole(role));
    if (hasRole) {
      return true;
    }

    return this.router.parseUrl('/dashboard');
  }
}
