import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ChallengesService, Challenge, ChallengeStatus } from '../core/services/challenges.service';
import { ChallengePhasesService, ChallengePhase, ChallengePhaseStatus } from '../core/services/challenge-phases.service';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-edit-challenge',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './edit-challenge.html',
  styleUrl: './edit-challenge.scss',
})
export class EditChallenge implements OnInit {
  private challengesService = inject(ChallengesService);
  private phasesService = inject(ChallengePhasesService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);

  challenge: Challenge = {};
  phases: ChallengePhase[] = [];
  isEditMode = false;
  loading = false;
  savingChallenge = false;
  savingPhases = false;
  error = '';
  phaseError = '';
  successMessage = '';
  
  ChallengeStatus = ChallengeStatus;
  ChallengePhaseStatus = ChallengePhaseStatus;

  currentUser = this.authService.currentUser;

  newPhase: ChallengePhase = {
    name: '',
    description: '',
    status: ChallengePhaseStatus.NotStarted,
    displayOrder: 0
  };

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      await this.loadChallenge(id);
    } else {
      this.challenge.status = ChallengeStatus.Draft;
    }
  }

  async loadChallenge(id: string): Promise<void> {
    try {

      this.loading = true;
      this.error = '';
      this.cdr.detectChanges();
      
      const userId = this.currentUser()?.id || '';
      console.log('Fetching challenge with id:', id, 'userId:', userId);
      const result = await this.challengesService.get({ userId, challengeId: id });
      console.log('Challenge result loaded from API:', result);
      console.log('Challenge name:', result.challenge?.name);
      console.log('Challenge description:', result.challenge?.description);
      console.log('Challenge status:', result.challenge?.status);
      console.log('Challenge category:', result.challenge?.category);
      console.log('Phases count:', result.phases?.length);
      console.log('Total posts:', result.totalPosts);
      
      // Direct assignment from result
      this.challenge = result.challenge;
      this.phases = result.phases || [];
      this.phases.sort((a, b) => (a.displayOrder || 0) - (b.displayOrder || 0));
      console.log('Challenge assigned to component:', this.challenge);
      console.log('this.challenge.name:', this.challenge.name);
      
      // Force change detection multiple times
      this.cdr.detectChanges();
      setTimeout(() => this.cdr.detectChanges(), 0);
    } catch (error: any) {
      this.error = error.message || 'Failed to load challenge';
      console.error('Error loading challenge:', error);
      this.cdr.detectChanges();
    } finally {
      this.loading = false;
      this.cdr.detectChanges();
    }
  }

  async loadPhases(challengeId: string): Promise<void> {
    try {
      const userId = this.currentUser()?.id || '';
      this.phases = await this.phasesService.list({ userId, challengeId });
      this.phases.sort((a, b) => (a.displayOrder || 0) - (b.displayOrder || 0));
      this.cdr.detectChanges();
    } catch (error: any) {
      this.phaseError = error.message || 'Failed to load phases';
      console.error('Error loading phases:', error);
      this.cdr.detectChanges();
    }
  }

  async saveChallenge(): Promise<void> {
    try {
      this.savingChallenge = true;
      this.error = '';
      this.successMessage = '';
      this.cdr.detectChanges();
      
      const userId = this.currentUser()?.id || '';
      this.challenge.createdByUserId = userId;
      
      // Assign GUID for new challenges
      if (!this.isEditMode && !this.challenge.id) {
        this.challenge.id = crypto.randomUUID();
      }
      
      const savedChallenge = await this.challengesService.store(this.challenge);
      
      // If this is a new challenge, create default phases
      if (!this.isEditMode && savedChallenge.id) {
        await this.createDefaultPhases(savedChallenge.id);
        this.isEditMode = true;
        this.challenge = savedChallenge;
        this.successMessage = 'Challenge and default phases created successfully!';
        this.cdr.detectChanges();
        // Navigate to edit mode
        this.router.navigate(['/challenges/edit', savedChallenge.id]);
      } else {
        this.successMessage = 'Challenge saved successfully!';
        this.cdr.detectChanges();
      }
    } catch (error: any) {
      this.error = error.message || 'Failed to save challenge';
      console.error('Error saving challenge:', error);
      this.cdr.detectChanges();
    } finally {
      this.savingChallenge = false;
      this.cdr.detectChanges();
    }
  }

  async createDefaultPhases(challengeId: string): Promise<void> {
    const defaultPhases = [
      { name: 'Problems to solve', description: 'Identify and document problems to address', displayOrder: 1 },
      { name: 'Ideas', description: 'Brainstorm potential solutions', displayOrder: 2 },
      { name: 'Draft proposals', description: 'Develop initial proposals', displayOrder: 3 },
      { name: 'Refined proposals', description: 'Refine and finalize proposals', displayOrder: 4 }
    ];

    const userId = this.currentUser()?.id || '';
    
    for (const phaseData of defaultPhases) {
      const phase: ChallengePhase = {
        id: crypto.randomUUID(),
        challengeId,
        name: phaseData.name,
        description: phaseData.description,
        status: ChallengePhaseStatus.NotStarted,
        displayOrder: phaseData.displayOrder
      };
      await this.phasesService.store(phase);
    }
    
    await this.loadPhases(challengeId);
  }

  async addPhase(): Promise<void> {
    if (!this.challenge.id) {
      this.phaseError = 'Please save the challenge first before adding phases';
      this.cdr.detectChanges();
      return;
    }

    if (!this.newPhase.name) {
      this.phaseError = 'Phase name is required';
      this.cdr.detectChanges();
      return;
    }

    try {
      this.phaseError = '';
      this.newPhase.id = crypto.randomUUID();
      this.newPhase.challengeId = this.challenge.id;
      this.newPhase.displayOrder = this.phases.length + 1;
      
      await this.phasesService.store(this.newPhase);
      await this.loadPhases(this.challenge.id);
      
      // Reset form
      this.newPhase = {
        name: '',
        description: '',
        status: ChallengePhaseStatus.NotStarted,
        displayOrder: 0
      };
      this.cdr.detectChanges();
    } catch (error: any) {
      this.phaseError = error.message || 'Failed to add phase';
      console.error('Error adding phase:', error);
      this.cdr.detectChanges();
    }
  }

  async savePhase(phase: ChallengePhase): Promise<void> {
    try {
      this.savingPhases = true;
      this.phaseError = '';
      this.cdr.detectChanges();
      
      await this.phasesService.store(phase);
      await this.loadPhases(this.challenge.id!);
    } catch (error: any) {
      this.phaseError = error.message || 'Failed to save phase';
      console.error('Error saving phase:', error);
      this.cdr.detectChanges();
    } finally {
      this.savingPhases = false;
      this.cdr.detectChanges();
    }
  }

  async deletePhase(phaseId: string): Promise<void> {
    if (!confirm('Are you sure you want to delete this phase? This action cannot be undone.')) {
      return;
    }

    try {
      const userId = this.currentUser()?.id || '';
      await this.phasesService.delete({ userId, entityId: phaseId });
      await this.loadPhases(this.challenge.id!);
      this.cdr.detectChanges();
    } catch (error: any) {
      this.phaseError = error.message || 'Failed to delete phase';
      console.error('Error deleting phase:', error);
      this.cdr.detectChanges();
    }
  }

  async deleteChallenge(): Promise<void> {
    if (!confirm('Are you sure you want to delete this challenge? This will also delete all associated phases and posts. This action cannot be undone.')) {
      return;
    }

    try {
      this.loading = true;
      this.error = '';
      this.cdr.detectChanges();
      
      const userId = this.currentUser()?.id || '';
      await this.challengesService.delete({ userId, entityId: this.challenge.id! });
      this.router.navigate(['/challenges/list']);
    } catch (error: any) {
      this.error = error.message || 'Failed to delete challenge';
      console.error('Error deleting challenge:', error);
      this.cdr.detectChanges();
    } finally {
      this.loading = false;
      this.cdr.detectChanges();
    }
  }

  cancel(): void {
    this.router.navigate(['/challenges/list']);
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

  getPhaseStatusLabel(status?: ChallengePhaseStatus): string {
    switch (status) {
      case ChallengePhaseStatus.NotStarted:
        return 'Not Started';
      case ChallengePhaseStatus.Active:
        return 'Active';
      case ChallengePhaseStatus.Completed:
        return 'Completed';
      case ChallengePhaseStatus.Archived:
        return 'Archived';
      default:
        return 'Unknown';
    }
  }
}
