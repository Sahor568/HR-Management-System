# HR Management System - System Architecture

## Overview

The HR Management System is a modern, enterprise-grade application built on ASP.NET Core MVC architecture. It provides comprehensive HR management capabilities with role-based access control and real-time notifications.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         Client Layer (Frontend)                  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              Razor Views (21 templates)                   │  │
│  │  ┌──────────────────────────────────────────────────────┐ │  │
│  │  │ Authentication │ Dashboard │ Employee │ Leave │...   │ │  │
│  │  └──────────────────────────────────────────────────────┘ │  │
│  │                                                            │  │
│  │  ┌──────────────────────────────────────────────────────┐ │  │
│  │  │  Bootstrap 5 │ Font Awesome │ Custom CSS │ jQuery   │ │  │
│  │  └──────────────────────────────────────────────────────┘ │  │
│  │                                                            │  │
│  │  ┌──────────────────────────────────────────────────────┐ │  │
│  │  │ site.js - API Client Library & Helpers              │ │  │
│  │  └──────────────────────────────────────────────────────┘ │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────┬──────────────────────────────────┘
                              │
                    HTTP/HTTPS (REST)
                              │
┌─────────────────────────────▼──────────────────────────────────┐
│                 API Layer (Request/Response)                    │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  JWT Bearer Authentication  │  Cookie Sessions          │  │
│  │  CORS Configuration         │  Request Logging          │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────┬──────────────────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────────────┐
│              Controller Layer (15 Controllers)                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ Home │ Dashboard │ Employees │ Departments │ Leaves │... │  │
│  │ (CRUD Operations & Business Logic)                       │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────┬──────────────────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────────────┐
│              Service Layer (3 Services)                         │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ LogService    │ NotificationService │ Middleware         │  │
│  │ (Business Logic, Validation, Authorization)             │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────┬──────────────────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────────────┐
│          Data Access Layer (Entity Framework Core)             │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  ManagementContext (DbContext)                          │  │
│  │  ┌──────────────────────────────────────────────────────┐ │  │
│  │  │ DbSet<User> │ DbSet<Employee> │ DbSet<Leave> │...  │ │  │
│  │  └──────────────────────────────────────────────────────┘ │  │
│  │  Relationships │ Migrations │ Lazy Loading              │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────┬──────────────────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────────────┐
│              Database Layer (SQL Server)                        │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ Users │ Employees │ Departments │ Leaves │ Attendance   │  │
│  │ Payroll │ Holidays │ Notifications │ Logs │ Roles       │  │
│  │ (11 Tables with Relationships & Indexes)                │  │
│  └──────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

## Request/Response Flow

```
User Interface
    │
    ▼ (Click Button/Submit Form)
JavaScript (site.js)
    │
    ▼ (HTTP Request with JWT Token)
API Controller
    │
    ├─→ Authentication Check ──→ [Pass] ──┐
    │   (JWT Validation)           [Fail] ──→ 401 Unauthorized
    │
    ├─→ Authorization Check ───→ [Pass] ──┐
    │   (Role/Policy Check)       [Fail] ──→ 403 Forbidden
    │
    ├─→ Service Layer ──────→ Business Logic
    │   (Validation, Processing)
    │
    ├─→ Data Access Layer ──→ EF Core
    │   (Query Building)
    │
    ▼
SQL Server
    │
    ├─→ Query Execution
    │
    └─→ Return Data
         │
         ▼
Service Layer
    │
    ├─→ Process Results
    │   (Formatting, Calculations)
    │
    ▼
Controller
    │
    ├─→ Format Response
    │   (JSON Serialization)
    │
    ▼
HTTP Response
    │
    ▼ (Receive JSON)
JavaScript
    │
    ├─→ Parse Response
    │
    ├─→ Update UI
    │   (DOM Manipulation)
    │
    └─→ Show Notification
        (Success/Error)

User sees result
```

## Data Flow

### Authentication Flow
```
1. User enters credentials
2. POST /api/Login/login
3. Verify BCrypt password hash
4. Generate JWT token
5. Store token in localStorage
6. Cookie auth via /Home/Login
7. Subsequent requests include Bearer token
8. TokenValidationParameters validate JWT
9. Claims extracted for authorization
```

### Database Schema

```sql
Users (1:Many)
├── Employees
│   ├── Department (N:1)
│   ├── Supervisor (Self-referential)
│   ├── Attendances (1:Many)
│   ├── Leaves (1:Many)
│   └── Payrolls (1:Many)
│
Departments (1:Many)
├── Employees
│
Leaves (N:1)
├── Employee
│
Attendances (N:1)
├── Employee
│
Payroll (N:1)
├── Employee
│
Holidays (Standalone)
│
Notifications (N:1)
├── User
│
SystemLogs (Standalone)
│
Roles (Static)
```

## Component Interaction

### 1. Authentication Component
```
Login Form
   │
   ▼
HomeController.ApiLogin()
   │
   ├─→ Validate credentials
   ├─→ Hash & verify password (BCrypt)
   ├─→ Generate JWT token
   └─→ Return token & user info
        │
        ▼
localStorage
   ├─ auth_token
   ├─ user_email
   ├─ user_role
   ├─ user_id
   └─ user_employeeId
        │
        ▼
Subsequent API calls with Bearer token
```

### 2. Dashboard Component
```
Dashboard View
   │
   ├─→ DashboardAPI.getStats()
   │   └─→ DashboardController.GetDashboardStats()
   │       ├─→ Count employees
   │       ├─→ Count attendance today
   │       ├─→ Count pending leaves
   │       └─→ Calculate payroll (HR/Admin only)
   │
   ├─→ DashboardAPI.getAttendanceTrend()
   │   └─→ DashboardController.GetAttendanceTrend()
   │
   ├─→ DashboardAPI.getLeaveSummary()
   │   └─→ DashboardController.GetLeaveSummary()
   │
   └─→ DashboardAPI.getEmployeeDistribution()
       └─→ DashboardController.GetEmployeeDistribution()

All data aggregated and displayed in charts
```

### 3. CRUD Operations
```
List View (e.g., Employees)
   │
   ├─→ Load Data
   │   └─→ EmployeesAPI.getAll()
   │       └─→ EmployeesController.GetAll()
   │           └─→ EF Query: SELECT * FROM Employees
   │
   ├─→ Add Button → Modal Opens
   │   │
   │   └─→ Submit Form
   │       └─→ EmployeesAPI.create(data)
   │           └─→ EmployeesController.Create()
   │               ├─→ Validate data
   │               ├─→ Check authorization
   │               ├─→ Save to database
   │               └─→ Return new record
   │
   ├─→ Edit Button → Modal Opens with Data
   │   │
   │   └─→ EmployeesAPI.update(id, data)
   │       └─→ EmployeesController.Update()
   │           └─→ Save changes to database
   │
   └─→ Delete Button → Confirm → Delete
       └─→ EmployeesAPI.delete(id)
           └─→ EmployeesController.Delete()
               └─→ Remove from database
```

## Security Architecture

### Authentication Chain
```
Request
   │
   ├─→ Middleware: UseAuthentication()
   │   │
   │   ├─ Check JWT Bearer token
   │   │   └─→ Decode & validate
   │   │       ├─ Check signature
   │   │       ├─ Check expiration
   │   │       ├─ Check issuer/audience
   │   │
   │   └─ Check Cookie Auth
   │       └─→ Validate session
   │           ├─ Check if valid
   │           └─ Check if expired
   │
   └─→ Middleware: UseAuthorization()
       │
       ├─ Check policy requirements
       ├─ Check role membership
       ├─ Check claims
       │
       └─→ [Authorized] ✓ → Continue
           └→ [Denied] ✗ → 401/403
```

### Data Protection
```
Password Storage:
  User Input → BCrypt.HashPassword() → Database

API Communication:
  Request → HTTPS → Response
  XSS Prevention: Razor view escaping
  SQL Injection: EF Core parameterized queries

Session Security:
  JWT Token: Exp time, signed
  Cookie: HttpOnly, Secure flag, SameSite
```

## Performance Optimization

### Database Optimization
```
Eager Loading:
  var employees = await _context.Employees
    .Include(e => e.Department)
    .Include(e => e.Supervisor)
    .ToListAsync()

Query Optimization:
  - Indexes on foreign keys
  - Pagination (Skip/Take)
  - Filtered queries

Caching:
  - Response caching
  - Browser caching for static files
```

### Frontend Optimization
```
CSS/JS Delivery:
  - Bootstrap from CDN
  - Font Awesome from CDN
  - Minified custom CSS
  - Async JavaScript loading

Performance:
  - Lazy component loading
  - Client-side filtering/searching
  - Pagination in tables
  - Debounced input handlers
```

## Deployment Architecture

### Development
```
Local Machine
├── Visual Studio / VS Code
├── .NET 10.0 SDK
├── LocalDB / SQL Server
└── dotnet run → localhost:5000
```

### Production
```
Server
├── IIS (Windows) or Linux container
├── .NET 10.0 Runtime
├── SQL Server
├── SSL/TLS Certificate
└── Load Balancer (optional)

Docker (Alternative):
├── Dockerfile
├── Docker Image
├── Container Registry
└── Kubernetes (optional)
```

## Monitoring & Logging

### Logging Architecture
```
Application
   │
   ├─→ Serilog Configuration
   │   ├─ Console Sink
   │   ├─ File Sink (daily rotation)
   │   └─ Structured Logging
   │
   ├─→ Request Logging Middleware
   │   ├─ Log all requests
   │   ├─ Include headers
   │   └─ Log response status
   │
   ├─→ System Logs
   │   ├─ User actions
   │   ├─ Data changes
   │   └─ Errors/exceptions
   │
   └─→ Notification Service
       └─ Create notifications for events
           └─ Store in database
```

## Error Handling

```
Try-Catch Blocks
   │
   ├─ Controller Level: Catch & return BadRequest
   ├─ Service Level: Log & throw
   └─ Middleware Level: Log & return error response

User Experience:
   Error → API Response → JavaScript → Toast Notification
```

## Conclusion

The HR Management System follows proven architectural patterns:
- **Separation of Concerns**: Views, Controllers, Services, Data Access
- **DRY Principle**: Reusable components and utilities
- **SOLID Principles**: Single responsibility, Open/closed, etc.
- **Security First**: Authentication, Authorization, Data Protection
- **Scalability**: Async operations, efficient queries, caching

This architecture supports:
✓ Easy maintenance and updates
✓ Horizontal scaling (stateless)
✓ Testing and debugging
✓ Security and compliance
✓ Performance optimization
