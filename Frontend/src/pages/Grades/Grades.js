import React, { useState, useEffect } from 'react';
import { useSelector, useDispatch } from 'react-redux';
import {
  fetchGrades,
  createGrade,
  clearError,
  setPagination,
} from '../../store/slices/academicSlice';
import {
  Plus,
  Search,
  Filter,
  Edit,
  Trash2,
  Eye,
  Download,
  Award,
  TrendingUp,
  TrendingDown,
  BarChart3,
  Users,
  BookOpen,
  ChevronLeft,
  ChevronRight,
  Star,
} from 'lucide-react';
import toast from 'react-hot-toast';

const Grades = () => {
  const dispatch = useDispatch();
  const { grades, isLoading, error, pagination } = useSelector((state) => state.academic);
  
  const [searchTerm, setSearchTerm] = useState('');
  const [showGradeModal, setShowGradeModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [selectedGrade, setSelectedGrade] = useState(null);
  const [filters, setFilters] = useState({
    course: '',
    exam: '',
    gradeRange: '',
  });

  const [gradeData, setGradeData] = useState({
    studentId: '',
    courseId: '',
    examId: '',
    marks: '',
    grade: '',
    feedback: '',
  });

  useEffect(() => {
    loadGrades();
  }, [pagination.page, pagination.pageSize, searchTerm, filters]);

  useEffect(() => {
    if (error) {
      toast.error(error);
      dispatch(clearError());
    }
  }, [error, dispatch]);

  const loadGrades = () => {
    const params = {
      page: pagination.page,
      pageSize: pagination.pageSize,
      search: searchTerm,
      ...filters,
    };
    dispatch(fetchGrades(params));
  };

  const handleCreateGrade = () => {
    setShowGradeModal(true);
  };

  const handleDelete = (grade) => {
    setSelectedGrade(grade);
    setShowDeleteModal(true);
  };

  const confirmCreateGrade = async () => {
    try {
      await dispatch(createGrade(gradeData)).unwrap();
      toast.success('Grade assigned successfully');
      setShowGradeModal(false);
      setGradeData({
        studentId: '',
        courseId: '',
        examId: '',
        marks: '',
        grade: '',
        feedback: '',
      });
      loadGrades();
    } catch (error) {
      toast.error(error || 'Failed to assign grade');
    }
  };

  const confirmDelete = async () => {
    try {
      await dispatch(deleteGrade(selectedGrade.GradeId)).unwrap();
      toast.success('Grade deleted successfully');
      setShowDeleteModal(false);
      setSelectedGrade(null);
      loadGrades();
    } catch (error) {
      toast.error(error || 'Failed to delete grade');
    }
  };

  const handlePageChange = (newPage) => {
    dispatch(setPagination({ page: newPage }));
  };

  const handlePageSizeChange = (newPageSize) => {
    dispatch(setPagination({ pageSize: newPageSize, page: 1 }));
  };

  const calculateGrade = (marks, totalMarks) => {
    const percentage = (marks / totalMarks) * 100;
    if (percentage >= 90) return 'A+';
    if (percentage >= 85) return 'A';
    if (percentage >= 80) return 'A-';
    if (percentage >= 75) return 'B+';
    if (percentage >= 70) return 'B';
    if (percentage >= 65) return 'B-';
    if (percentage >= 60) return 'C+';
    if (percentage >= 55) return 'C';
    if (percentage >= 50) return 'C-';
    return 'F';
  };

  const getGradeColor = (grade) => {
    const gradeColors = {
      'A+': 'bg-green-100 text-green-800 border-green-200',
      'A': 'bg-green-100 text-green-800 border-green-200',
      'A-': 'bg-green-100 text-green-800 border-green-200',
      'B+': 'bg-blue-100 text-blue-800 border-blue-200',
      'B': 'bg-blue-100 text-blue-800 border-blue-200',
      'B-': 'bg-blue-100 text-blue-800 border-blue-200',
      'C+': 'bg-yellow-100 text-yellow-800 border-yellow-200',
      'C': 'bg-yellow-100 text-yellow-800 border-yellow-200',
      'C-': 'bg-yellow-100 text-yellow-800 border-yellow-200',
      'F': 'bg-red-100 text-red-800 border-red-200',
    };
    return gradeColors[grade] || 'bg-gray-100 text-gray-800 border-gray-200';
  };

  const getPerformanceIcon = (marks, totalMarks) => {
    const percentage = (marks / totalMarks) * 100;
    if (percentage >= 80) return <TrendingUp className="w-4 h-4 text-green-600" />;
    if (percentage >= 60) return <BarChart3 className="w-4 h-4 text-blue-600" />;
    return <TrendingDown className="w-4 h-4 text-red-600" />;
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Grades</h1>
          <p className="text-gray-600">Manage student grades and academic performance</p>
        </div>
        <button onClick={handleCreateGrade} className="btn btn-primary btn-md">
          <Plus className="w-4 h-4 mr-2" />
          Assign Grade
        </button>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Total Grades</p>
              <p className="text-2xl font-bold text-gray-900">{pagination.totalCount}</p>
            </div>
            <div className="p-3 rounded-lg bg-blue-500">
              <Award className="w-6 h-6 text-white" />
            </div>
          </div>
        </div>
        
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Average Grade</p>
              <p className="text-2xl font-bold text-gray-900">B+</p>
              <div className="flex items-center mt-2 text-sm text-green-600">
                <TrendingUp className="w-4 h-4 mr-1" />
                +5% from last semester
              </div>
            </div>
            <div className="p-3 rounded-lg bg-green-500">
              <Star className="w-6 h-6 text-white" />
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">A Grades</p>
              <p className="text-2xl font-bold text-gray-900">
                {grades.filter(g => g.Grade?.startsWith('A')).length}
              </p>
            </div>
            <div className="p-3 rounded-lg bg-purple-500">
              <Award className="w-6 h-6 text-white" />
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Failures</p>
              <p className="text-2xl font-bold text-gray-900">
                {grades.filter(g => g.Grade === 'F').length}
              </p>
            </div>
            <div className="p-3 rounded-lg bg-red-500">
              <TrendingDown className="w-6 h-6 text-white" />
            </div>
          </div>
        </div>
      </div>

      {/* Search and Filters */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <div className="flex flex-col md:flex-row gap-4">
          <div className="flex-1">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
              <input
                type="text"
                placeholder="Search by student name, course, or grade..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="input pl-10"
              />
            </div>
          </div>
          
          <div className="flex gap-2">
            <select
              value={filters.course}
              onChange={(e) => setFilters({ ...filters, course: e.target.value })}
              className="input"
            >
              <option value="">All Courses</option>
              <option value="CS101">Computer Science 101</option>
              <option value="CS201">Data Structures</option>
              <option value="CS301">Web Development</option>
            </select>
            
            <select
              value={filters.exam}
              onChange={(e) => setFilters({ ...filters, exam: e.target.value })}
              className="input"
            >
              <option value="">All Exams</option>
              <option value="midterm1">Midterm 1</option>
              <option value="midterm2">Midterm 2</option>
              <option value="final">Final</option>
            </select>
            
            <select
              value={filters.gradeRange}
              onChange={(e) => setFilters({ ...filters, gradeRange: e.target.value })}
              className="input"
            >
              <option value="">All Grades</option>
              <option value="A">A Range (A+, A, A-)</option>
              <option value="B">B Range (B+, B, B-)</option>
              <option value="C">C Range (C+, C, C-)</option>
              <option value="F">Fail</option>
            </select>
            
            <button className="btn btn-secondary btn-md">
              <Filter className="w-4 h-4 mr-2" />
              More Filters
            </button>
            
            <button className="btn btn-secondary btn-md">
              <Download className="w-4 h-4 mr-2" />
              Export
            </button>
          </div>
        </div>
      </div>

      {/* Grades Table */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Student
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Course
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Exam
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Marks
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Grade
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Performance
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {isLoading ? (
                <tr>
                  <td colSpan="7" className="px-6 py-12 text-center">
                    <div className="flex items-center justify-center">
                      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
                    </div>
                  </td>
                </tr>
              ) : grades.length === 0 ? (
                <tr>
                  <td colSpan="7" className="px-6 py-12 text-center text-gray-500">
                    No grade records found
                  </td>
                </tr>
              ) : (
                grades.map((grade) => (
                  <tr key={grade.GradeId} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center">
                        <div className="w-8 h-8 bg-primary-100 rounded-full flex items-center justify-center">
                          <span className="text-primary-600 font-medium text-xs">
                            {grade.StudentName?.[0] || 'S'}
                          </span>
                        </div>
                        <div className="ml-3">
                          <div className="text-sm font-medium text-gray-900">
                            {grade.StudentName}
                          </div>
                          <div className="text-sm text-gray-500">
                            ID: {grade.StudentId}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-900">
                        {grade.CourseName}
                      </div>
                      <div className="text-sm text-gray-500">
                        {grade.CourseCode}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-900">
                        {grade.ExamTitle}
                      </div>
                      <div className="text-sm text-gray-500">
                        {new Date(grade.ExamDate).toLocaleDateString()}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-900">
                        {grade.Marks} / {grade.TotalMarks}
                      </div>
                      <div className="w-full bg-gray-200 rounded-full h-2 mt-1">
                        <div
                          className={`h-2 rounded-full ${
                            (grade.Marks / grade.TotalMarks) >= 0.8 ? 'bg-green-500' :
                            (grade.Marks / grade.TotalMarks) >= 0.6 ? 'bg-blue-500' :
                            'bg-red-500'
                          }`}
                          style={{ width: `${(grade.Marks / grade.TotalMarks) * 100}%` }}
                        />
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-medium border ${getGradeColor(grade.Grade)}`}>
                        {grade.Grade}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center space-x-2">
                        {getPerformanceIcon(grade.Marks, grade.TotalMarks)}
                        <span className="text-sm text-gray-600">
                          {Math.round((grade.Marks / grade.TotalMarks) * 100)}%
                        </span>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <div className="flex items-center space-x-2">
                        <button className="text-blue-600 hover:text-blue-900">
                          <Eye className="w-4 h-4" />
                        </button>
                        <button className="text-green-600 hover:text-green-900">
                          <Edit className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => handleDelete(grade)}
                          className="text-red-600 hover:text-red-900"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {pagination.totalPages > 1 && (
          <div className="px-6 py-4 border-t border-gray-200">
            <div className="flex items-center justify-between">
              <div className="text-sm text-gray-700">
                Showing {((pagination.page - 1) * pagination.pageSize) + 1} to{' '}
                {Math.min(pagination.page * pagination.pageSize, pagination.totalCount)} of{' '}
                {pagination.totalCount} results
              </div>
              <div className="flex items-center space-x-2">
                <select
                  value={pagination.pageSize}
                  onChange={(e) => handlePageSizeChange(Number(e.target.value))}
                  className="input text-sm"
                >
                  <option value={10}>10 per page</option>
                  <option value={25}>25 per page</option>
                  <option value={50}>50 per page</option>
                </select>
                
                <button
                  onClick={() => handlePageChange(pagination.page - 1)}
                  disabled={pagination.page === 1}
                  className="p-2 border border-gray-300 rounded-md disabled:opacity-50"
                >
                  <ChevronLeft className="w-4 h-4" />
                </button>
                
                <span className="text-sm text-gray-700">
                  Page {pagination.page} of {pagination.totalPages}
                </span>
                
                <button
                  onClick={() => handlePageChange(pagination.page + 1)}
                  disabled={pagination.page === pagination.totalPages}
                  className="p-2 border border-gray-300 rounded-md disabled:opacity-50"
                >
                  <ChevronRight className="w-4 h-4" />
                </button>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Assign Grade Modal */}
      {showGradeModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">
              Assign Grade
            </h3>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Student
                </label>
                <select
                  value={gradeData.studentId}
                  onChange={(e) => setGradeData({ ...gradeData, studentId: e.target.value })}
                  className="input"
                >
                  <option value="">Select Student</option>
                  <option value="1">John Doe</option>
                  <option value="2">Jane Smith</option>
                  <option value="3">Mike Johnson</option>
                </select>
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Course
                </label>
                <select
                  value={gradeData.courseId}
                  onChange={(e) => setGradeData({ ...gradeData, courseId: e.target.value })}
                  className="input"
                >
                  <option value="">Select Course</option>
                  <option value="CS101">Computer Science 101</option>
                  <option value="CS201">Data Structures</option>
                  <option value="CS301">Web Development</option>
                </select>
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Exam
                </label>
                <select
                  value={gradeData.examId}
                  onChange={(e) => setGradeData({ ...gradeData, examId: e.target.value })}
                  className="input"
                >
                  <option value="">Select Exam</option>
                  <option value="1">Midterm 1</option>
                  <option value="2">Midterm 2</option>
                  <option value="3">Final</option>
                </select>
              </div>
              
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Marks Obtained
                  </label>
                  <input
                    type="number"
                    value={gradeData.marks}
                    onChange={(e) => setGradeData({ ...gradeData, marks: e.target.value })}
                    className="input"
                    placeholder="85"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Grade
                  </label>
                  <select
                    value={gradeData.grade}
                    onChange={(e) => setGradeData({ ...gradeData, grade: e.target.value })}
                    className="input"
                  >
                    <option value="">Select Grade</option>
                    <option value="A+">A+</option>
                    <option value="A">A</option>
                    <option value="A-">A-</option>
                    <option value="B+">B+</option>
                    <option value="B">B</option>
                    <option value="B-">B-</option>
                    <option value="C+">C+</option>
                    <option value="C">C</option>
                    <option value="C-">C-</option>
                    <option value="F">F</option>
                  </select>
                </div>
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Feedback
                </label>
                <textarea
                  value={gradeData.feedback}
                  onChange={(e) => setGradeData({ ...gradeData, feedback: e.target.value })}
                  className="input"
                  rows={3}
                  placeholder="Provide feedback for the student..."
                />
              </div>
            </div>
            <div className="flex justify-end space-x-3 mt-6">
              <button
                onClick={() => setShowGradeModal(false)}
                className="btn btn-secondary btn-md"
              >
                Cancel
              </button>
              <button
                onClick={confirmCreateGrade}
                className="btn btn-primary btn-md"
              >
                Assign Grade
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {showDeleteModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">
              Confirm Delete
            </h3>
            <p className="text-gray-600 mb-6">
              Are you sure you want to delete this grade record?
              This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setShowDeleteModal(false)}
                className="btn btn-secondary btn-md"
              >
                Cancel
              </button>
              <button
                onClick={confirmDelete}
                className="btn btn-danger btn-md"
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Grades;
