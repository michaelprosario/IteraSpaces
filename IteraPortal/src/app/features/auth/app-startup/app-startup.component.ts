import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-startup',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="startup-container">
      <div class="loading-content">
        <div class="spinner"></div>
        <h2>Loading IteraSpaces...</h2>
        <p>Please wait while we set up your workspace</p>
      </div>
    </div>
  `,
  styles: [`
    .startup-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    }

    .loading-content {
      text-align: center;
      color: white;
      padding: 2rem;
    }

    .spinner {
      width: 60px;
      height: 60px;
      margin: 0 auto 2rem;
      border: 4px solid rgba(255, 255, 255, 0.3);
      border-top: 4px solid white;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    h2 {
      font-size: 2rem;
      margin-bottom: 1rem;
      font-weight: 600;
    }

    p {
      font-size: 1.1rem;
      opacity: 0.9;
    }
  `]
})
export class AppStartupComponent {
  // This component just shows a loading state
  // The actual routing logic is handled by the authStartupGuard
}
