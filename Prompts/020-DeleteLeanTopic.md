Given
- I am using the lean-sessions/view screen
- there's an existing topic on the board

When
- I click the delete button

Then
- The system should ask me if I really want to delete this topic?
- If I confirm, then the system should delete the lean topic
- The system should update the appropriate kanban lane
- The system should notify other clients appropriately