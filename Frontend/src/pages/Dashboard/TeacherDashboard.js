import React, { useState } from 'react';
import { useSelector } from 'react-redux';
import {
  BookOpen,
  Users,
  Calendar,
  Clock,
  Award,
  CheckCircle,
  AlertCircle,
  Plus,
  Edit,
  Eye,
  TrendingUp,
  UserCheck,
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
} from 'recharts';

const TeacherDashboard = () => {
  const { user } = useSelector((state) => state.auth);
  const [stats] = useState({
    totalCourses: 6,
    totalStudents: 180,
    averageAttendance: 88,
    pendingGrades: 12,
    upcomingClasses: 4,
    completedLectures: 45,
  });

  const [myCourses] = useState([
    {
      id: 1,
      name: 'Computer Science 101',
      code: 'CS101',
      students: 45,
      attendance: 92,
      nextClass: 'Today, 2:00 PM',
      progress: 75,
    },
    {
      id: 2,
      name: 'Data Structures',
      code: 'CS201',
      students: 38,
      attendance: 85,
      nextClass: 'Tomorrow, 10:00 AM',
      progress: 60,
    },
    {
      id: 3,
      name: 'Web Development',
      code: 'CS301',
      students: 42,
      attendance: 90,
      nextClass: 'Friday, 3:00 PM',
      progress: 80,
    },
  ]);

  const [todaySchedule] = useState([
    {
      id: 1,
      time: '9:00 AM - 10:30 AM',
      course: 'Computer Science 101',
      room: 'Lab 201',
      topic: 'Introduction to Algorithms',
      status: 'completed',
    },
    {
      id: 2,
      time: '2:00 PM - 3:30 PM',
      course: 'Data Structures',
      room: 'Room 105',
      topic: 'Binary Trees',
      status: 'upcoming',
    },
    {
      id: 3,
      time: '4:00 PM - 5:30 PM',
      course: 'Web Development',
      room: 'Lab 301',
      topic: 'React Components',
      status: 'upcoming',
    },
  ]);

  const [recentActivities] = useState([
    {
      id: 1,
      type: 'grade',
      message: 'Graded midterm exams for CS101',
      time: '2 hours ago',
      icon: Award,
      color: 'text-green-600',
    },
    {
      id: 2,
      type: 'attendance',
      message: 'Marked attendance for Data Structures',
      time: '4 hours ago',
      icon: CheckCircle,
      color: 'text-blue-600',
    },
    {
      id: 3,
      type: 'assignment',
      message: 'Posted new assignment for Web Development',
      time: '1 day ago',
      icon: Edit,
      color: 'text-purple-600',
    },
    {
      id: 4,
      type: 'alert',
      message: '5 students have low attendance in CS101',
      time: '2 days ago',
      icon: AlertCircle,
      color: 'text-red-600',
    },
  ]);

  // Sample data for charts
  const attendanceData = [
    { course: 'CS101', attendance: 92 },
    { course: 'CS201', attendance: 85 },
    { course: 'CS301', attendance: 90 },
    { course: 'CS401', attendance: 88 },
  ];

  const gradeDistribution = [
    { grade: 'A', count: 25, color: '#10b981' },
    { grade: 'B', count: 35, color: '#3b82f6' },
    { grade: 'C', count: 20, color: '#f59e0b' },
    { grade: 'D', count: 15, color: '#ef4444' },
    { grade: 'F', count: 5, color: '#6b7280' },
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
      <div className="bg-gradient-to-r from-green-600 to-teal-600 rounded-lg p-6 text-white">
        <h1 className="text-3xl font-bold mb-2">
          Welcome back, Professor {user?.LastName}! 👋
        </h1>
        <p className="text-green-100">
          You have {stats.upcomingClasses} classes scheduled for today.
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard
          title="My Courses"
          value={stats.totalCourses}
          icon={BookOpen}
          color="bg-blue-500"
        />
        <StatCard
          title="Total Students"
          value={stats.totalStudents}
          icon={Users}
          change="+15 this semester"
          changeType="positive"
          color="bg-green-500"
        />
        <StatCard
          title="Avg Attendance"
          value={`${stats.averageAttendance}%`}
          icon={UserCheck}
          change="+3% from last month"
          changeType="positive"
          color="bg-purple-500"
        />
        <StatCard
          title="Pending Grades"
          value={stats.pendingGrades}
          icon={Clock}
          change="Due this week"
          changeType="negative"
          color="bg-yellow-500"
        />
      </div>

      {/* Today's Schedule */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">
          Today's Schedule
        </h3>
        <div className="space-y-3">
          {todaySchedule.map((schedule) => (
            <div
              key={schedule.id}
              className={`flex items-center justify-between p-4 rounded-lg border ${
                schedule.status === 'completed'
                  ? 'bg-green-50 border-green-200'
                  : 'bg-blue-50 border-blue-200'
              }`}
            >
              <div className="flex items-center space-x-4">
                <div className={`w-3 h-3 rounded-full ${
                  schedule.status === 'completed' ? 'bg-green-500' : 'bg-blue-500'
                }`} />
                <div>
                  <p className="font-medium text-gray-900">{schedule.course}</p>
                  <p className="text-sm text-gray-600">{schedule.topic}</p>
                  <p className="text-xs text-gray-500">{schedule.room}</p>
                </div>
              </div>
              <div className="text-right">
                <p className="text-sm font-medium text-gray-900">{schedule.time}</p>
                <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${
                  schedule.status === 'completed'
                    ? 'bg-green-100 text-green-800'
                    : 'bg-blue-100 text-blue-800'
                }`}>
                  {schedule.status === 'completed' ? 'Completed' : 'Upcoming'}
                </span>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* My Courses */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-gray-900">My Courses</h3>
          <button className="btn btn-primary btn-sm">
            <Plus className="w-4 h-4 mr-1" />
            Add Course
          </button>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Course
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Students
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Attendance
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Progress
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Next Class
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {myCourses.map((course) => (
                <tr key={course.id}>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div>
                      <div className="text-sm font-medium text-gray-900">
                        {course.name}
                      </div>
                      <div className="text-sm text-gray-500">{course.code}</div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {course.students}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <span className="text-sm text-gray-900">{course.attendance}%</span>
                      <div className="ml-2 w-16 bg-gray-200 rounded-full h-2">
                        <div
                          className="bg-green-500 h-2 rounded-full"
                          style={{ width: `${course.attendance}%` }}
                        />
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <span className="text-sm text-gray-900">{course.progress}%</span>
                      <div className="ml-2 w-16 bg-gray-200 rounded-full h-2">
                        <div
                          className="bg-blue-500 h-2 rounded-full"
                          style={{ width: `${course.progress}%` }}
                        />
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {course.nextClass}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                    <div className="flex space-x-2">
                      <button className="text-blue-600 hover:text-blue-900">
                        <Eye className="w-4 h-4" />
                      </button>
                      <button className="text-green-600 hover:text-green-900">
                        <Edit className="w-4 h-4" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Attendance by Course */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Attendance by Course
          </h3>
          <ResponsiveContainer width="100%" height={250}>
            <BarChart data={attendanceData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="course" />
              <YAxis />
              <Tooltip />
              <Bar dataKey="attendance" fill="#3b82f6" />
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Grade Distribution */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Grade Distribution
          </h3>
          <ResponsiveContainer width="100%" height={250}>
            <PieChart>
              <Pie
                data={gradeDistribution}
                cx="50%"
                cy="50%"
                labelLine={false}
                label={({ grade, count }) => `${grade}: ${count}`}
                outerRadius={80}
                fill="#8884d8"
                dataKey="count"
              >
                {gradeDistribution.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={entry.color} />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
        </div>
      </div>

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
    </div>
  );
};

export default TeacherDashboard;
