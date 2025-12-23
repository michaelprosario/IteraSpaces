import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { LeanTopic, TopicStatus } from '../../models/lean-session.models';
import { TopicCardComponent } from '../topic-card/topic-card.component';

@Component({
  selector: 'app-kanban-column',
  standalone: true,
  imports: [CommonModule, DragDropModule, TopicCardComponent],
  templateUrl: './kanban-column.component.html',
  styleUrls: ['./kanban-column.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class KanbanColumnComponent {
  @Input({ required: true }) status!: TopicStatus;
  @Input({ required: true }) topics: LeanTopic[] = [];
  @Input() currentUserVotes: string[] = [];
  @Input() currentUserId: string = '';
  
  @Output() addTopic = new EventEmitter<void>();
  @Output() editTopic = new EventEmitter<string>();
  @Output() deleteTopic = new EventEmitter<string>();
  @Output() voteTopic = new EventEmitter<{ topicId: string; vote: boolean }>();
  @Output() topicDropped = new EventEmitter<CdkDragDrop<LeanTopic[]>>();
  
  getColumnTitle(status: TopicStatus): string {
    switch (status) {
      case TopicStatus.ToDiscuss:
        return 'To Discuss';
      case TopicStatus.Discussing:
        return 'Discussing';
      case TopicStatus.Discussed:
        return 'Discussed';
      case TopicStatus.Archived:
        return 'Archived';
      default:
        return 'Unknown';
    }
  }
  
  getColumnClass(): string {
    switch (this.status) {
      case TopicStatus.ToDiscuss:
        return 'column-todiscuss';
      case TopicStatus.Discussing:
        return 'column-discussing';
      case TopicStatus.Discussed:
        return 'column-discussed';
      default:
        return '';
    }
  }
  
  getColumnIcon(status: TopicStatus): string {
    switch (status) {
      case TopicStatus.ToDiscuss:
        return 'bi bi-list-task';
      case TopicStatus.Discussing:
        return 'bi bi-chat-dots';
      case TopicStatus.Discussed:
        return 'bi bi-check2-circle';
      default:
        return 'bi bi-circle';
    }
  }
  
  trackByTopicId(index: number, topic: LeanTopic): string {
    return topic.id;
  }
  
  canEditTopic(topic: LeanTopic): boolean {
    return topic.authorId === this.currentUserId;
  }
  
  isToDiscuss(): boolean {
    return this.status === TopicStatus.ToDiscuss;
  }
  
  isDiscussing(topicStatus: TopicStatus): boolean {
    return topicStatus === TopicStatus.Discussing;
  }
  
  onTopicDropped(event: CdkDragDrop<LeanTopic[]>): void {
    this.topicDropped.emit(event);
  }
}
