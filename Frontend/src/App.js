import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { useSelector } from 'react-redux';
import { Toaster } from 'react-hot-toast';

// Components
import Layout from './components/Layout/Layout';
import Login from './pages/Auth/Login';
import Register from './pages/Auth/Register';
import Dashboard from './pages/Dashboard/Dashboard';
import LoadingSpinner from './components/UI/LoadingSpinner';

// Role-based dashboards
import AdminDashboard from './pages/Dashboard/AdminDashboard';
import TeacherDashboard from './pages/Dashboard/TeacherDashboard';
import StudentDashboard from './pages/Dashboard/StudentDashboard';

// Auth components
import ProtectedRoute from './components/Auth/ProtectedRoute';
import PublicRoute from './components/Auth/PublicRoute';

// Pages
import Profile from './pages/Profile/Profile';
import Students from './pages/Students/Students';
import Courses from './pages/Courses/Courses';
import Attendance from './pages/Attendance/Attendance';
import Fees from './pages/Fees/Fees';
import Enrollments from './pages/Enrollments/Enrollments';
import Exams from './pages/Exams/Exams';
import Grades from './pages/Grades/Grades';
import Notices from './pages/Notices/Notices';
import Settings from './pages/Settings/Settings';

function App() {
  const { isAuthenticated, user, isLoading } = useSelector((state) => state.auth);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  return (
    <Router>
      <div className="App">
        <Routes>
          {/* Public Routes */}
          <Route
            path="/login"
            element={
              <PublicRoute>
                <Login />
              </PublicRoute>
            }
          />
          <Route
            path="/register"
            element={
              <PublicRoute>
                <Register />
              </PublicRoute>
            }
          />

          {/* Protected Routes */}
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <Layout />
              </ProtectedRoute>
            }
          >
            <Route index element={<Navigate to="/dashboard" replace />} />
            
            {/* Role-based Dashboard */}
            <Route path="dashboard" element={<Dashboard />}>
              {user?.role === 'Admin' && (
                <Route index element={<AdminDashboard />} />
              )}
              {user?.role === 'Teacher' && (
                <Route index element={<TeacherDashboard />} />
              )}
              {user?.role === 'Student' && (
                <Route index element={<StudentDashboard />} />
              )}
            </Route>

            {/* Common Routes for all authenticated users */}
            <Route path="profile" element={<Profile />} />
            <Route path="settings" element={<Settings />} />

            {/* Admin Routes */}
            {user?.role === 'Admin' && (
              <>
                <Route path="students" element={<Students />} />
                <Route path="courses" element={<Courses />} />
                <Route path="attendance" element={<Attendance />} />
                <Route path="fees" element={<Fees />} />
                <Route path="enrollments" element={<Enrollments />} />
                <Route path="exams" element={<Exams />} />
                <Route path="grades" element={<Grades />} />
                <Route path="notices" element={<Notices />} />
              </>
            )}

            {/* Teacher Routes */}
            {user?.role === 'Teacher' && (
              <>
                <Route path="courses" element={<Courses />} />
                <Route path="attendance" element={<Attendance />} />
                <Route path="exams" element={<Exams />} />
                <Route path="grades" element={<Grades />} />
                <Route path="notices" element={<Notices />} />
              </>
            )}

            {/* Student Routes */}
            {user?.role === 'Student' && (
              <>
                <Route path="courses" element={<Courses />} />
                <Route path="attendance" element={<Attendance />} />
                <Route path="fees" element={<Fees />} />
                <Route path="enrollments" element={<Enrollments />} />
                <Route path="exams" element={<Exams />} />
                <Route path="grades" element={<Grades />} />
                <Route path="notices" element={<Notices />} />
              </>
            )}
          </Route>

          {/* Catch all route */}
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </div>
    </Router>
  );
}

export default App;
