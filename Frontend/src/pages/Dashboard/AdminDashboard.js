import React, { useState, useEffect } from 'react';
import { useSelector } from 'react-redux';
import {
  Users,
  BookOpen,
  DollarSign,
  TrendingUp,
  Calendar,
  Award,
  Clock,
  CheckCircle,
  AlertCircle,
  UserCheck,
  UserX,
  Plus,
  Eye,
} from 'lucide-react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  LineChart,
  Line,
  Legend,
} from 'recharts';

const AdminDashboard = () => {
  const { user } = useSelector((state) => state.auth);
  const [stats, setStats] = useState({
    totalStudents: 1250,
    totalTeachers: 85,
    totalCourses: 45,
    totalRevenue: 2500000,
    monthlyRevenue: 450000,
    attendanceRate: 92,
    passRate: 88,
    pendingApprovals: 12,
  });

  const [recentActivities] = useState([
    {
      id: 1,
      type: 'enrollment',
      message: 'John Doe enrolled in Computer Science 101',
      time: '2 hours ago',
      icon: Plus,
      color: 'text-green-600',
    },
    {
      id: 2,
      type: 'payment',
      message: 'Jane Smith paid tuition fee - $5,000',
      time: '4 hours ago',
      icon: DollarSign,
      color: 'text-blue-600',
    },
    {
      id: 3,
      type: 'attendance',
      message: 'Mathematics class attendance updated',
      time: '6 hours ago',
      icon: CheckCircle,
      color: 'text-green-600',
    },
    {
      id: 4,
      type: 'alert',
      message: '5 students have low attendance (<75%)',
      time: '1 day ago',
      icon: AlertCircle,
      color: 'text-red-600',
    },
  ]);

  const [upcomingEvents] = useState([
    {
      id: 1,
      title: 'Mid-term Examinations',
      date: '2024-03-15',
      type: 'exam',
      icon: Calendar,
    },
    {
      id: 2,
      title: 'Faculty Meeting',
      date: '2024-03-10',
      type: 'meeting',
      icon: Users,
    },
    {
      id: 3,
      title: 'Graduation Ceremony',
      date: '2024-04-20',
      type: 'event',
      icon: Award,
    },
  ]);

  // Sample data for charts
  const enrollmentData = [
    { month: 'Jan', students: 1100, teachers: 80 },
    { month: 'Feb', students: 1150, teachers: 82 },
    { month: 'Mar', students: 1200, teachers: 83 },
    { month: 'Apr', students: 1220, teachers: 84 },
    { month: 'May', students: 1250, teachers: 85 },
  ];

  const departmentData = [
    { name: 'Computer Science', value: 450, color: '#3b82f6' },
    { name: 'Engineering', value: 380, color: '#10b981' },
    { name: 'Business', value: 280, color: '#f59e0b' },
    { name: 'Arts', value: 140, color: '#ef4444' },
  ];

  const revenueData = [
    { month: 'Jan', revenue: 380000 },
    { month: 'Feb', revenue: 420000 },
    { month: 'Mar', revenue: 450000 },
    { month: 'Apr', revenue: 410000 },
    { month: 'May', revenue: 450000 },
  ];

  const StatCard = ({ title, value, icon: Icon, change, changeType, color }) => (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm font-medium text-gray-600">{title}</p>
          <p className="text-2xl font-bold text-gray-900 mt-1">{value}</p>
          {change && (
            <div className={`flex items-center mt-2 text-sm ${
              changeType === 'positive' ? 'text-green-600' : 'text-red-600'
            }`}>
              <TrendingUp className="w-4 h-4 mr-1" />
              {change}
            </div>
          )}
        </div>
        <div className={`p-3 rounded-lg ${color}`}>
          <Icon className="w-6 h-6 text-white" />
        </div>
      </div>
    </div>
  );

  return (
    <div className="space-y-6">
      {/* Welcome Header */}
      <div className="bg-gradient-to-r from-primary-600 to-primary-700 rounded-lg p-6 text-white">
        <h1 className="text-3xl font-bold mb-2">
          Welcome back, {user?.FirstName}! 👋
        </h1>
        <p className="text-primary-100">
          Here's what's happening at your college today.
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard
          title="Total Students"
          value={stats.totalStudents.toLocaleString()}
          icon={Users}
          change="+12% from last month"
          changeType="positive"
          color="bg-blue-500"
        />
        <StatCard
          title="Total Teachers"
          value={stats.totalTeachers}
          icon={UserCheck}
          change="+2 from last month"
          changeType="positive"
          color="bg-green-500"
        />
        <StatCard
          title="Total Courses"
          value={stats.totalCourses}
          icon={BookOpen}
          change="+5 new courses"
          changeType="positive"
          color="bg-purple-500"
        />
        <StatCard
          title="Monthly Revenue"
          value={`$${stats.monthlyRevenue.toLocaleString()}`}
          icon={DollarSign}
          change="+8% from last month"
          changeType="positive"
          color="bg-yellow-500"
        />
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Enrollment Trends */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Enrollment Trends
          </h3>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={enrollmentData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="month" />
              <YAxis />
              <Tooltip />
              <Legend />
              <Line
                type="monotone"
                dataKey="students"
                stroke="#3b82f6"
                strokeWidth={2}
              />
              <Line
                type="monotone"
                dataKey="teachers"
                stroke="#10b981"
                strokeWidth={2}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>

        {/* Department Distribution */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Students by Department
          </h3>
          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={departmentData}
                cx="50%"
                cy="50%"
                labelLine={false}
                label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                outerRadius={80}
                fill="#8884d8"
                dataKey="value"
              >
                {departmentData.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={entry.color} />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Revenue Chart */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">
          Revenue Overview
        </h3>
        <ResponsiveContainer width="100%" height={300}>
          <BarChart data={revenueData}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="month" />
            <YAxis />
            <Tooltip />
            <Bar dataKey="revenue" fill="#3b82f6" />
          </BarChart>
        </ResponsiveContainer>
      </div>

      {/* Recent Activities & Upcoming Events */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Recent Activities */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Recent Activities
          </h3>
          <div className="space-y-4">
            {recentActivities.map((activity) => (
              <div key={activity.id} className="flex items-start space-x-3">
                <div className={`p-2 rounded-lg bg-gray-50 ${activity.color}`}>
                  <activity.icon className="w-4 h-4" />
                </div>
                <div className="flex-1">
                  <p className="text-sm text-gray-900">{activity.message}</p>
                  <p className="text-xs text-gray-500 mt-1">{activity.time}</p>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Upcoming Events */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Upcoming Events
          </h3>
          <div className="space-y-4">
            {upcomingEvents.map((event) => (
              <div key={event.id} className="flex items-center space-x-3">
                <div className="p-2 rounded-lg bg-primary-50">
                  <event.icon className="w-4 h-4 text-primary-600" />
                </div>
                <div className="flex-1">
                  <p className="text-sm font-medium text-gray-900">{event.title}</p>
                  <p className="text-xs text-gray-500">{event.date}</p>
                </div>
                <button className="text-primary-600 hover:text-primary-700">
                  <Eye className="w-4 h-4" />
                </button>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">
          Quick Actions
        </h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <button className="flex flex-col items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
            <Plus className="w-6 h-6 text-primary-600 mb-2" />
            <span className="text-sm text-gray-700">Add Student</span>
          </button>
          <button className="flex flex-col items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
            <BookOpen className="w-6 h-6 text-primary-600 mb-2" />
            <span className="text-sm text-gray-700">Create Course</span>
          </button>
          <button className="flex flex-col items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
            <Calendar className="w-6 h-6 text-primary-600 mb-2" />
            <span className="text-sm text-gray-700">Schedule Exam</span>
          </button>
          <button className="flex flex-col items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
            <DollarSign className="w-6 h-6 text-primary-600 mb-2" />
            <span className="text-sm text-gray-700">Generate Report</span>
          </button>
        </div>
      </div>
    </div>
  );
};

export default AdminDashboard;
