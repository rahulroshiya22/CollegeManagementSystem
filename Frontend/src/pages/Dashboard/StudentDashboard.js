import React, { useState } from 'react';
import { useSelector } from 'react-redux';
import {
  BookOpen,
  Calendar,
  Clock,
  Award,
  DollarSign,
  CheckCircle,
  AlertCircle,
  TrendingUp,
  Download,
  Play,
  FileText,
  Bell,
} from 'lucide-react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  LineChart,
  Line,
  PieChart,
  Pie,
  Cell,
} from 'recharts';

const StudentDashboard = () => {
  const { user } = useSelector((state) => state.auth);
  const [stats] = useState({
    enrolledCourses: 6,
    completedCourses: 12,
    currentGPA: 3.8,
    attendanceRate: 92,
    pendingFees: 2500,
    totalCredits: 48,
  });

  const [enrolledCourses] = useState([
    {
      id: 1,
      name: 'Computer Science 101',
      code: 'CS101',
      instructor: 'Dr. Smith',
      credits: 3,
      attendance: 95,
      grade: 'A-',
      nextClass: 'Today, 2:00 PM',
      progress: 75,
    },
    {
      id: 2,
      name: 'Data Structures',
      code: 'CS201',
      instructor: 'Prof. Johnson',
      credits: 4,
      attendance: 88,
      grade: 'B+',
      nextClass: 'Tomorrow, 10:00 AM',
      progress: 60,
    },
    {
      id: 3,
      name: 'Web Development',
      code: 'CS301',
      instructor: 'Dr. Williams',
      credits: 3,
      attendance: 92,
      grade: 'A',
      nextClass: 'Friday, 3:00 PM',
      progress: 80,
    },
  ]);

  const [upcomingAssignments] = useState([
    {
      id: 1,
      title: 'Algorithm Analysis Report',
      course: 'CS101',
      dueDate: '2024-03-12',
      priority: 'high',
      submitted: false,
    },
    {
      id: 2,
      title: 'Binary Tree Implementation',
      course: 'CS201',
      dueDate: '2024-03-15',
      priority: 'medium',
      submitted: false,
    },
    {
      id: 3,
      title: 'React Project',
      course: 'CS301',
      dueDate: '2024-03-20',
      priority: 'medium',
      submitted: true,
    },
  ]);

  const [recentGrades] = useState([
    {
      id: 1,
      course: 'Computer Science 101',
      assignment: 'Midterm Exam',
      grade: 'A-',
      score: 92,
      total: 100,
      date: '2024-03-01',
    },
    {
      id: 2,
      course: 'Data Structures',
      assignment: 'Quiz 3',
      grade: 'B+',
      score: 87,
      total: 100,
      date: '2024-02-28',
    },
    {
      id: 3,
      course: 'Web Development',
      assignment: 'Project 1',
      grade: 'A',
      score: 95,
      total: 100,
      date: '2024-02-25',
    },
  ]);

  const [announcements] = useState([
    {
      id: 1,
      title: 'Mid-term Examination Schedule',
      message: 'Mid-term exams will start from March 15th. Please check the schedule.',
      date: '2024-03-05',
      priority: 'high',
    },
    {
      id: 2,
      title: 'Fee Payment Reminder',
      message: 'Last date for fee payment is March 20th. Late fees will apply after that.',
      date: '2024-03-03',
      priority: 'medium',
    },
    {
      id: 3,
      title: 'Workshop on Web Development',
      message: 'A workshop on modern web development will be held on March 10th.',
      date: '2024-03-01',
      priority: 'low',
    },
  ]);

  // Sample data for charts
  const gradeTrend = [
    { month: 'Jan', gpa: 3.6 },
    { month: 'Feb', gpa: 3.7 },
    { month: 'Mar', gpa: 3.8 },
    { month: 'Apr', gpa: 3.75 },
    { month: 'May', gpa: 3.8 },
  ];

  const courseProgress = [
    { course: 'CS101', progress: 75 },
    { course: 'CS201', progress: 60 },
    { course: 'CS301', progress: 80 },
    { course: 'CS401', progress: 45 },
  ];

  const gradeDistribution = [
    { grade: 'A', count: 8, color: '#10b981' },
    { grade: 'B', count: 12, color: '#3b82f6' },
    { grade: 'C', count: 4, color: '#f59e0b' },
    { grade: 'D', count: 2, color: '#ef4444' },
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

  const getPriorityColor = (priority) => {
    switch (priority) {
      case 'high':
        return 'bg-red-100 text-red-800';
      case 'medium':
        return 'bg-yellow-100 text-yellow-800';
      case 'low':
        return 'bg-green-100 text-green-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  return (
    <div className="space-y-6">
      {/* Welcome Header */}
      <div className="bg-gradient-to-r from-purple-600 to-pink-600 rounded-lg p-6 text-white">
        <h1 className="text-3xl font-bold mb-2">
          Welcome back, {user?.FirstName}! 👋
        </h1>
        <p className="text-purple-100">
          You're doing great! Keep up the good work with your studies.
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard
          title="Current GPA"
          value={stats.currentGPA.toFixed(1)}
          icon={Award}
          change="+0.2 this semester"
          changeType="positive"
          color="bg-purple-500"
        />
        <StatCard
          title="Enrolled Courses"
          value={stats.enrolledCourses}
          icon={BookOpen}
          change={`${stats.totalCredits} credits`}
          changeType="positive"
          color="bg-blue-500"
        />
        <StatCard
          title="Attendance Rate"
          value={`${stats.attendanceRate}%`}
          icon={CheckCircle}
          change="+5% from last month"
          changeType="positive"
          color="bg-green-500"
        />
        <StatCard
          title="Pending Fees"
          value={`$${stats.pendingFees}`}
          icon={DollarSign}
          change="Due by March 20"
          changeType="negative"
          color="bg-yellow-500"
        />
      </div>

      {/* My Courses */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">
          My Enrolled Courses
        </h3>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Course
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Instructor
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Credits
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Attendance
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Grade
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Next Class
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {enrolledCourses.map((course) => (
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
                    {course.instructor}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {course.credits}
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
                    <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                      {course.grade}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {course.nextClass}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Upcoming Assignments & Recent Grades */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Upcoming Assignments */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Upcoming Assignments
          </h3>
          <div className="space-y-3">
            {upcomingAssignments.map((assignment) => (
              <div
                key={assignment.id}
                className={`p-4 rounded-lg border ${
                  assignment.submitted
                    ? 'bg-green-50 border-green-200'
                    : 'bg-white border-gray-200'
                }`}
              >
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <p className="font-medium text-gray-900">{assignment.title}</p>
                    <p className="text-sm text-gray-600">{assignment.course}</p>
                    <p className="text-xs text-gray-500 mt-1">Due: {assignment.dueDate}</p>
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getPriorityColor(assignment.priority)}`}>
                      {assignment.priority}
                    </span>
                    {assignment.submitted ? (
                      <CheckCircle className="w-5 h-5 text-green-600" />
                    ) : (
                      <button className="text-blue-600 hover:text-blue-900">
                        <FileText className="w-5 h-5" />
                      </button>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Recent Grades */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Recent Grades
          </h3>
          <div className="space-y-3">
            {recentGrades.map((grade) => (
              <div key={grade.id} className="p-4 rounded-lg border border-gray-200">
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <p className="font-medium text-gray-900">{grade.assignment}</p>
                    <p className="text-sm text-gray-600">{grade.course}</p>
                    <p className="text-xs text-gray-500 mt-1">{grade.date}</p>
                  </div>
                  <div className="text-right">
                    <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                      {grade.grade}
                    </span>
                    <p className="text-sm text-gray-600 mt-1">
                      {grade.score}/{grade.total}
                    </p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* GPA Trend */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            GPA Trend
          </h3>
          <ResponsiveContainer width="100%" height={250}>
            <LineChart data={gradeTrend}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="month" />
              <YAxis domain={[3, 4]} />
              <Tooltip />
              <Line
                type="monotone"
                dataKey="gpa"
                stroke="#8b5cf6"
                strokeWidth={2}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>

        {/* Course Progress */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Course Progress
          </h3>
          <ResponsiveContainer width="100%" height={250}>
            <BarChart data={courseProgress}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="course" />
              <YAxis />
              <Tooltip />
              <Bar dataKey="progress" fill="#3b82f6" />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Announcements */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-gray-900">
            Announcements
          </h3>
          <Bell className="w-5 h-5 text-gray-500" />
        </div>
        <div className="space-y-4">
          {announcements.map((announcement) => (
            <div key={announcement.id} className="p-4 rounded-lg border border-gray-200">
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center space-x-2">
                    <h4 className="font-medium text-gray-900">{announcement.title}</h4>
                    <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getPriorityColor(announcement.priority)}`}>
                      {announcement.priority}
                    </span>
                  </div>
                  <p className="text-sm text-gray-600 mt-1">{announcement.message}</p>
                  <p className="text-xs text-gray-500 mt-2">{announcement.date}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default StudentDashboard;
