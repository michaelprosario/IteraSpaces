import { Component, Input, Output, EventEmitter, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LeanTopic } from '../../models/lean-session.models';
import { LeanTopicsService } from '../../../core/services/lean-topics.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-add-topic-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-topic-modal.component.html',
  styleUrls: ['./add-topic-modal.component.scss']
})
export class AddTopicModalComponent implements OnInit {
  private leanTopicsService = inject(LeanTopicsService);
  private authService = inject(AuthService);
  
  @Input({ required: true }) sessionId!: string;
  @Input() topicToEdit?: LeanTopic;
  @Output() topicSaved = new EventEmitter<void>();
  @Output() modalClosed = new EventEmitter<void>();
  
  title = signal('');
  description = signal('');
  isSaving = signal(false);
  error = signal<string | null>(null);
  
  readonly maxTitleLength = 100;
  readonly maxDescriptionLength = 500;
  
  ngOnInit(): void {
    if (this.topicToEdit) {
      this.title.set(this.topicToEdit.title || '');
      this.description.set(this.topicToEdit.description || '');
    }
  }
  
  get isEditMode(): boolean {
    return !!this.topicToEdit;
  }
  
  get remainingTitleChars(): number {
    return this.maxTitleLength - this.title().length;
  }
  
  get remainingDescriptionChars(): number {
    return this.maxDescriptionLength - this.description().length;
  }
  
  async save(): Promise<void> {
    const titleValue = this.title().trim();
    
    if (!titleValue) {
      this.error.set('Title is required');
      return;
    }
    
    if (titleValue.length > this.maxTitleLength) {
      this.error.set(`Title must be ${this.maxTitleLength} characters or less`);
      return;
    }
    
    const descriptionValue = this.description().trim();
    if (descriptionValue.length > this.maxDescriptionLength) {
      this.error.set(`Description must be ${this.maxDescriptionLength} characters or less`);
      return;
    }
    
    this.isSaving.set(true);
    this.error.set(null);
    
    try {
      const currentUser = this.authService.currentUser();
      
      const topic: any = {
        id: this.topicToEdit?.id,
        leanSessionId: this.sessionId,
        title: titleValue,
        description: descriptionValue || undefined,
        submittedByUserId: currentUser?.id,
        createdBy: currentUser?.id,
        updatedBy: currentUser?.id
      };
      
      await this.leanTopicsService.storeEntity(topic);
      this.topicSaved.emit();
      this.close();
    } catch (error: any) {
      console.error('Error saving topic:', error);
      this.error.set(error.message || 'Failed to save topic. Please try again.');
    } finally {
      this.isSaving.set(false);
    }
  }
  
  close(): void {
    this.modalClosed.emit();
  }
  
  onTitleChange(value: string): void {
    this.title.set(value);
    this.error.set(null);
  }
  
  onDescriptionChange(value: string): void {
    this.description.set(value);
    this.error.set(null);
  }
}
