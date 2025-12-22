import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkDragDrop, moveItemInArray, transferArrayItem, DragDropModule } from '@angular/cdk/drag-drop';
import { LeanTopic, TopicStatus } from '../../models/lean-session.models';
import { KanbanColumnComponent } from '../kanban-column/kanban-column.component';

@Component({
  selector: 'app-kanban-board',
  standalone: true,
  imports: [CommonModule, DragDropModule, KanbanColumnComponent],
  templateUrl: './kanban-board.component.html',
  styleUrls: ['./kanban-board.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class KanbanBoardComponent {
  @Input({ required: true }) topics: LeanTopic[] = [];
  @Input() currentUserVotes: string[] = [];
  @Input() currentUserId: string = '';
  
  @Output() addTopic = new EventEmitter<void>();
  @Output() editTopic = new EventEmitter<string>();
  @Output() deleteTopic = new EventEmitter<string>();
  @Output() voteTopic = new EventEmitter<{ topicId: string; vote: boolean }>();
  @Output() moveTopic = new EventEmitter<{ topicId: string; newStatus: TopicStatus }>();
  
  readonly columnStatuses = [TopicStatus.ToDiscuss, TopicStatus.Discussing, TopicStatus.Discussed];
  
  getTopicsByStatus(status: TopicStatus): LeanTopic[] {
    const filtered = this.topics.filter(t => t.status === status);
    // Sort "To Discuss" by vote count descending
    if (status === TopicStatus.ToDiscuss) {
      return filtered.sort((a, b) => b.voteCount - a.voteCount);
    }
    return filtered;
  }
  
  onTopicDropped(event: CdkDragDrop<LeanTopic[]>): void {
    if (event.previousContainer === event.container) {
      // Same column - just reorder (not needed for our use case, but keep for future)
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      // Different column - move topic to new status
      const topic = event.previousContainer.data[event.previousIndex];
      const newStatus = this.getStatusFromListId(event.container.id);
      
      if (newStatus && topic) {
        this.moveTopic.emit({ topicId: topic.id, newStatus });
      }
    }
  }
  
  private getStatusFromListId(listId: string): TopicStatus | null {
    if (listId.includes('ToDiscuss')) return TopicStatus.ToDiscuss;
    if (listId.includes('Discussing')) return TopicStatus.Discussing;
    if (listId.includes('Discussed')) return TopicStatus.Discussed;
    return null;
  }
}
