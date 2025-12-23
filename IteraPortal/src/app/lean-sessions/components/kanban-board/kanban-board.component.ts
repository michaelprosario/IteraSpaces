import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy, computed, signal } from '@angular/core';
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
  private topicsSignal = signal<LeanTopic[]>([]);
  
  @Input({ required: true }) set topics(value: LeanTopic[]) {
    this.topicsSignal.set(value || []);
  }
  get topics(): LeanTopic[] {
    return this.topicsSignal();
  }
  
  @Input() currentUserVotes: string[] = [];
  @Input() currentUserId: string = '';
  
  @Output() addTopic = new EventEmitter<void>();
  @Output() editTopic = new EventEmitter<string>();
  @Output() deleteTopic = new EventEmitter<string>();
  @Output() voteTopic = new EventEmitter<{ topicId: string; vote: boolean }>();
  @Output() moveTopic = new EventEmitter<{ topicId: string; newStatus: TopicStatus }>();
  
  readonly columnStatuses = [TopicStatus.ToDiscuss, TopicStatus.Discussing, TopicStatus.Discussed];
  
  getTopicsByStatus(status: TopicStatus): LeanTopic[] {
    const filtered = this.topicsSignal().filter(t => t.status === status);
    // Sort "To Discuss" by vote count descending
    if (status === TopicStatus.ToDiscuss) {
      return filtered.sort((a, b) => b.voteCount - a.voteCount);
    }
    return filtered;
  }
  
  onTopicDropped(event: CdkDragDrop<LeanTopic[]>): void {
    console.log('[KanbanBoard] Topic dropped:', {
      previousContainer: event.previousContainer.id,
      currentContainer: event.container.id,
      previousIndex: event.previousIndex,
      currentIndex: event.currentIndex
    });
    
    if (event.previousContainer === event.container) {
      // Same column - just reorder (not needed for our use case, but keep for future)
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      // Different column - move topic to new status
      const topic = event.previousContainer.data[event.previousIndex];
      const newStatus = this.getStatusFromListId(event.container.id);
      
      console.log('[KanbanBoard] Moving topic:', {
        topicId: topic?.id,
        topicTitle: topic?.title,
        newStatus: newStatus
      });
      
      if (newStatus !== null && topic) {
        this.moveTopic.emit({ topicId: topic.id, newStatus });
      } else {
        console.error('[KanbanBoard] Could not determine new status from:', event.container.id);
      }
    }
  }
  
  private getStatusFromListId(listId: string): TopicStatus | null {
    // Extract the numeric status from the list ID (e.g., "list-0" -> 0)
    const match = listId.match(/list-(\d+)/);
    if (match) {
      const statusValue = parseInt(match[1], 10);
      if (statusValue === TopicStatus.ToDiscuss) return TopicStatus.ToDiscuss;
      if (statusValue === TopicStatus.Discussing) return TopicStatus.Discussing;
      if (statusValue === TopicStatus.Discussed) return TopicStatus.Discussed;
    }
    return null;
  }
}
