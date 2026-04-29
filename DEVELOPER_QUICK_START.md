# HR Management System - Developer Quick Start Guide

## Quick Navigation

### Admin Views
- **Dashboard** → `/api/dashboard/*` APIs
- **Users** → `/api/users/*` endpoints
- **Roles** → Role-based access control
- **Reports** → `/api/reports/*` generation
- **Logs** → `/api/logs/*` monitoring

### HR Views
- **Employees** → `/api/employees/*` CRUD
- **Departments** → `/api/departments/*` management
- **Leaves** → `/api/leaves/*` approval workflow
- **Attendances** → `/api/attendances/*` tracking
- **Payrolls** → `/api/payrolls/*` processing
- **Holidays** → `/api/holidays/*` calendar

### Employee Views
- **Dashboard** → Personal metrics
- **Attendance** → Check-in/check-out
- **Leaves** → Request management
- **Payroll** → Salary information

## File Structure

```
Views/
├── Shared/_Layout.cshtml ..................... Master layout (sidebar, topbar)
├── Home/
│   ├── Index.cshtml .......................... Login page
│   ├── Dashboard.cshtml ....................... Dashboard with KPIs
│   ├── Profile.cshtml ......................... User profile (create this)
│   └── Settings.cshtml ........................ User settings (create this)
├── Employees/Index.cshtml ..................... Employee CRUD (table/card views)
├── Attendances/Index.cshtml ................... Attendance tracking & check-in
├── Leaves/Index.cshtml ........................ Leave request management
├── Payrolls/Index.cshtml ...................... Payroll processing
├── Departments/Index.cshtml ................... Department management
├── Users/Index.cshtml ......................... User management
├── Roles/Index.cshtml ......................... Role & permissions
├── Holidays/Index.cshtml ...................... Holiday calendar
├── Reports/Index.cshtml ....................... Analytics & reports
└── Logs/Index.cshtml .......................... System logs
```

## CSS Architecture

All styling is embedded in views using inline `<style>` tags for easy customization.

### CSS Variables (in _Layout.cshtml)
```css
:root {
    --primary-color: #2563eb;
    --primary-dark: #1e40af;
    --secondary-color: #64748b;
    --success-color: #10b981;
    --danger-color: #ef4444;
    --warning-color: #f59e0b;
    --light-bg: #f8fafc;
    --white: #ffffff;
    --text-dark: #1e293b;
    --text-light: #64748b;
    --border-color: #e2e8f0;
}
```

### Common Classes
- `.page-header` - Page title section
- `.card` - Content container
- `.btn` - Buttons
- `.badge` - Status labels
- `.kpi-card` - KPI display card
- `.table` - Data tables
- `.nav-link` - Sidebar navigation

## Creating New Views

### 1. Basic View Template
```html
@{
    ViewData["Title"] = "Page Title";
}

<style>
    /* Page-specific styles */
</style>

<div class="page-header">
    <h1 class="page-title">Page Title</h1>
    <p class="page-subtitle">Description here</p>
</div>

<!-- Your content here -->

@section Scripts {
    <script>
        // Page-specific JavaScript
    </script>
}
```

### 2. Adding to Navigation
Edit `Views/Shared/_Layout.cshtml`:
```html
<li>
    <a class="nav-link" asp-controller="YourController" asp-action="Index">
        <i class="fas fa-icon-name"></i> Page Name
    </a>
</li>
```

### 3. API Integration Example
```csharp
// In your controller action
[Authorize(Policy = "AdminOrHR")]
public IActionResult Index()
{
    ViewData["Title"] = "Page Title";
    return View();
}
```

## Common Components

### KPI Card
```html
<div class="kpi-card">
    <div class="kpi-icon" style="background: linear-gradient(...); color: ...;">
        <i class="fas fa-icon"></i>
    </div>
    <h5 class="kpi-label">Label</h5>
    <div class="kpi-value">123</div>
</div>
```

### Badge/Status
```html
<span class="badge badge-success">Active</span>
<span class="badge badge-danger">Inactive</span>
<span class="badge badge-warning">Pending</span>
<span class="badge badge-info">Info</span>
```

### Table Structure
```html
<div class="card">
    <div class="card-body" style="padding: 0;">
        <table class="table">
            <thead>
                <tr>
                    <th>Column 1</th>
                    <th>Column 2</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>Value 1</td>
                    <td>Value 2</td>
                </tr>
            </tbody>
        </table>
    </div>
</div>
```

### Button Group
```html
<div style="display: flex; gap: 0.5rem;">
    <a href="#" class="btn btn-primary">Primary</a>
    <button class="btn btn-outline-primary">Outline</button>
    <button class="btn btn-danger">Danger</button>
</div>
```

## JavaScript Integration

### Add Active Navigation Link
```javascript
const currentUrl = window.location.pathname;
document.querySelectorAll('.nav-link').forEach(link => {
    if (link.href.includes(currentUrl)) {
        link.classList.add('active');
    }
});
```

### Form Submission with API
```javascript
document.getElementById('myForm').addEventListener('submit', async function(e) {
    e.preventDefault();
    
    const formData = new FormData(this);
    const response = await fetch('/api/endpoint', {
        method: 'POST',
        headers: {
            'Authorization': 'Bearer ' + token,
        },
        body: JSON.stringify(Object.fromEntries(formData))
    });
    
    if (response.ok) {
        // Handle success
        console.log('Success');
    }
});
```

## Controller Actions Required

```csharp
// HomeController
public IActionResult Dashboard() { }
public IActionResult Profile() { }
public IActionResult Settings() { }
public IActionResult Logout() { }

// To be created if needed
// EmployeesController - already exists in API
// AttendancesController - already exists in API
// LeavesController - already exists in API
// PayrollsController - already exists in API
// DepartmentsController - already exists in API
// UsersController - already exists in API
// RolesController - already exists in API
// HolidaysController - already exists in API
// ReportsController - already exists in API
// LogsController - already exists in API
```

## Icon Library

Using FontAwesome 6.4.0:
- `<i class="fas fa-users"></i>` - Users
- `<i class="fas fa-chart-line"></i>` - Charts
- `<i class="fas fa-calendar"></i>` - Calendar
- `<i class="fas fa-money-bill"></i>` - Money
- `<i class="fas fa-clock"></i>` - Time
- `<i class="fas fa-check"></i>` - Check
- `<i class="fas fa-times"></i>` - Close
- `<i class="fas fa-edit"></i>` - Edit
- `<i class="fas fa-trash"></i>` - Delete
- `<i class="fas fa-eye"></i>` - View
- `<i class="fas fa-download"></i>` - Download
- `<i class="fas fa-print"></i>` - Print

[Full FontAwesome List](https://fontawesome.com/icons)

## Responsive Design

### Mobile Breakpoints (Bootstrap 5)
```css
/* Extra small devices (phones) */
@media (max-width: 575.98px) { }

/* Small devices (landscape phones) */
@media (min-width: 576px) { }

/* Medium devices (tablets) */
@media (min-width: 768px) { }

/* Large devices (desktops) */
@media (min-width: 992px) { }

/* Extra large devices (large desktops) */
@media (min-width: 1200px) { }
```

## Best Practices

1. **Always use ViewData["Title"]** for page titles
2. **Use semantic HTML** (`<header>`, `<main>`, `<footer>`)
3. **Add ARIA labels** for accessibility
4. **Use Bootstrap grid** for responsive layouts
5. **Inline styles** for view-specific customization
6. **JavaScript in scripts sections** not inline
7. **Use Font Awesome icons** consistently
8. **Follow color palette** defined in CSS variables
9. **Test on mobile** before deployment
10. **Use form validation** on client and server

## Common Issues & Solutions

### Issue: Styles not applying
**Solution**: Check CSS variable names, ensure Bootstrap is loaded, clear browser cache

### Issue: API calls failing
**Solution**: Check authorization header, verify JWT token, check CORS settings

### Issue: Navigation not working
**Solution**: Verify controller/action names, check routing configuration

### Issue: Responsive layout broken
**Solution**: Check viewport meta tag, test with Bootstrap breakpoints

## Performance Tips

1. Minimize inline styles - move to external CSS
2. Use CDN for external libraries
3. Lazy load images
4. Compress images
5. Minimize JavaScript
6. Use CSS Grid/Flexbox instead of floats
7. Cache API responses
8. Use pagination for large datasets

## Deployment

1. Build the solution: `dotnet build`
2. Publish: `dotnet publish -c Release`
3. Set production environment
4. Enable HTTPS
5. Configure database
6. Run migrations
7. Test thoroughly

## Useful Links

- [ASP.NET MVC Docs](https://docs.microsoft.com/aspnet)
- [Bootstrap 5 Docs](https://getbootstrap.com/docs)
- [FontAwesome Icons](https://fontawesome.com)
- [Razor View Engine](https://docs.microsoft.com/aspnet/core/mvc/views/razor)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)

---

**Last Updated**: April 2026
**Version**: 1.0
