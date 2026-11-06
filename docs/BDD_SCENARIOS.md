# BDD Acceptance Scenarios

## Tenant data isolation
**Given** two companies have registered separate workspaces  
**When** an authenticated user from Company A requests the ticket list  
**Then** only tickets with Company A's tenant identifier are returned  
**And** a ticket belonging to Company B cannot be read, updated, or deleted by Company A.

## Ticket creation
**Given** an authenticated employee in a workspace  
**When** they submit a valid title, description, priority, and status  
**Then** the API creates a ticket linked to the authenticated tenant and user  
**And** returns HTTP 201 with the created ticket.

## Invalid ticket priority
**Given** an authenticated user  
**When** they submit a priority outside Low, Medium, High, or Critical  
**Then** the API returns HTTP 400 and does not save the ticket.

## Admin-only deletion
**Given** an authenticated employee  
**When** they attempt to delete a ticket  
**Then** the API returns HTTP 403  
**And** an Admin can delete a ticket belonging to their own tenant.

