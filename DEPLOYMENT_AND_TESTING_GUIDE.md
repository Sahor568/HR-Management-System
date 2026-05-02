# HR Management System - Deployment & Testing Guide

## System Overview

This is a complete **ASP.NET Core MVC** HR Management System with:
- ✅ Backend: C# with Entity Framework Core + SQL Server
- ✅ Frontend: Razor Views with Bootstrap 5 + Custom CSS
- ✅ API: RESTful endpoints with JWT Authentication
- ✅ Database: SQL Server with Entity Framework migrations
- ✅ Authentication: JWT Bearer + Cookie-based sessions
- ✅ Authorization: Role-based access control (Admin, HR, Employee)

## Prerequisites

### Required Software
```
- .NET 10.0 SDK or higher
- SQL Server 2019 or higher (or LocalDB for development)
- Git
```

### Optional Tools
- SQL Server Management Studio (for database inspection)
- Postman or Insomnia (for API testing)
- Visual Studio 2022 or VS Code

## Installation & Setup

### Step 1: Clone the Repository
```bash
git clone https://github.com/Sahor568/HR-Management-System.git
cd HR-Management-System
```

### Step 2: Configure Database Connection

Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SaHor;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

**Options:**
- **LocalDB**: `Server=(localdb)\\mssqllocaldb;Database=HRManagementDB;Integrated Security=true;TrustServerCertificate=true;`
- **Remote Server**: `Server=your-server-ip;Database=HRManagementDB;User Id=sa;Password=YourPassword;TrustServerCertificate=true;`
- **SQL Server Container**: `Server=host.docker.internal;Database=HRManagementDB;User Id=sa;Password=YourPassword;TrustServerCertificate=true;`

### Step 3: Apply Database Migrations
```bash
dotnet ef database update
```

This will:
- Create the database if it doesn't exist
- Create all tables (Users, Employees, Departments, Leaves, Attendance, Payroll, Holidays, Notifications, Logs, Roles)
- Create indexes and relationships
- Seed initial admin user (admin@hrsystem.com / Admin@123)

### Step 4: Build the Application
```bash
dotnet build
```

### Step 5: Run the Application
```bash
dotnet run
```

The application will start at:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`

**Note**: Default port may vary. Check console output for actual URL.

## Initial Login

### Default Credentials
```
Admin User:
  Email: admin@hrsystem.com
  Password: Admin@123

HR User:
  Email: hr@hrsystem.com
  Password: Hr@123
```

### First-Time Setup
1. Login with admin credentials
2. Navigate to "User Management"
3. Create additional admin/HR users as needed
4. Create employees through "Employees" section
5. Configure departments in "Departments" section

## Frontend Views & Features

### Dashboard (`/Home/Dashboard`)
- **KPI Cards**: Total employees, present today, pending leaves, payroll, departments, average salary
- **Charts**: Attendance trends (7-day), leave status, department distribution
- **Activities**: Recent system activities and notifications
- **Quick Actions**: Role-based action buttons

### Employee Management (`/Employees`)
- View all employees
- Add new employees
- Edit employee details
- Delete employees (Admin only)
- Search and filter by name/department
- View employee profiles
- Supervisor assignment

### Leave Management (`/Leaves`)
- Submit leave requests
- View leave history
- Approve/reject leaves (HR/Admin)
- Check leave balance
- View pending requests

### Attendance (`/Attendances`)
- Daily check-in/check-out
- View attendance records
- Filter by date range
- Mark attendance for employees (HR/Admin)
- Attendance statistics

### Payroll (`/Payrolls`)
- Generate payroll
- View payroll records
- Approve/reject payroll (Admin/HR)
- View salary components (Basic, Bonus, Deductions, Net)
- Payroll history

### Holidays (`/Holidays`)
- Manage company holidays
- Bulk upload holidays
- View holiday calendar
- Set weekend patterns

### Departments (`/Departments`)
- Create/edit departments
- View employee count per department
- Department performance metrics

### User Management (`/Users`)
- Create users (Admin only)
- Assign roles
- View all users
- Edit user permissions
- Delete users

### Roles & Access Control (`/Roles`)
- View system roles (Admin, HR, Employee)
- Manage role permissions
- View users per role
- Role configuration

### System Logs (`/Logs`)
- View system activity logs
- Filter logs by date, user, action
- Log statistics and reports
- Audit trail

### Notifications (`/Notifications`)
- View all notifications
- Mark as read
- Delete notifications
- Real-time notification bell

### User Profile (`/Home/Profile`)
- View own profile
- Update personal information
- View assigned leaves
- View performance metrics

### Settings (`/Home/Settings`)
- Change password
- Update preferences
- Notification settings
- Account management

## API Endpoints Reference

### Authentication API
```
POST   /api/Login/login               - User login
POST   /api/Login/register            - Create user
GET    /api/Login/profile             - Get current user
POST   /api/Login/change-password     - Change password
```

### Dashboard API
```
GET    /api/Dashboard/stats                    - Dashboard statistics
GET    /api/Dashboard/employee-distribution    - Employees by department
GET    /api/Dashboard/attendance-trend         - Attendance trend chart
GET    /api/Dashboard/leave-summary            - Leave statistics
GET    /api/Dashboard/payroll-summary          - Payroll statistics
GET    /api/Dashboard/upcoming-holidays        - Next holidays
GET    /api/Dashboard/employee-performance     - Top performers
GET    /api/Dashboard/department-performance   - Department metrics
```

### Employees API
```
GET    /api/Employees                 - List all employees
GET    /api/Employees/{id}            - Get employee details
POST   /api/Employees                 - Create employee
PUT    /api/Employees/{id}            - Update employee
DELETE /api/Employees/{id}            - Delete employee
GET    /api/Employees/my-profile      - Get own employee profile
```

### Other APIs
- **Departments**: `/api/Departments` (CRUD)
- **Leaves**: `/api/Leaves` (CRUD + approve/reject)
- **Attendance**: `/api/Attendances` (check-in/out)
- **Payroll**: `/api/Payrolls` (CRUD + approve/reject)
- **Holidays**: `/api/Holidays` (CRUD + bulk upload)
- **Users**: `/api/Users` (CRUD)
- **Notifications**: `/api/Notifications` (CRUD)
- **Logs**: `/api/Logs` (read)

## Testing Procedures

### 1. Manual Testing Checklist

#### Authentication
- [ ] Login with valid credentials succeeds
- [ ] Login with invalid credentials fails with error message
- [ ] Token is stored in localStorage
- [ ] Redirect to dashboard on successful login
- [ ] Logout clears session and redirects to login
- [ ] Register new user works (for authorized users)

#### Dashboard
- [ ] All KPI cards display correct data
- [ ] Charts load and render properly
- [ ] Attendance trend shows last 7 days
- [ ] Leave summary displays all statuses
- [ ] Department distribution is accurate
- [ ] Recent activities feed populates

#### Employee Management
- [ ] List loads all employees with pagination
- [ ] Search filters employees by name/email
- [ ] Department filter works correctly
- [ ] Add employee modal opens and submits
- [ ] Edit employee updates data
- [ ] Delete employee removes from list
- [ ] View employee profile shows full details

#### Leave Management
- [ ] Leave list shows all requests
- [ ] Filter by status (Pending, Approved, Rejected)
- [ ] Approve leave updates status
- [ ] Reject leave with remarks
- [ ] Employee can submit leave request
- [ ] HR/Admin can view all leaves
- [ ] Leave balance is calculated correctly

#### Attendance
- [ ] Check-in records current time
- [ ] Check-out updates attendance record
- [ ] Cannot check-in twice same day
- [ ] Can check-out after check-in
- [ ] Attendance history displays correctly
- [ ] Filter by date range works

#### Payroll
- [ ] Generate payroll for month
- [ ] View payroll records
- [ ] Approve/reject payroll by Admin/HR
- [ ] View salary components (Basic, Bonus, Deductions, Net)
- [ ] Calculate correct net salary
- [ ] Payroll history shows past records

### 2. Automated Testing (for developers)

```bash
# Build and run tests
dotnet test

# Run specific test class
dotnet test --filter "TestClassName"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### 3. API Testing with Curl

```bash
# Login
curl -X POST http://localhost:5000/api/Login/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@hrsystem.com","password":"Admin@123"}'

# Get dashboard stats (requires token)
curl -X GET http://localhost:5000/api/Dashboard/stats \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Get all employees
curl -X GET http://localhost:5000/api/Employees \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 4. Postman Collection

Import the Postman collection from the `postman/` directory:
1. Open Postman
2. Click "Import"
3. Select `HR_Management_API.postman_collection.json`
4. Create environment variable `token` with JWT token value
5. Test each endpoint

## Performance Optimization

### Database
- Ensure indexes are created on frequently queried columns
- Use `async/await` for all database operations
- Implement query optimization for large datasets

### Frontend
- Enable browser caching for static files
- Minify CSS and JavaScript
- Implement pagination for large lists
- Lazy load images and components

### API
- Implement response caching where appropriate
- Use efficient pagination (skip/take)
- Compress API responses
- Implement request throttling

## Security Best Practices

### Authentication & Authorization
✅ JWT token-based API authentication
✅ Cookie-based MVC authentication
✅ Role-based access control (Admin, HR, Employee)
✅ Main admin protection (IsMainAdmin claim)

### Data Protection
✅ Password hashing with BCrypt
✅ SQL injection prevention (EF Core parameterized queries)
✅ XSS prevention (Razor view escaping)
✅ CORS properly configured
✅ HTTPS in production

### Audit Trail
✅ Custom middleware logs all requests
✅ System logs track all changes
✅ User actions are recorded
✅ Deletion history maintained

## Troubleshooting

### Common Issues

#### Database Connection Failed
**Error**: `Cannot connect to database`
**Solution**: 
- Check SQL Server is running
- Verify connection string in `appsettings.json`
- Ensure database exists or migrations will create it
- Check user permissions for SQL Server

#### Migration Failed
**Error**: `Pending migrations detected`
**Solution**: 
```bash
dotnet ef database update
```

#### Port Already in Use
**Error**: `Address already in use`
**Solution**: 
```bash
# Change port in Properties/launchSettings.json
# Or kill process using port
lsof -i :5000  # macOS/Linux
netstat -ano | findstr :5000  # Windows
```

#### Authentication Issues
**Error**: `401 Unauthorized` on API calls
**Solution**:
- Ensure JWT token is included in Authorization header
- Check token hasn't expired
- Verify token is valid (use jwt.io to decode)
- Clear localStorage and login again

#### Frontend Not Loading
**Error**: `404 Not Found` for views
**Solution**:
- Check controller names match route
- Verify view files exist in correct directory
- Check Layout reference in _ViewStart.cshtml
- Review appsettings.json configuration

## Production Deployment

### Pre-Deployment Checklist
- [ ] All unit tests pass
- [ ] Security audit completed
- [ ] Database backup configured
- [ ] Logging configured
- [ ] Error handling in place
- [ ] HTTPS enabled
- [ ] CORS properly configured for frontend domain

### Deployment Steps

1. **Prepare Environment**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Configure Production Settings**
   - Update `appsettings.Production.json`
   - Set production database connection string
   - Configure JWT secret key
   - Set Serilog logging levels

3. **Deploy to IIS (Windows)**
   - Copy publish folder to IIS directory
   - Create application pool (.NET CLR version: No Managed Code)
   - Create IIS website pointing to publish folder
   - Bind SSL certificate

4. **Deploy to Linux (Docker)**
   - Create Dockerfile
   - Build image: `docker build -t hr-system .`
   - Run container with database link

5. **Database Deployment**
   - Create production SQL Server instance
   - Run migrations: `dotnet ef database update`
   - Setup automated backups
   - Configure replication if needed

### Monitoring & Maintenance

- Monitor application logs
- Track database performance
- Set up health checks
- Configure alerts for errors
- Regular security updates
- Database maintenance tasks

## Performance Benchmarks

**Expected Performance:**
- Dashboard load time: < 2 seconds
- Employee list load (500+ employees): < 3 seconds
- API response time: < 500ms
- Database query time: < 200ms
- Page rendering: < 1 second

## Support & Documentation

- **API Documentation**: Generated automatically via Swagger/OpenAPI
- **Code Comments**: Detailed inline documentation
- **Architecture Guide**: See ARCHITECTURE.md
- **Database Schema**: Documented in migrations
- **Frontend Structure**: See FRONTEND_FIXES_APPLIED.md

## Version Information

- **.NET**: 10.0
- **ASP.NET Core**: 10.0
- **Entity Framework Core**: Latest compatible version
- **Bootstrap**: 5.x
- **jQuery**: Latest CDN version
- **Font Awesome**: 6.4.0

---

**Last Updated**: May 2, 2026
**Status**: ✅ PRODUCTION READY
