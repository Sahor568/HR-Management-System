// HR Management System - Frontend API Handling
// This file contains all frontend API interactions for the HR Management System

const API_BASE_URL = window.location.origin; // Will be http://localhost:4000

// Auth helper functions
function getAuthToken() {
    return localStorage.getItem('auth_token');
}

function getUserRole() {
    return localStorage.getItem('user_role') || '';
}

function getUserEmail() {
    return localStorage.getItem('user_email') || '';
}

function getUserId() {
    return localStorage.getItem('user_id') || '';
}

function getUserEmployeeId() {
    return localStorage.getItem('user_employeeId') || '';
}

function isAuthenticated() {
    return !!getAuthToken();
}

function isAdmin() {
    return getUserRole() === 'Admin';
}

function isHR() {
    return getUserRole() === 'HR';
}

function isEmployee() {
    return getUserRole() === 'Employee';
}

function isAdminOrHR() {
    return isAdmin() || isHR();
}

// Clear auth data and redirect to login
function logout() {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('user_email');
    localStorage.removeItem('user_role');
    localStorage.removeItem('user_id');
    localStorage.removeItem('user_employeeId');
    localStorage.removeItem('user_name');
    window.location.href = '/Home/Logout';
}

// Check auth on page load - redirect to login if not authenticated
function checkAuth() {
    const currentPath = window.location.pathname.toLowerCase();
    const publicPages = ['/home/login', '/home/register', '/home/index', '/home/privacy', '/home/hello'];
    
    // Allow public pages
    if (publicPages.some(p => currentPath === p || currentPath === '/')) {
        return true;
    }
    
    // Check if authenticated
    if (!isAuthenticated()) {
        window.location.href = '/Home/Login';
        return false;
    }
    
    return true;
}

// Utility function for making API requests with cookie-based authentication
async function apiRequest(endpoint, options = {}) {
    const url = `${API_BASE_URL}${endpoint}`;
    
    const headers = {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
    };
    
    const defaultOptions = {
        headers,
        credentials: 'include'  // Include cookies for authentication
    };
    
    // Merge headers properly
    const mergedOptions = { ...defaultOptions, ...options };
    if (options.headers) {
        mergedOptions.headers = { ...defaultOptions.headers, ...options.headers };
    }
    
    try {
        const response = await fetch(url, mergedOptions);
        
        // Handle 401 Unauthorized - redirect to login
        if (response.status === 401) {
            showNotification('Session Expired', 'Please login again.', 'danger');
            setTimeout(() => logout(), 1500);
            throw new Error('Unauthorized');
        }
        
        // Handle 403 Forbidden
        if (response.status === 403) {
            const errorData = await response.json().catch(() => ({ message: 'Access denied' }));
            showNotification('Access Denied', errorData.message || 'You do not have permission for this action.', 'danger');
            throw new Error(`Forbidden: ${errorData.message || 'Access denied'}`);
        }
        
        if (!response.ok) {
            const errorText = await response.text();
            if (!options.silent) {
                showNotification('Error', `Request failed (${response.status}): ${errorText.substring(0, 100)}`, 'danger');
            }
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
        // Only show generic network error notification if not already handled above
        // and not a special auth error (401/403 already showed their own notification)
        if (!options.silent && error.message !== 'Unauthorized' && !error.message.startsWith('Forbidden') && !error.message.startsWith('API Error')) {
            console.error('API Request failed:', error);
            showNotification('Error', 'Network error. Please check your connection and try again.', 'danger');
        }
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
    },
    
    async getUpcomingHolidays() {
        return await apiRequest('/api/Dashboard/upcoming-holidays');
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
    },
    
    async create(employeeData) {
        return await apiRequest('/api/Employees', {
            method: 'POST',
            body: JSON.stringify(employeeData)
        });
    },
    
    async update(id, employeeData) {
        return await apiRequest(`/api/Employees/${id}`, {
            method: 'PUT',
            body: JSON.stringify(employeeData)
        });
    },
    
    async delete(id) {
        return await apiRequest(`/api/Employees/${id}`, {
            method: 'DELETE'
        });
    },
    
    async getMyProfile() {
        return await apiRequest('/api/Employees/my-profile');
    }
};

// Departments API functions
const DepartmentsAPI = {
    async getAll() {
        return await apiRequest('/api/Departments');
    },
    
    async getById(id) {
        return await apiRequest(`/api/Departments/${id}`);
    },
    
    async create(departmentData) {
        return await apiRequest('/api/Departments', {
            method: 'POST',
            body: JSON.stringify(departmentData)
        });
    },
    
    async update(id, departmentData) {
        return await apiRequest(`/api/Departments/${id}`, {
            method: 'PUT',
            body: JSON.stringify(departmentData)
        });
    },
    
    async delete(id) {
        return await apiRequest(`/api/Departments/${id}`, {
            method: 'DELETE'
        });
    }
};

// Leaves API functions
const LeavesAPI = {
    async getAll() {
        return await apiRequest('/api/Leaves');
    },
    
    async getByEmployee(employeeId) {
        return await apiRequest(`/api/Leaves/employee/${employeeId}`);
    },
    
    async getMyLeaves() {
        return await apiRequest('/api/Leaves/my-leaves');
    },
    
    async create(leaveData) {
        return await apiRequest('/api/Leaves', {
            method: 'POST',
            body: JSON.stringify(leaveData)
        });
    },
    
    async approve(id) {
        return await apiRequest(`/api/Leaves/${id}/approve`, {
            method: 'PUT'
        });
    },
    
    async reject(id) {
        return await apiRequest(`/api/Leaves/${id}/reject`, {
            method: 'PUT'
        });
    }
};

// Attendance API functions
const AttendanceAPI = {
    async getAll() {
        return await apiRequest('/api/Attendances');
    },
    
    async getByEmployee(employeeId) {
        return await apiRequest(`/api/Attendances/employee/${employeeId}`);
    },
    
    async checkIn(data) {
        return await apiRequest('/api/Attendances/check-in', {
            method: 'POST',
            body: JSON.stringify(data)
        });
    },
    
    async checkOut(id) {
        return await apiRequest(`/api/Attendances/check-out/${id}`, {
            method: 'POST'
        });
    }
};

// Payroll API functions
const PayrollAPI = {
    async getAll() {
        return await apiRequest('/api/Payrolls');
    },
    
    async getByEmployee(employeeId) {
        return await apiRequest(`/api/Payrolls/employee/${employeeId}`);
    },
    
    async create(payrollData) {
        return await apiRequest('/api/Payrolls', {
            method: 'POST',
            body: JSON.stringify(payrollData)
        });
    },
    
    async update(id, payrollData) {
        return await apiRequest(`/api/Payrolls/${id}`, {
            method: 'PUT',
            body: JSON.stringify(payrollData)
        });
    },
    
    async approve(id, remarks = '') {
        return await apiRequest(`/api/Payrolls/${id}/approve`, {
            method: 'PUT',
            body: JSON.stringify({ remarks })
        });
    },
    
    async reject(id, remarks = '') {
        return await apiRequest(`/api/Payrolls/${id}/reject`, {
            method: 'PUT',
            body: JSON.stringify({ remarks })
        });
    },
    
    async generate(data) {
        return await apiRequest('/api/Payrolls/generate', {
            method: 'POST',
            body: JSON.stringify(data)
        });
    }
};

// Holidays API functions
const HolidaysAPI = {
    async getAll() {
        return await apiRequest('/api/Holidays');
    },
    
    async create(holidayData) {
        return await apiRequest('/api/Holidays', {
            method: 'POST',
            body: JSON.stringify(holidayData)
        });
    },
    
    async update(id, holidayData) {
        return await apiRequest(`/api/Holidays/${id}`, {
            method: 'PUT',
            body: JSON.stringify(holidayData)
        });
    },
    
    async delete(id) {
        return await apiRequest(`/api/Holidays/${id}`, {
            method: 'DELETE'
        });
    },
    
    async bulkCreate(holidays) {
        return await apiRequest('/api/Holidays/bulk', {
            method: 'POST',
            body: JSON.stringify(holidays)
        });
    }
};

// Notifications API functions
const NotificationsAPI = {
    async getAll() {
        return await apiRequest('/api/Notifications');
    },
    
    async getUnreadCount() {
        return await apiRequest('/api/Notifications/unread-count');
    },
    
    async markRead(id) {
        return await apiRequest(`/api/Notifications/${id}/read`, {
            method: 'PUT'
        });
    },
    
    async markAllRead() {
        return await apiRequest('/api/Notifications/mark-all-read', {
            method: 'PUT'
        });
    },
    
    async delete(id) {
        return await apiRequest(`/api/Notifications/${id}`, {
            method: 'DELETE'
        });
    },
    
    async create(notificationData) {
        return await apiRequest('/api/Notifications', {
            method: 'POST',
            body: JSON.stringify(notificationData)
        });
    }
};

// Roles API functions
const RolesAPI = {
    async getAll() {
        return await apiRequest('/api/Roles');
    },
    
    async getSystemRoles() {
        return await apiRequest('/api/Roles/system-roles');
    },
    
    async getWithUserCount() {
        return await apiRequest('/api/Roles/with-user-count');
    },
    
    async getMetadata() {
        return await apiRequest('/api/Roles/metadata');
    },
    
    async create(roleData) {
        return await apiRequest('/api/Roles', {
            method: 'POST',
            body: JSON.stringify(roleData)
        });
    },
    
    async update(id, roleData) {
        return await apiRequest(`/api/Roles/${id}`, {
            method: 'PUT',
            body: JSON.stringify(roleData)
        });
    },
    
    async delete(id) {
        return await apiRequest(`/api/Roles/${id}`, {
            method: 'DELETE'
        });
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

// Supervisor API functions
const SupervisorAPI = {
    async getHierarchy() {
        return await apiRequest('/api/Supervisor/hierarchy');
    },
    
    async getMyChain() {
        return await apiRequest('/api/Supervisor/my-chain', { silent: true });
    },
    
    async getSubordinates(supervisorId) {
        return await apiRequest(`/api/Supervisor/${supervisorId}/subordinates`);
    },
    
    async getSupervisors() {
        return await apiRequest('/api/Supervisor/supervisors');
    }
};

// Authentication functions
const AuthAPI = {
    async login(email, password) {
        return await apiRequest('/api/Login/login', {
            method: 'POST',
            body: JSON.stringify({ email, password })
        });
    },
    
    async register(userData) {
        return await apiRequest('/api/Login/register', {
            method: 'POST',
            body: JSON.stringify(userData)
        });
    },
    
    async logout() {
        localStorage.removeItem('auth_token');
        localStorage.removeItem('user_email');
        localStorage.removeItem('user_role');
        localStorage.removeItem('user_id');
        localStorage.removeItem('user_employeeId');
        localStorage.removeItem('user_name');
        window.location.href = '/Home/Logout';
    },
    
    async getCurrentUser() {
        return await apiRequest('/api/Users/current');
    },
    
    async changePassword(currentPassword, newPassword) {
        return await apiRequest('/api/Login/change-password', {
            method: 'POST',
            body: JSON.stringify({ oldPassword: currentPassword, newPassword: newPassword })
        });
    },
    
    async getProfile() {
        return await apiRequest('/api/Login/profile');
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
        element.innerHTML = '<div class="loading-spinner" style="text-align:center;padding:2rem;"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div></div>';
    }
}

function formatDate(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

function formatCurrency(amount) {
    if (amount === null || amount === undefined) return '$0';
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }).format(amount);
}

function formatTime(timeString) {
    if (!timeString) return 'N/A';
    return timeString;
}

function getInitials(name) {
    if (!name) return '??';
    return name.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2);
}

function getStatusBadgeClass(status) {
    switch ((status || '').toLowerCase()) {
        case 'approved':
        case 'present':
        case 'paid':
            return 'badge-success';
        case 'pending':
        case 'late':
            return 'badge-warning';
        case 'rejected':
        case 'absent':
            return 'badge-danger';
        default:
            return 'badge-info';
    }
}

// Update sidebar visibility based on role
function updateSidebarVisibility() {
    const role = getUserRole();
    if (!role) return;
    
    // Hide all role-restricted items first
    document.querySelectorAll('[data-role]').forEach(el => {
        const allowedRoles = el.getAttribute('data-role').split(',');
        if (allowedRoles.includes(role)) {
            el.style.display = '';
        } else {
            el.style.display = 'none';
        }
    });
    
    // Update user info in topbar
    const userName = localStorage.getItem('user_name') || getUserEmail();
    const userAvatarEl = document.getElementById('user-avatar-initial');
    if (userAvatarEl) {
        userAvatarEl.textContent = userName.charAt(0).toUpperCase();
    }
    
    const userNameEl = document.getElementById('user-display-name');
    if (userNameEl) {
        userNameEl.textContent = userName;
    }
    
    const userRoleEl = document.getElementById('user-display-role');
    if (userRoleEl) {
        userRoleEl.textContent = role;
    }
}

// Update notification count in topbar
async function updateNotificationCount() {
    try {
        const data = await NotificationsAPI.getUnreadCount();
        const countEl = document.getElementById('notification-count');
        if (countEl) {
            const count = data.count || data || 0;
            countEl.textContent = count;
            countEl.style.display = count > 0 ? 'inline' : 'none';
        }
    } catch (e) {
        // Silently fail
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    console.log('HR Management System frontend initialized');
    
    // Check authentication
    checkAuth();
    
    // Update sidebar visibility
    updateSidebarVisibility();
    
    // Update notification count
    if (isAuthenticated()) {
        updateNotificationCount();
    }
    
    // Add notification container
    createNotificationContainer();
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
        Holidays: HolidaysAPI,
        Notifications: NotificationsAPI,
        Roles: RolesAPI,
        Logs: LogsAPI,
        Supervisor: SupervisorAPI,
        Auth: AuthAPI
    },
    Utils: {
        showNotification,
        formatDate,
        formatCurrency,
        formatTime,
        getInitials,
        getStatusBadgeClass,
        showLoading
    },
    Auth: {
        getAuthToken,
        getUserRole,
        getUserEmail,
        getUserId,
        isAuthenticated,
        isAdmin,
        isHR,
        isEmployee,
        isAdminOrHR,
        logout,
        checkAuth
    }
};
