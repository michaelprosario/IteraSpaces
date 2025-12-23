import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LeanSessionNote, NoteType } from '../../models/lean-session.models';

@Component({
  selector: 'app-session-notes',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './session-notes.component.html',
  styleUrls: ['./session-notes.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SessionNotesComponent {
  @Input() sessionId: string = '';
  @Input() notes: LeanSessionNote[] = [];
  
  getNoteTypeBadgeClass(noteType: NoteType): string {
    switch (noteType) {
      case NoteType.Decision:
        return 'bg-success';
      case NoteType.ActionItem:
        return 'bg-warning text-dark';
      case NoteType.KeyPoint:
        return 'bg-info';
      case NoteType.General:
      default:
        return 'bg-secondary';
    }
  }
  
  getNoteTypeIcon(noteType: NoteType): string {
    switch (noteType) {
      case NoteType.Decision:
        return 'bi-check-circle';
      case NoteType.ActionItem:
        return 'bi-list-check';
      case NoteType.KeyPoint:
        return 'bi-lightbulb';
      case NoteType.General:
      default:
        return 'bi-journal-text';
    }
  }
  
  formatDate(date: Date): string {
    return new Date(date).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }
  
  trackByNoteId(index: number, note: LeanSessionNote): string {
    return note.id;
  }
}
