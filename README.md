# HR Management System

A comprehensive HR Management System built with ASP.NET Core Web API, featuring role-based authorization, JWT authentication, and complete HR operations management.

## Features

### 🔐 Authentication & Authorization
- JWT-based authentication with role-based access control
- Three user roles: Admin, HR, Employee
- Secure password hashing using BCrypt

### 👥 Employee Management
- Complete CRUD operations for employees
- Supervisor-subordinate hierarchical relationships
- Department assignment and management
- Self-service profile updates for employees

### 🏢 Department Management
- Create, read, update, and delete departments
- Department-wise employee tracking
- Prevent deletion of departments with active employees

### 📊 Attendance Tracking
- Check-in/check-out functionality
- Attendance records with timestamps
- Daily attendance reporting

### 🏝️ Leave Management
- Leave request submission and approval
- Leave balance tracking
- Overlapping leave prevention
- Role-based leave approval (HR/Admin)

### 💰 Payroll Management
- Payroll generation and calculation
- Salary components: Basic, Bonus, Deductions, Net Salary
- Payroll history and records

### 🎉 Holiday Management
- Company holiday scheduling
- Bulk holiday upload functionality
- Holiday calendar view

### 👨‍💼 HR Personnel Management
- HR staff management
- Role assignment and permissions

## Technology Stack

- **Backend**: ASP.NET Core Web API (.NET 10.0)
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT (JSON Web Tokens)
- **Password Hashing**: BCrypt.Net-Next
- **Architecture**: RESTful API with Repository Pattern

## API Endpoints

### Authentication
- `POST /api/Login/register` - User registration
- `POST /api/Login/login` - User login (returns JWT token)
- `GET /api/Login/profile` - Get user profile
- `POST /api/Login/change-password` - Change password

### Employees
- `GET /api/Employees` - Get all employees (Admin/HR only)
- `GET /api/Employees/{id}` - Get employee by ID
- `POST /api/Employees` - Create new employee (Admin/HR only)
- `PUT /api/Employees/{id}` - Update employee
- `DELETE /api/Employees/{id}` - Delete employee (Admin only)
- `GET /api/Employees/{id}/subordinates` - Get subordinates
- `GET /api/Employees/{id}/supervisor-chain` - Get supervisor hierarchy
- `GET /api/Employees/with-supervisor` - Get employees with supervisor info

### Departments
- `GET /api/Departments` - Get all departments
- `GET /api/Departments/{id}` - Get department by ID
- `POST /api/Departments` - Create department (Admin/HR only)
- `PUT /api/Departments/{id}` - Update department (Admin/HR only)
- `DELETE /api/Departments/{id}` - Delete department (Admin only)

### Attendance
- `GET /api/Attendances` - Get all attendance records (Admin/HR only)
- `GET /api/Attendances/{id}` - Get attendance by ID
- `POST /api/Attendances` - Create attendance record
- `POST /api/Attendances/check-in` - Employee check-in
- `POST /api/Attendances/check-out` - Employee check-out
- `GET /api/Attendances/today/{employeeId}` - Get today's attendance

### Leaves
- `GET /api/Leaves` - Get all leaves (Admin/HR only)
- `GET /api/Leaves/{id}` - Get leave by ID
- `POST /api/Leaves` - Create leave request
- `PUT /api/Leaves/{id}/approve` - Approve leave (HR/Admin only)
- `PUT /api/Leaves/{id}/reject` - Reject leave (HR/Admin only)

### Payroll
- `GET /api/Payrolls` - Get all payrolls (Admin/HR only)
- `GET /api/Payrolls/{id}` - Get payroll by ID
- `POST /api/Payrolls` - Create payroll (Admin/HR only)
- `POST /api/Payrolls/generate/{employeeId}` - Generate payroll for employee

### Holidays
- `GET /api/Holidays` - Get all holidays
- `GET /api/Holidays/{id}` - Get holiday by ID
- `POST /api/Holidays` - Create holiday (Admin/HR only)
- `POST /api/Holidays/bulk` - Bulk upload holidays (Admin/HR only)

### HR Personnel
- `GET /api/HR` - Get all HR personnel (Admin only)
- `GET /api/HR/{id}` - Get HR by ID
- `POST /api/HR` - Create HR record (Admin only)
- `PUT /api/HR/{id}` - Update HR record (Admin only)
- `DELETE /api/HR/{id}` - Delete HR record (Admin only)

## Role-Based Access Control

### Admin
- Full system access
- Can manage all entities
- Can assign roles
- Can delete any record

### HR
- Manage employees, departments, attendance, leaves, payroll, holidays
- Cannot delete departments with employees
- Cannot access system settings

### Employee
- View own profile and attendance
- Apply for leaves
- Check-in/check-out
- View own payroll
- Cannot modify other employees' data

## Setup Instructions

### Prerequisites
- .NET 10.0 SDK
- SQL Server
- Git

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/Sahor568/HR-Management-System.git
   cd HR-Management-System/Management/Management
   ```

2. Update database connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=HRManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```

3. Apply database migrations:
   ```bash
   dotnet ef database update --context ManagementContext
   ```

4. Run the application:
   ```bash
   dotnet run
   ```

5. The API will be available at `https://localhost:5001` or `http://localhost:5000`

### Initial Setup
1. Register the first admin user using the `/api/Login/register` endpoint
2. Use the admin credentials to create other users and setup the system

## Database Schema

The system uses the following main entities:
- **User**: Authentication and user information
- **Employee**: Employee details with supervisor relationship
- **Department**: Organizational departments
- **Attendance**: Daily attendance records
- **Leave**: Employee leave requests
- **Payroll**: Salary and compensation records
- **Holiday**: Company holidays
- **HR**: HR personnel management

## Supervisor Relationship

The system supports hierarchical supervisor relationships:
- Employees can have a supervisor (who is also an employee)
- Supervisors can have multiple subordinates
- Circular references are prevented
- Supervisor chain can be traversed using API endpoints

## Security Features

- JWT tokens with expiration
- Role-based authorization policies
- Password hashing with BCrypt
- Input validation and sanitization
- SQL injection prevention via Entity Framework
- CORS configuration for frontend integration

## Testing the API

Use tools like Postman or Swagger to test the API endpoints. Include the JWT token in the Authorization header:
```
Authorization: Bearer {your_jwt_token}
```

## Project Structure

```
Management/
├── Controllers/          # API controllers
├── Models/              # Entity models and DbContext
├── Data/                # Database context
├── Migrations/          # Entity Framework migrations
├── Properties/          # Project properties
├── Views/               # MVC views (if needed)
├── wwwroot/             # Static files
├── Program.cs           # Application startup
├── appsettings.json     # Configuration
└── README.md            # This file
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License.

## Support

For issues and feature requests, please create an issue in the GitHub repository.

---

**Developed with ❤️ for HR Management Solutions**