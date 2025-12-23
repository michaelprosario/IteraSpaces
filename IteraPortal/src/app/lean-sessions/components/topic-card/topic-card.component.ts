import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LeanTopic } from '../../models/lean-session.models';

@Component({
  selector: 'app-topic-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './topic-card.component.html',
  styleUrls: ['./topic-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TopicCardComponent {
  @Input({ required: true }) topic!: LeanTopic;
  @Input() isVoted = false;
  @Input() isActive = false;
  @Input() canEdit = false;
  
  @Output() edit = new EventEmitter<void>();
  @Output() delete = new EventEmitter<void>();
  @Output() vote = new EventEmitter<boolean>();
  
  private isExpandedSignal = signal(false);
  public isExpanded = this.isExpandedSignal.asReadonly();
  
  toggleExpanded(): void {
    this.isExpandedSignal.update(val => !val);
  }
  
  onVoteClick(): void {
    this.vote.emit(!this.isVoted);
  }
  
  getInitials(name: string): string {
    if (!name) return '?';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  }
}
