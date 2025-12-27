import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ChallengesService, Challenge } from '../core/services/challenges.service';
import { ChallengePhasesService, ChallengePhase } from '../core/services/challenge-phases.service';
import { ChallengePostsService, ChallengePost, GetChallengePostsQuery } from '../core/services/challenge-posts.service';
import { AuthService } from '../core/services/auth.service';

interface PhaseWithPosts {
  phase: ChallengePhase;
  posts: ChallengePost[];
}

@Component({
  selector: 'app-view-challenge',
  imports: [CommonModule],
  templateUrl: './view-challenge.html',
  styleUrl: './view-challenge.scss',
})
export class ViewChallenge implements OnInit {
  private challengesService = inject(ChallengesService);
  private challengePhasesService = inject(ChallengePhasesService);
  private challengePostsService = inject(ChallengePostsService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  challenge: Challenge | null = null;
  phasesWithPosts: PhaseWithPosts[] = [];
  loading = true;
  error: string | null = null;

  async ngOnInit() {
    const challengeId = this.route.snapshot.paramMap.get('id');
    if (!challengeId) {
      this.error = 'No challenge ID provided';
      this.loading = false;
      return;
    }

    await this.loadChallengeData(challengeId);
  }

  private async loadChallengeData(challengeId: string) {
    try {
      this.loading = true;
      this.error = null;

      const userId = this.authService.currentUser()?.id;
      if (!userId) {
        this.error = 'You must be logged in to view challenges';
        this.loading = false;
        return;
      }

      // Load challenge details
      const challengeResult = await this.challengesService.get({ 
        userId, 
        challengeId 
      });
      this.challenge = challengeResult.challenge;

      // Load phases
      const phasesResult = await this.challengePhasesService.list({ 
        userId, 
        challengeId 
      });
      
      // Sort phases by display order
      const phases = (phasesResult || []).sort((a: ChallengePhase, b: ChallengePhase) => 
        (a.displayOrder || 0) - (b.displayOrder || 0)
      );

      // Load posts for each phase
      this.phasesWithPosts = await Promise.all(
        phases.map(async (phase: ChallengePhase) => {
          const postsQuery: GetChallengePostsQuery = {
            userId,
            challengePhaseId: phase.id
          };
          const posts = await this.challengePostsService.list(postsQuery);
          return { phase, posts: posts || [] };
        })
      );

    } catch (err: any) {
      console.error('Error loading challenge:', err);
      this.error = err.message || 'Failed to load challenge';
    } finally {
      this.loading = false;
    }
  }

  getChallengeStatusText(status?: number): string {
    switch (status) {
      case 0: return 'Draft';
      case 1: return 'Open';
      case 2: return 'Closed';
      case 3: return 'Archived';
      default: return 'Unknown';
    }
  }

  getChallengeStatusBadgeClass(status?: number): string {
    switch (status) {
      case 0: return 'bg-secondary';
      case 1: return 'bg-success';
      case 2: return 'bg-danger';
      case 3: return 'bg-warning';
      default: return 'bg-secondary';
    }
  }

  getPhaseStatusText(status?: number): string {
    switch (status) {
      case 0: return 'Not Started';
      case 1: return 'Active';
      case 2: return 'Completed';
      case 3: return 'Archived';
      default: return 'Unknown';
    }
  }

  getPhaseStatusBadgeClass(status?: number): string {
    switch (status) {
      case 0: return 'bg-secondary';
      case 1: return 'bg-primary';
      case 2: return 'bg-success';
      case 3: return 'bg-warning';
      default: return 'bg-secondary';
    }
  }

  goBack() {
    this.router.navigate(['/list-challenges']);
  }

  editChallenge() {
    if (this.challenge?.id) {
      this.router.navigate(['/edit-challenge', this.challenge.id]);
    }
  }
}
