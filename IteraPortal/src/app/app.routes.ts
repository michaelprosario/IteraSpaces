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
        path: 'edit/:id',
        loadComponent: () => import('./edit-user/edit-user')
          .then(m => m.EditUser)
      },
      {
        path: ':id',
        loadComponent: () => import('./features/profile/profile.component')
          .then(m => m.ProfileComponent)
      },

    ]
  },
  {
    path: 'lean-sessions',
    canActivate: [authGuard],
    children: [
      {
        path: 'list',
        loadComponent: () => import('./lean-sessions/list-lean-sessions')
          .then(m => m.ListLeanSessions)
      },
      {
        path: 'add',
        loadComponent: () => import('./lean-sessions/add-lean-session')
          .then(m => m.AddLeanSession)
      },
      {
        path: 'edit/:id',
        loadComponent: () => import('./lean-sessions/edit-lean-session')
          .then(m => m.EditLeanSession)
      },
      {
        path: 'view/:id',
        loadComponent: () => import('./lean-sessions/view-lean-session')
          .then(m => m.ViewLeanSession)
      },
      {
        path: 'close/:id',
        loadComponent: () => import('./lean-sessions/close-lean-session')
          .then(m => m.CloseLeanSession)
      }
    ]
  },
  {
    path: '**',
    redirectTo: '/login'
  }
];
