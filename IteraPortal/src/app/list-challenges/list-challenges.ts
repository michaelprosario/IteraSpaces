import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ChallengesService, Challenge, ChallengeStatus } from '../core/services/challenges.service';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-list-challenges',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './list-challenges.html',
  styleUrl: './list-challenges.scss',
})
export class ListChallenges implements OnInit {
  private challengesService = inject(ChallengesService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  challenges: Challenge[] = [];
  loading = false;
  error = '';
  ChallengeStatus = ChallengeStatus;

  currentUser = this.authService.currentUser;

  async ngOnInit(): Promise<void> {
    await this.loadChallenges();
  }

  async loadChallenges(): Promise<void> {
    try {
      this.loading = true;
      this.error = '';
      this.cdr.detectChanges();
      
      const userId = this.currentUser()?.id || '';
      this.challenges = await this.challengesService.list({
        userId,
        pageNumber: 1,
        pageSize: 100
      });
      this.cdr.detectChanges();
    } catch (error: any) {
      this.error = error.message || 'Failed to load challenges';
      console.error('Error loading challenges:', error);
      this.cdr.detectChanges();
    } finally {
      this.loading = false;
      this.cdr.detectChanges();
    }
  }

  navigateToAddChallenge(): void {
    this.router.navigate(['/challenges/add']);
  }

  navigateToEditChallenge(challengeId: string): void {
    this.router.navigate(['/challenges/edit', challengeId]);
  }

  navigateToViewChallenge(challengeId: string): void {
    this.router.navigate(['/challenges/view', challengeId]);
  }

  getStatusLabel(status?: ChallengeStatus): string {
    switch (status) {
      case ChallengeStatus.Draft:
        return 'Draft';
      case ChallengeStatus.Open:
        return 'Open';
      case ChallengeStatus.Closed:
        return 'Closed';
      case ChallengeStatus.Archived:
        return 'Archived';
      default:
        return 'Unknown';
    }
  }
}
