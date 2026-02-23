import React, { useState } from 'react';
import { Outlet, NavLink, useNavigate, useLocation } from 'react-router-dom';
import { useSelector, useDispatch } from 'react-redux';
import {
  Home,
  Users,
  BookOpen,
  Calendar,
  DollarSign,
  FileText,
  ClipboardList,
  Award,
  Bell,
  Settings,
  LogOut,
  Menu,
  X,
  User,
  ChevronDown,
} from 'lucide-react';
import { logoutUser } from '../../store/slices/authSlice';
import toast from 'react-hot-toast';

const Layout = () => {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const location = useLocation();
  const { user } = useSelector((state) => state.auth);
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [profileDropdownOpen, setProfileDropdownOpen] = useState(false);

  const handleLogout = async () => {
    try {
      await dispatch(logoutUser()).unwrap();
      toast.success('Logged out successfully');
      navigate('/login');
    } catch (error) {
      toast.error('Logout failed');
    }
  };

  const navigation = {
    Admin: [
      { name: 'Dashboard', href: '/dashboard', icon: Home },
      { name: 'Students', href: '/students', icon: Users },
      { name: 'Courses', href: '/courses', icon: BookOpen },
      { name: 'Attendance', href: '/attendance', icon: ClipboardList },
      { name: 'Fees', href: '/fees', icon: DollarSign },
      { name: 'Enrollments', href: '/enrollments', icon: FileText },
      { name: 'Exams', href: '/exams', icon: Calendar },
      { name: 'Grades', href: '/grades', icon: Award },
      { name: 'Notices', href: '/notices', icon: Bell },
    ],
    Teacher: [
      { name: 'Dashboard', href: '/dashboard', icon: Home },
      { name: 'My Courses', href: '/courses', icon: BookOpen },
      { name: 'Attendance', href: '/attendance', icon: ClipboardList },
      { name: 'Exams', href: '/exams', icon: Calendar },
      { name: 'Grades', href: '/grades', icon: Award },
      { name: 'Notices', href: '/notices', icon: Bell },
    ],
    Student: [
      { name: 'Dashboard', href: '/dashboard', icon: Home },
      { name: 'My Courses', href: '/courses', icon: BookOpen },
      { name: 'Attendance', href: '/attendance', icon: ClipboardList },
      { name: 'Fees', href: '/fees', icon: DollarSign },
      { name: 'Enrollments', href: '/enrollments', icon: FileText },
      { name: 'Exams', href: '/exams', icon: Calendar },
      { name: 'Grades', href: '/grades', icon: Award },
      { name: 'Notices', href: '/notices', icon: Bell },
    ],
  };

  const userNavigation = [
    { name: 'Profile', href: '/profile', icon: User },
    { name: 'Settings', href: '/settings', icon: Settings },
  ];

  const currentNavigation = navigation[user?.Role] || [];

  return (
    <div className="min-h-screen bg-gray-50 flex">
      {/* Mobile sidebar overlay */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-40 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        >
          <div className="absolute inset-0 bg-gray-600 opacity-75" />
        </div>
      )}

      {/* Sidebar */}
      <div
        className={`
          fixed inset-y-0 left-0 z-50 w-64 bg-white shadow-lg transform transition-transform duration-300 ease-in-out
          lg:translate-x-0 lg:static lg:inset-0
          ${sidebarOpen ? 'translate-x-0' : '-translate-x-full'}
        `}
      >
        <div className="flex items-center justify-between h-16 px-6 border-b border-gray-200">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <div className="w-8 h-8 bg-primary-600 rounded-lg flex items-center justify-center">
                <span className="text-white font-bold text-sm">CMS</span>
              </div>
            </div>
            <span className="ml-3 text-xl font-semibold text-gray-900">
              CollegeMS
            </span>
          </div>
          <button
            onClick={() => setSidebarOpen(false)}
            className="lg:hidden"
          >
            <X className="w-6 h-6 text-gray-500 hover:text-gray-700" />
          </button>
        </div>

        <nav className="mt-6 px-3">
          <div className="space-y-1">
            {currentNavigation.map((item) => {
              const isActive = location.pathname === item.href;
              return (
                <NavLink
                  key={item.name}
                  to={item.href}
                  className={({ isActive }) =>
                    `sidebar-item ${isActive ? 'active' : ''}`
                  }
                  onClick={() => setSidebarOpen(false)}
                >
                  <item.icon className="w-5 h-5" />
                  <span className="flex-1">{item.name}</span>
                </NavLink>
              );
            })}
          </div>

          <div className="mt-8 pt-6 border-t border-gray-200">
            <div className="space-y-1">
              {userNavigation.map((item) => (
                <NavLink
                  key={item.name}
                  to={item.href}
                  className={({ isActive }) =>
                    `sidebar-item ${isActive ? 'active' : ''}`
                  }
                  onClick={() => setSidebarOpen(false)}
                >
                  <item.icon className="w-5 h-5" />
                  <span className="flex-1">{item.name}</span>
                </NavLink>
              ))}
            </div>
          </div>
        </nav>
      </div>

      {/* Main content */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Top header */}
        <header className="bg-white shadow-sm border-b border-gray-200">
          <div className="flex items-center justify-between px-4 sm:px-6 lg:px-8 h-16">
            <button
              onClick={() => setSidebarOpen(true)}
              className="lg:hidden"
            >
              <Menu className="w-6 h-6 text-gray-500 hover:text-gray-700" />
            </button>

            <div className="flex items-center space-x-4">
              {/* Notifications */}
              <button className="relative p-2 text-gray-500 hover:text-gray-700">
                <Bell className="w-6 h-6" />
                <span className="absolute top-1 right-1 w-2 h-2 bg-red-500 rounded-full"></span>
              </button>

              {/* Profile dropdown */}
              <div className="relative">
                <button
                  onClick={() => setProfileDropdownOpen(!profileDropdownOpen)}
                  className="flex items-center space-x-3 text-sm rounded-lg p-2 hover:bg-gray-100"
                >
                  <div className="w-8 h-8 bg-primary-600 rounded-full flex items-center justify-center">
                    <span className="text-white font-medium">
                      {user?.FirstName?.[0] || user?.Email?.[0] || 'U'}
                    </span>
                  </div>
                  <span className="hidden md:block font-medium text-gray-700">
                    {user?.FirstName} {user?.LastName}
                  </span>
                  <ChevronDown className="w-4 h-4 text-gray-500" />
                </button>

                {profileDropdownOpen && (
                  <div className="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-50">
                    <div className="px-4 py-2 border-b border-gray-200">
                      <p className="text-sm font-medium text-gray-900">
                        {user?.FirstName} {user?.LastName}
                      </p>
                      <p className="text-xs text-gray-500">{user?.Email}</p>
                      <p className="text-xs text-primary-600 font-medium">
                        {user?.Role}
                      </p>
                    </div>
                    <button
                      onClick={handleLogout}
                      className="w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-red-50 flex items-center space-x-2"
                    >
                      <LogOut className="w-4 h-4" />
                      <span>Logout</span>
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-auto">
          <div className="py-6">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
              <Outlet />
            </div>
          </div>
        </main>
      </div>
    </div>
  );
};

export default Layout;
