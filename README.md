# HR Management System

A comprehensive HR Management System built with ASP.NET Core MVC, featuring role-based authorization, JWT authentication, and complete HR operations management with both API endpoints and MVC views.

## Features

### 🔐 Authentication & Authorization
- JWT-based authentication with role-based access control
- Three user roles: Admin, HR, Employee
- Secure password hashing using BCrypt
- MVC-based login/registration pages

### 👥 Employee Management
- Complete CRUD operations for employees
- Supervisor-subordinate hierarchical relationships
- Department assignment and management
- Self-service profile updates for employees
- MVC views for employee management

### 🏢 Department Management
- Create, read, update, and delete departments
- Department-wise employee tracking
- Prevent deletion of departments with active employees
- Web interface for department management

### 📊 Attendance Tracking
- Check-in/check-out functionality
- Attendance records with timestamps
- Daily attendance reporting
- Both API and web interface

### 🏝️ Leave Management
- Leave request submission and approval
- Leave balance tracking
- Overlapping leave prevention
- Role-based leave approval (HR/Admin)
- Web interface for leave management

### 💰 Payroll Management
- Payroll generation and calculation
- Salary components: Basic, Bonus, Deductions, Net Salary
- Payroll history and records
- Web interface for payroll management

### 🎉 Holiday Management
- Company holiday scheduling
- Bulk holiday upload functionality
- Holiday calendar view
- Web interface for holiday management

### 📈 Dashboard & Reporting
- Real-time dashboard with statistics
- Comprehensive reporting system
- System logs and audit trails
- Notifications system

## Technology Stack

- **Backend**: ASP.NET Core MVC (.NET 10.0)
- **Frontend**: Razor Views with Bootstrap 5
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT (JSON Web Tokens) with cookie authentication
- **Password Hashing**: BCrypt.Net-Next
- **Architecture**: MVC with Repository Pattern
- **Logging**: Custom request logging middleware

## Project Structure

The project follows a clean MVC architecture:

```
HR_Management/
├── Controllers/          # MVC and API controllers
│   ├── *Controller.cs    # MVC controllers with views
│   └── *ApiController.cs # API controllers for AJAX calls
├── Models/              # Entity models and DbContext
├── Views/               # Razor views for all pages
├── Services/            # Business logic services
├── Middleware/          # Custom middleware (Request logging)
├── Migrations/          # Entity Framework migrations
├── Properties/          # Project properties
├── wwwroot/             # Static files (CSS, JS, libs)
├── Program.cs           # Application startup
├── appsettings.json     # Configuration
└── README.md            # This file
```

## Setup Instructions

### Prerequisites
- .NET 10.0 SDK
- SQL Server
- Git

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/Sahor568/HR-Management-System.git
   cd HR-Management-System
   ```

2. Update database connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=HRManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```

3. Apply database migrations:
   ```bash
   dotnet ef database update
   ```

4. Run the application:
   ```bash
   dotnet run
   ```

5. The application will be available at `http://localhost:4000` (default)

### Initial Setup
1. Navigate to `/Home/Register` to register the first admin user
2. Use the admin credentials to create other users and setup the system
3. Access the dashboard at `/Home/Dashboard`

## Web Interface

The system provides a complete web interface with the following main sections:

- **Dashboard**: Overview of system statistics
- **Employees**: Manage employee records
- **Departments**: Department management
- **Attendance**: Track employee attendance
- **Leaves**: Leave request management
- **Payroll**: Salary and compensation management
- **Holidays**: Company holiday calendar
- **Reports**: Generate various reports
- **System Logs**: View audit trails
- **User Management**: Manage user accounts and roles

## API Endpoints

The system also provides RESTful API endpoints for programmatic access:

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

### Departments
- `GET /api/Departments` - Get all departments
- `GET /api/Departments/{id}` - Get department by ID
- `POST /api/Departments` - Create department (Admin/HR only)
- `PUT /api/Departments/{id}` - Update department (Admin/HR only)
- `DELETE /api/Departments/{id}` - Delete department (Admin only)

### Dashboard
- `GET /api/Dashboard/stats` - Get dashboard statistics

## Role-Based Access Control

### Admin
- Full system access
- Can manage all entities
- Can assign roles
- Can delete any record
- Access to system settings and logs

### HR
- Manage employees, departments, attendance, leaves, payroll, holidays
- Cannot delete departments with employees
- Cannot access system settings
- Can approve/reject leave requests

### Employee
- View own profile and attendance
- Apply for leaves
- Check-in/check-out
- View own payroll
- Cannot modify other employees' data

## Security Features

- JWT tokens with expiration for API access
- Cookie authentication for web interface
- Role-based authorization policies
- Password hashing with BCrypt
- Input validation and sanitization
- SQL injection prevention via Entity Framework
- Custom request logging middleware for audit trails
- CORS configuration for frontend integration

## Testing

### Web Interface
Access the web interface at `http://localhost:4000` and navigate through the menus.

### API Testing
Use tools like Postman or Swagger to test the API endpoints. Include the JWT token in the Authorization header:
```
Authorization: Bearer {your_jwt_token}
```

## Database Schema

The system uses the following main entities:
- **User**: Authentication and user information
- **Employee**: Employee details with supervisor relationship
- **Department**: Organizational departments
- **Attendance**: Daily attendance records
- **Leave**: Employee leave requests
- **Payroll**: Salary and compensation records
- **Holiday**: Company holidays
- **Notification**: System notifications
- **SystemLog**: Audit log for all system actions

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