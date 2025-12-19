import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { authStartupGuard } from './core/guards/auth-startup.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/startup',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component')
      .then(m => m.LoginComponent)
  },
  {
    path: 'startup',
    loadComponent: () => import('./features/auth/app-startup/app-startup.component')
      .then(m => m.AppStartupComponent),
    canActivate: [authStartupGuard]
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/user-registration/user-registration.component')
      .then(m => m.UserRegistrationComponent)
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard.component')
      .then(m => m.DashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/profile/profile.component')
          .then(m => m.ProfileComponent)
      },
      {
        path: 'edit',
        loadComponent: () => import('./features/profile/edit-profile/edit-profile.component')
          .then(m => m.EditProfileComponent)
      },
      {
        path: 'privacy',
        loadComponent: () => import('./features/profile/privacy-settings/privacy-settings.component')
          .then(m => m.PrivacySettingsComponent)
      }
    ]
  },
  {
    path: 'users',
    canActivate: [authGuard],
    children: [
      {
        path: 'list',
        loadComponent: () => import('./list-users/list-users')
          .then(m => m.ListUsers)
      },
      {
        path: ':id',
        loadComponent: () => import('./features/profile/profile.component')
          .then(m => m.ProfileComponent)
      },

    ]
  },
  {
    path: '**',
    redirectTo: '/login'
  }
];
