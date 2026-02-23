/**
 * ========================================
 * 🔌 API Service Layer
 * College Management System - Frontend
 * ========================================
 * 
 * Handles all communication with the backend API Gateway
 */

const API_CONFIG = {
    // Change this to match your API Gateway URL
    BASE_URL: 'https://localhost:7000/api',
    TIMEOUT: 30000, // 30 seconds
    RETRY_ATTEMPTS: 3,
    RETRY_DELAY: 1000, // 1 second
};

/**
 * Main API Service Class
 */
class APIService {
    constructor(baseUrl = API_CONFIG.BASE_URL) {
        this.baseUrl = baseUrl;
    }

    /**
     * Generic fetch wrapper with error handling and retries
     */
    async fetch(endpoint, options = {}, retryCount = 0) {
        // Convert endpoint to lowercase for Ocelot gateway routing
        const lowerEndpoint = endpoint.toLowerCase();
        const url = `${this.baseUrl}${lowerEndpoint}`;

        // Auto-include JWT token from localStorage
        const token = localStorage.getItem('accessToken');
        const authHeaders = token ? { 'Authorization': `Bearer ${token}` } : {};

        const defaultOptions = {
            headers: {
                'Content-Type': 'application/json',
                ...authHeaders,
                ...options.headers,
            },
        };

        const finalOptions = { ...defaultOptions, ...options };

        try {
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), API_CONFIG.TIMEOUT);

            const response = await fetch(url, {
                ...finalOptions,
                signal: controller.signal,
            });

            clearTimeout(timeoutId);

            // If 401 Unauthorized, redirect to login
            if (response.status === 401) {
                console.warn('API returned 401 - redirecting to login');
                localStorage.removeItem('accessToken');
                localStorage.removeItem('refreshToken');
                localStorage.removeItem('userInfo');
                window.location.href = (window.location.pathname.includes('/pages/') ? '../' : './') + 'pages/login.html';
                return;
            }

            // Handle different HTTP status codes
            if (!response.ok) {
                const error = await this.handleError(response);
                throw error;
            }

            // Handle 204 No Content
            if (response.status === 204) {
                return null;
            }

            // Parse JSON response
            const text = await response.text();
            if (!text) return null;
            const data = JSON.parse(text);

            // Handle wrapped responses {"data": [...]} from backend
            if (data && typeof data === 'object' && 'data' in data) {
                return data.data;
            }

            return data;

        } catch (error) {
            // Retry logic for network errors (but not auth errors)
            if (retryCount < API_CONFIG.RETRY_ATTEMPTS && this.shouldRetry(error)) {
                console.log(`Retrying request... Attempt ${retryCount + 1}`);
                await this.delay(API_CONFIG.RETRY_DELAY);
                return this.fetch(endpoint, options, retryCount + 1);
            }

            throw error;
        }
    }

    /**
     * Handle API errors
     */
    async handleError(response) {
        let errorMessage = 'An error occurred';

        try {
            const errorData = await response.json();
            errorMessage = errorData.message || errorData.title || errorMessage;
        } catch (e) {
            errorMessage = response.statusText || errorMessage;
        }

        const error = new Error(errorMessage);
        error.status = response.status;
        error.statusText = response.statusText;

        return error;
    }

    /**
     * Determine if request should be retried
     */
    shouldRetry(error) {
        // Retry on network errors or 5xx server errors
        return error.name === 'AbortError' ||
            (error.status && error.status >= 500);
    }

    /**
     * Delay utility for retries
     */
    delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    // ========================================
    // Student Service APIs
    // ========================================

    /**
     * Get all students
     */
    async getStudents() {
        return await this.fetch('/Student');
    }

    /**
     * Get student by ID
     */
    async getStudentById(id) {
        return await this.fetch(`/Student/${id}`);
    }

    /**
     * Create new student
     */
    async createStudent(studentData) {
        return await this.fetch('/Student', {
            method: 'POST',
            body: JSON.stringify(studentData),
        });
    }

    /**
     * Update student
     */
    async updateStudent(id, studentData) {
        return await this.fetch(`/Student/${id}`, {
            method: 'PUT',
            body: JSON.stringify(studentData),
        });
    }

    /**
     * Delete student
     */
    async deleteStudent(id) {
        return await this.fetch(`/Student/${id}`, {
            method: 'DELETE',
        });
    }

    // ========================================
    // Course Service APIs
    // ========================================

    /**
     * Get all courses
     */
    async getCourses() {
        return await this.fetch('/Course');
    }

    /**
     * Get course by ID
     */
    async getCourseById(id) {
        return await this.fetch(`/Course/${id}`);
    }

    /**
     * Create new course
     */
    async createCourse(courseData) {
        return await this.fetch('/Course', {
            method: 'POST',
            body: JSON.stringify(courseData),
        });
    }

    /**
     * Update course
     */
    async updateCourse(id, courseData) {
        return await this.fetch(`/Course/${id}`, {
            method: 'PUT',
            body: JSON.stringify(courseData),
        });
    }

    /**
     * Delete course
     */
    async deleteCourse(id) {
        return await this.fetch(`/Course/${id}`, {
            method: 'DELETE',
        });
    }

    // ========================================
    // Enrollment Service APIs
    // ========================================

    /**
     * Get all enrollments
     */
    async getEnrollments() {
        return await this.fetch('/Enrollment');
    }

    /**
     * Get enrollment by ID
     */
    async getEnrollmentById(id) {
        return await this.fetch(`/Enrollment/${id}`);
    }

    /**
     * Create new enrollment (triggers RabbitMQ event)
     */
    async createEnrollment(enrollmentData) {
        return await this.fetch('/Enrollment', {
            method: 'POST',
            body: JSON.stringify(enrollmentData),
        });
    }

    /**
     * Update enrollment
     */
    async updateEnrollment(id, enrollmentData) {
        return await this.fetch(`/Enrollment/${id}`, {
            method: 'PUT',
            body: JSON.stringify(enrollmentData),
        });
    }

    /**
     * Delete enrollment
     */
    async deleteEnrollment(id) {
        return await this.fetch(`/Enrollment/${id}`, {
            method: 'DELETE',
        });
    }

    // ========================================
    // Fee Service APIs
    // ========================================

    /**
     * Get all fees
     */
    async getFees() {
        return await this.fetch('/Fee');
    }

    /**
     * Get fee by ID
     */
    async getFeeById(id) {
        return await this.fetch(`/Fee/${id}`);
    }

    /**
     * Update fee (e.g., mark as paid)
     */
    async updateFee(id, feeData) {
        return await this.fetch(`/Fee/${id}`, {
            method: 'PUT',
            body: JSON.stringify(feeData),
        });
    }

    // ========================================
    // Attendance Service APIs
    // ========================================

    /**
     * Get all attendance records
     */
    async getAttendance() {
        return await this.fetch('/Attendance');
    }

    /**
     * Get attendance by ID
     */
    async getAttendanceById(id) {
        return await this.fetch(`/Attendance/${id}`);
    }

    /**
     * Create attendance record
     */
    async createAttendance(attendanceData) {
        return await this.fetch('/Attendance', {
            method: 'POST',
            body: JSON.stringify(attendanceData),
        });
    }

    /**
     * Bulk import attendance (CSV/Excel)
     */
    async bulkImportAttendance(formData) {
        return await this.fetch('/Attendance/bulk-import', {
            method: 'POST',
            headers: {}, // Remove Content-Type for FormData
            body: formData,
        });
    }

    // ========================================
    // AI Assistant Service APIs
    // ========================================

    /**
     * Send chat message to AI
     */
    async sendChatMessage(userId, message) {
        return await this.fetch('/Chat', {
            method: 'POST',
            body: JSON.stringify({ userId, message }),
        });
    }

    /**
     * Get chat history
     */
    async getChatHistory(userId) {
        return await this.fetch(`/Chat/history/${userId}`);
    }

    /**
     * Clear chat history
     */
    async clearChatHistory(userId) {
        return await this.fetch(`/Chat/history/${userId}`, {
            method: 'DELETE',
        });
    }

    /**
     * Confirm a pending action
     */
    async confirmAction(actionId, userId) {
        return await this.fetch(`/Chat/confirm/${actionId}`, {
            method: 'POST',
            body: JSON.stringify({ userId, confirmed: true }),
        });
    }

    /**
     * Cancel a pending action
     */
    async cancelAction(actionId) {
        return await this.fetch(`/Chat/cancel/${actionId}`, {
            method: 'POST',
        });
    }

    // ========================================
    // Statistics & Dashboard APIs
    // ========================================

    /**
     * Get dashboard statistics
     */
    async getDashboardStats() {
        try {
            const [students, courses, enrollments, fees] = await Promise.all([
                this.getStudents(),
                this.getCourses(),
                this.getEnrollments(),
                this.getFees(),
            ]);

            return {
                totalStudents: students.length || 0,
                totalCourses: courses.length || 0,
                totalEnrollments: enrollments.length || 0,
                totalFees: fees.reduce((sum, fee) => sum + (fee.amount || 0), 0),
                pendingFees: fees.filter(fee => fee.status === 'Pending').length,
            };
        } catch (error) {
            console.error('Error fetching dashboard stats:', error);
            return {
                totalStudents: 0,
                totalCourses: 0,
                totalEnrollments: 0,
                totalFees: 0,
                pendingFees: 0,
            };
        }
    }
}

// Create singleton instance
const api = new APIService();

// Export for use in other files
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { api, APIService };
}
