// HR Management System - Frontend API Handling
// This file contains all frontend API interactions for the HR Management System

const API_BASE_URL = window.location.origin; // Will be http://localhost:4000

// Utility function for making API requests
async function apiRequest(endpoint, options = {}) {
    const url = `${API_BASE_URL}${endpoint}`;
    
    const defaultOptions = {
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        },
        credentials: 'include' // Include cookies for authentication
    };
    
    const mergedOptions = { ...defaultOptions, ...options };
    
    try {
        const response = await fetch(url, mergedOptions);
        
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`API Error ${response.status}: ${errorText}`);
        }
        
        // Check if response has content
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            return await response.json();
        } else {
            return await response.text();
        }
    } catch (error) {
        console.error('API Request failed:', error);
        showNotification('Error', 'Failed to fetch data from server. Please try again.', 'danger');
        throw error;
    }
}

// Dashboard API functions
const DashboardAPI = {
    async getStats() {
        return await apiRequest('/api/Dashboard/stats');
    },
    
    async getEmployeeDistribution() {
        return await apiRequest('/api/Dashboard/employee-distribution');
    },
    
    async getAttendanceTrend(days = 30) {
        return await apiRequest(`/api/Dashboard/attendance-trend?days=${days}`);
    },
    
    async getLeaveSummary(month = 0, year = 0) {
        return await apiRequest(`/api/Dashboard/leave-summary?month=${month}&year=${year}`);
    },
    
    async getPayrollSummary(month = 0, year = 0) {
        return await apiRequest(`/api/Dashboard/payroll-summary?month=${month}&year=${year}`);
    }
};

// Users API functions
const UsersAPI = {
    async getAll() {
        return await apiRequest('/api/Users');
    },
    
    async getById(id) {
        return await apiRequest(`/api/Users/${id}`);
    },
    
    async create(userData) {
        return await apiRequest('/api/Users', {
            method: 'POST',
            body: JSON.stringify(userData)
        });
    },
    
    async update(id, userData) {
        return await apiRequest(`/api/Users/${id}`, {
            method: 'PUT',
            body: JSON.stringify(userData)
        });
    },
    
    async delete(id) {
        return await apiRequest(`/api/Users/${id}`, {
            method: 'DELETE'
        });
    }
};

// Employees API functions
const EmployeesAPI = {
    async getAll() {
        return await apiRequest('/api/Employees');
    },
    
    async getById(id) {
        return await apiRequest(`/api/Employees/${id}`);
    }
};

// Departments API functions
const DepartmentsAPI = {
    async getAll() {
        return await apiRequest('/api/Departments');
    }
};

// Leaves API functions
const LeavesAPI = {
    async getAll() {
        return await apiRequest('/api/Leaves');
    }
};

// Attendance API functions
const AttendanceAPI = {
    async getAll() {
        return await apiRequest('/api/Attendances');
    }
};

// Payroll API functions
const PayrollAPI = {
    async getAll() {
        return await apiRequest('/api/Payrolls');
    }
};

// Logs API functions
const LogsAPI = {
    async getAll() {
        return await apiRequest('/api/Logs');
    },
    
    async getStatistics() {
        return await apiRequest('/api/Logs/GetStatistics');
    }
};

// Authentication functions
const AuthAPI = {
    async login(email, password) {
        return await apiRequest('/api/Login', {
            method: 'POST',
            body: JSON.stringify({ email, password })
        });
    },
    
    async logout() {
        return await apiRequest('/api/Logout', {
            method: 'POST'
        });
    },
    
    async getCurrentUser() {
        return await apiRequest('/api/Users/current');
    }
};

// UI Helper functions
function showNotification(title, message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <div class="notification-header">
            <strong>${title}</strong>
            <button class="notification-close">&times;</button>
        </div>
        <div class="notification-body">${message}</div>
    `;
    
    // Add to page
    const container = document.getElementById('notification-container') || createNotificationContainer();
    container.appendChild(notification);
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        if (notification.parentNode) {
            notification.style.opacity = '0';
            setTimeout(() => notification.remove(), 300);
        }
    }, 5000);
    
    // Close button handler
    notification.querySelector('.notification-close').addEventListener('click', () => {
        notification.remove();
    });
}

function createNotificationContainer() {
    const container = document.createElement('div');
    container.id = 'notification-container';
    container.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
        max-width: 350px;
    `;
    document.body.appendChild(container);
    return container;
}

function showLoading(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.innerHTML = '<div class="loading-spinner"><div></div><div></div><div></div><div></div></div>';
    }
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

function formatCurrency(amount) {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD'
    }).format(amount);
}

// Dashboard initialization
async function initializeDashboard() {
    const dashboardElement = document.getElementById('dashboard-stats');
    if (!dashboardElement) return;
    
    try {
        // Show loading state
        showLoading('dashboard-stats');
        
        // Fetch dashboard stats
        const stats = await DashboardAPI.getStats();
        
        // Update the dashboard with real data
        updateDashboardStats(stats);
        
        // Load chart data if charts exist
        loadCharts();
        
    } catch (error) {
        console.error('Failed to initialize dashboard:', error);
        showNotification('Dashboard Error', 'Could not load dashboard data. Using sample data.', 'warning');
        // You could fall back to the hardcoded values here
    }
}

function updateDashboardStats(stats) {
    // Update Total Employees
    const totalEmployeesEl = document.getElementById('total-employees');
    if (totalEmployeesEl && stats.totalEmployees !== undefined) {
        totalEmployeesEl.textContent = stats.totalEmployees;
    }
    
    // Update Present Today
    const presentTodayEl = document.getElementById('present-today');
    if (presentTodayEl && stats.presentToday !== undefined) {
        presentTodayEl.textContent = stats.presentToday;
        
        // Update attendance percentage
        const attendancePercent = stats.totalEmployees > 0
            ? Math.round((stats.presentToday / stats.totalEmployees) * 100)
            : 0;
        const attendanceText = document.getElementById('attendance-percent');
        if (attendanceText) {
            attendanceText.innerHTML = `<i class="fas fa-arrow-up"></i> ${attendancePercent}% attendance`;
        }
    }
    
    // Update Pending Leaves
    const pendingLeavesEl = document.getElementById('pending-leaves');
    if (pendingLeavesEl && stats.pendingLeaves !== undefined) {
        pendingLeavesEl.textContent = stats.pendingLeaves;
    }
    
    // Update Monthly Payroll
    const monthlyPayrollEl = document.getElementById('monthly-payroll');
    if (monthlyPayrollEl && stats.totalPayroll !== undefined) {
        monthlyPayrollEl.textContent = formatCurrency(stats.totalPayroll);
    }
    
    // Update Total Departments
    const totalDepartmentsEl = document.getElementById('total-departments');
    if (totalDepartmentsEl && stats.totalDepartments !== undefined) {
        totalDepartmentsEl.textContent = stats.totalDepartments;
    }
    
    // Calculate and update Average Salary
    const avgSalaryEl = document.getElementById('avg-salary');
    if (avgSalaryEl && stats.totalPayroll !== undefined && stats.totalEmployees !== undefined && stats.totalEmployees > 0) {
        const avgSalary = Math.round(stats.totalPayroll / stats.totalEmployees);
        avgSalaryEl.textContent = formatCurrency(avgSalary);
    }
}

function loadCharts() {
    // This is a placeholder for chart loading logic
    // You would integrate with a charting library like Chart.js here
    console.log('Charts would be loaded here');
}

// Data table initialization for listing pages
function initializeDataTable(tableId, apiFunction, columns) {
    const tableElement = document.getElementById(tableId);
    if (!tableElement) return;
    
    // Show loading
    const tbody = tableElement.querySelector('tbody');
    if (tbody) {
        tbody.innerHTML = '<tr><td colspan="' + columns.length + '" class="text-center">Loading data...</td></tr>';
    }
    
    // Fetch data
    apiFunction().then(data => {
        if (tbody && Array.isArray(data)) {
            tbody.innerHTML = '';
            data.forEach(item => {
                const row = document.createElement('tr');
                columns.forEach(col => {
                    const cell = document.createElement('td');
                    let value = item[col.field];
                    
                    // Apply formatters if specified
                    if (col.formatter) {
                        value = col.formatter(value, item);
                    }
                    
                    cell.innerHTML = value;
                    row.appendChild(cell);
                });
                tbody.appendChild(row);
            });
        }
    }).catch(error => {
        console.error('Failed to load table data:', error);
        if (tbody) {
            tbody.innerHTML = '<tr><td colspan="' + columns.length + '" class="text-center text-danger">Failed to load data</td></tr>';
        }
    });
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    console.log('HR Management System frontend initialized');
    
    // Initialize dashboard if on dashboard page
    if (document.getElementById('dashboard-stats')) {
        initializeDashboard();
    }
    
    // Initialize users table if on users page
    if (document.getElementById('users-table')) {
        initializeDataTable('users-table', UsersAPI.getAll, [
            { field: 'id', label: 'ID' },
            { field: 'email', label: 'Email' },
            { field: 'role', label: 'Role' }
        ]);
    }
    
    // Initialize employees table if on employees page
    if (document.getElementById('employees-table')) {
        initializeDataTable('employees-table', EmployeesAPI.getAll, [
            { field: 'id', label: 'ID' },
            { field: 'name', label: 'Name' },
            { field: 'department', label: 'Department' },
            { field: 'position', label: 'Position' }
        ]);
    }
    
    // Add notification container
    createNotificationContainer();
    
    // Handle login form if present
    const loginForm = document.getElementById('login-form');
    if (loginForm) {
        loginForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const email = document.getElementById('email').value;
            const password = document.getElementById('password').value;
            
            try {
                const result = await AuthAPI.login(email, password);
                if (result.success) {
                    window.location.href = '/Home/Dashboard';
                } else {
                    showNotification('Login Failed', result.message || 'Invalid credentials', 'danger');
                }
            } catch (error) {
                showNotification('Login Error', 'Failed to connect to server', 'danger');
            }
        });
    }
});

// Export APIs for use in browser console
window.HRManagement = {
    API: {
        Dashboard: DashboardAPI,
        Users: UsersAPI,
        Employees: EmployeesAPI,
        Departments: DepartmentsAPI,
        Leaves: LeavesAPI,
        Attendance: AttendanceAPI,
        Payroll: PayrollAPI,
        Logs: LogsAPI,
        Auth: AuthAPI
    },
    Utils: {
        showNotification,
        formatDate,
        formatCurrency
    }
};
