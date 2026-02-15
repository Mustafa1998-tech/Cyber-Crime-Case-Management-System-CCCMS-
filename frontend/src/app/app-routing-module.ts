import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { Login } from './features/auth/login/login';
import { Dashboard } from './features/dashboard/dashboard/dashboard';
import { Complaints } from './features/complaints/complaints/complaints';
import { Cases } from './features/cases/cases/cases';
import { Evidence } from './features/evidence/evidence/evidence';
import { Reports } from './features/reports/reports/reports';
import { Admin } from './features/admin/admin/admin';
import { AuthGuard } from './core/guards/auth.guard';
import { RoleGuard } from './core/guards/role.guard';

const routes: Routes = [
  { path: 'login', component: Login },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'dashboard', component: Dashboard, canActivate: [AuthGuard] },
  { path: 'complaints', component: Complaints, canActivate: [AuthGuard] },
  { path: 'cases', component: Cases, canActivate: [AuthGuard] },
  { path: 'evidence', component: Evidence, canActivate: [AuthGuard] },
  { path: 'reports', component: Reports, canActivate: [AuthGuard] },
  {
    path: 'admin',
    component: Admin,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['SuperAdmin', 'SystemAdmin'] }
  },
  { path: '**', redirectTo: 'dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
