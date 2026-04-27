# Supervisor Relationship Implementation

## Overview
Added supervisor relationship to the Employee model to support hierarchical organization structure where employees can have supervisors (who are also employees).

## Changes Made

### 1. Employee Model (`Models/Employee.cs`)
- Added `SupervisorId` (nullable long) property
- Added `Supervisor` navigation property (Employee)
- Added `Subordinates` collection for inverse relationship

### 2. ManagementContext (`Models/ManagementContext.cs`)
- Added self-referencing relationship configuration in `OnModelCreating`:
  ```csharp
  modelBuilder.Entity<Employee>()
      .HasOne(e => e.Supervisor)
      .WithMany(e => e.Subordinates)
      .HasForeignKey(e => e.SupervisorId)
      .OnDelete(DeleteBehavior.Restrict);
  ```

### 3. EmployeesController (`Controllers/EmployeesController.cs`)
- Updated existing endpoints to include Supervisor in queries
- Added supervisor validation in POST and PUT methods
- Added circular reference prevention logic
- Added new endpoints:

#### New Endpoints:
- `GET /api/Employees/{id}/subordinates` - Get all subordinates for a supervisor
- `GET /api/Employees/{id}/supervisor-chain` - Get the supervisor hierarchy
- `GET /api/Employees/with-supervisor` - Get employees with supervisor info

### 4. Database Migration
Created migration `AddSupervisorToEmployee` to add SupervisorId column to Employees table.

## Database Update Instructions

### Option 1: Apply Migration (Recommended)
1. Fix database connection SSL issue in appsettings.json if needed
2. Run: `dotnet ef database update --context ManagementContext`

### Option 2: Manual SQL Script
If migration cannot be applied, run this SQL on your database:
```sql
ALTER TABLE Employees ADD SupervisorId BIGINT NULL;
ALTER TABLE Employees ADD CONSTRAINT FK_Employees_Supervisor FOREIGN KEY (SupervisorId) REFERENCES Employees(Id) ON DELETE NO ACTION;
```

### Option 3: Recreate Database
Delete existing database and let Entity Framework recreate it with the new schema.

## API Usage Examples

### Assign Supervisor to Employee
```http
PUT /api/Employees/5
Authorization: Bearer {token}
Content-Type: application/json

{
  "id": 5,
  "fullName": "John Doe",
  "supervisorId": 2,
  ... other fields
}
```

### Get Employee with Supervisor
```http
GET /api/Employees/5
Authorization: Bearer {token}
```

### Get Subordinates
```http
GET /api/Employees/2/subordinates
Authorization: Bearer {token}
```

### Get Supervisor Chain
```http
GET /api/Employees/5/supervisor-chain
Authorization: Bearer {token}
```

## Validation Rules
1. Employee cannot be their own supervisor
2. Circular references are prevented (supervisor cannot be a subordinate)
3. Supervisor must exist in the system
4. Only Admin/HR can assign supervisors

## Role-Based Access
- **Admin/HR**: Can assign/update supervisor relationships
- **Employee**: Can view their own supervisor but cannot modify
- **All roles**: Can view supervisor chain for any employee they have access to

## Notes
- The `SupervisorId` is nullable, meaning employees may not have a supervisor
- The relationship uses `DeleteBehavior.Restrict` to prevent accidental deletion of supervisors with subordinates
- Circular reference detection uses BFS algorithm to traverse subordinate hierarchy