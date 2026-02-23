import React, { useState, useEffect } from 'react';
import { useSelector, useDispatch } from 'react-redux';
import {
  fetchExams,
  createExam,
  deleteExam,
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
  Calendar,
  Clock,
  Users,
  FileText,
  CheckCircle,
  XCircle,
  AlertTriangle,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import toast from 'react-hot-toast';

const Exams = () => {
  const dispatch = useDispatch();
  const { exams, isLoading, error, pagination } = useSelector((state) => state.academic);
  
  const [searchTerm, setSearchTerm] = useState('');
  const [showExamModal, setShowExamModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [selectedExam, setSelectedExam] = useState(null);
  const [filters, setFilters] = useState({
    status: '',
    type: '',
    course: '',
  });

  const [examData, setExamData] = useState({
    title: '',
    description: '',
    courseId: '',
    examDate: '',
    startTime: '',
    duration: '',
    totalMarks: '',
    examType: 'midterm',
    instructions: '',
  });

  useEffect(() => {
    loadExams();
  }, [pagination.page, pagination.pageSize, searchTerm, filters]);

  useEffect(() => {
    if (error) {
      toast.error(error);
      dispatch(clearError());
    }
  }, [error, dispatch]);

  const loadExams = () => {
    const params = {
      page: pagination.page,
      pageSize: pagination.pageSize,
      search: searchTerm,
      ...filters,
    };
    dispatch(fetchExams(params));
  };

  const handleCreateExam = () => {
    setShowExamModal(true);
  };

  const handleDelete = (exam) => {
    setSelectedExam(exam);
    setShowDeleteModal(true);
  };

  const confirmCreateExam = async () => {
    try {
      await dispatch(createExam(examData)).unwrap();
      toast.success('Exam created successfully');
      setShowExamModal(false);
      setExamData({
        title: '',
        description: '',
        courseId: '',
        examDate: '',
        startTime: '',
        duration: '',
        totalMarks: '',
        examType: 'midterm',
        instructions: '',
      });
      loadExams();
    } catch (error) {
      toast.error(error || 'Failed to create exam');
    }
  };

  const confirmDelete = async () => {
    try {
      await dispatch(deleteExam(selectedExam.ExamId)).unwrap();
      toast.success('Exam deleted successfully');
      setShowDeleteModal(false);
      setSelectedExam(null);
      loadExams();
    } catch (error) {
      toast.error(error || 'Failed to delete exam');
    }
  };

  const handlePageChange = (newPage) => {
    dispatch(setPagination({ page: newPage }));
  };

  const handlePageSizeChange = (newPageSize) => {
    dispatch(setPagination({ pageSize: newPageSize, page: 1 }));
  };

  const getStatusBadge = (status) => {
    const statusColors = {
      Upcoming: 'bg-blue-100 text-blue-800',
      Ongoing: 'bg-green-100 text-green-800',
      Completed: 'bg-gray-100 text-gray-800',
      Cancelled: 'bg-red-100 text-red-800',
    };
    return (
      <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${statusColors[status] || 'bg-gray-100 text-gray-800'}`}>
        {status}
      </span>
    );
  };

  const getStatusIcon = (status) => {
    const icons = {
      Upcoming: <Clock className="w-4 h-4 text-blue-600" />,
      Ongoing: <CheckCircle className="w-4 h-4 text-green-600" />,
      Completed: <FileText className="w-4 h-4 text-gray-600" />,
      Cancelled: <XCircle className="w-4 h-4 text-red-600" />,
    };
    return icons[status] || <Clock className="w-4 h-4 text-gray-600" />;
  };

  const getTypeBadge = (type) => {
    const typeColors = {
      midterm: 'bg-purple-100 text-purple-800',
      final: 'bg-red-100 text-red-800',
      quiz: 'bg-yellow-100 text-yellow-800',
      assignment: 'bg-green-100 text-green-800',
    };
    return (
      <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${typeColors[type] || 'bg-gray-100 text-gray-800'}`}>
        {type.charAt(0).toUpperCase() + type.slice(1)}
      </span>
    );
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Exams</h1>
          <p className="text-gray-600">Manage examinations and assessments</p>
        </div>
        <button onClick={handleCreateExam} className="btn btn-primary btn-md">
          <Plus className="w-4 h-4 mr-2" />
          Create Exam
        </button>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Total Exams</p>
              <p className="text-2xl font-bold text-gray-900">{pagination.totalCount}</p>
            </div>
            <div className="p-3 rounded-lg bg-blue-500">
              <FileText className="w-6 h-6 text-white" />
            </div>
          </div>
        </div>
        
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Upcoming</p>
              <p className="text-2xl font-bold text-gray-900">
                {exams.filter(e => e.Status === 'Upcoming').length}
              </p>
            </div>
            <div className="p-3 rounded-lg bg-blue-500">
              <Clock className="w-6 h-6 text-white" />
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Ongoing</p>
              <p className="text-2xl font-bold text-gray-900">
                {exams.filter(e => e.Status === 'Ongoing').length}
              </p>
            </div>
            <div className="p-3 rounded-lg bg-green-500">
              <CheckCircle className="w-6 h-6 text-white" />
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Completed</p>
              <p className="text-2xl font-bold text-gray-900">
                {exams.filter(e => e.Status === 'Completed').length}
              </p>
            </div>
            <div className="p-3 rounded-lg bg-gray-500">
              <FileText className="w-6 h-6 text-white" />
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
                placeholder="Search by exam title, course, or instructor..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="input pl-10"
              />
            </div>
          </div>
          
          <div className="flex gap-2">
            <select
              value={filters.status}
              onChange={(e) => setFilters({ ...filters, status: e.target.value })}
              className="input"
            >
              <option value="">All Status</option>
              <option value="Upcoming">Upcoming</option>
              <option value="Ongoing">Ongoing</option>
              <option value="Completed">Completed</option>
              <option value="Cancelled">Cancelled</option>
            </select>
            
            <select
              value={filters.type}
              onChange={(e) => setFilters({ ...filters, type: e.target.value })}
              className="input"
            >
              <option value="">All Types</option>
              <option value="midterm">Midterm</option>
              <option value="final">Final</option>
              <option value="quiz">Quiz</option>
              <option value="assignment">Assignment</option>
            </select>
            
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

      {/* Exams Table */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Exam
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Course
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Date & Time
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Duration
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Total Marks
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Status
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
              ) : exams.length === 0 ? (
                <tr>
                  <td colSpan="7" className="px-6 py-12 text-center text-gray-500">
                    No exams found
                  </td>
                </tr>
              ) : (
                exams.map((exam) => (
                  <tr key={exam.ExamId} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div>
                        <div className="text-sm font-medium text-gray-900">
                          {exam.Title}
                        </div>
                        <div className="text-sm text-gray-500">
                          {exam.Description}
                        </div>
                        <div className="mt-1">
                          {getTypeBadge(exam.ExamType)}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-900">
                        {exam.CourseName}
                      </div>
                      <div className="text-sm text-gray-500">
                        {exam.CourseCode}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-900">
                        {new Date(exam.ExamDate).toLocaleDateString()}
                      </div>
                      <div className="text-sm text-gray-500">
                        {exam.StartTime}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {exam.Duration}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {exam.TotalMarks}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center space-x-2">
                        {getStatusIcon(exam.Status)}
                        {getStatusBadge(exam.Status)}
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
                          onClick={() => handleDelete(exam)}
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

      {/* Create Exam Modal */}
      {showExamModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-2xl max-h-[80vh] overflow-y-auto">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">
              Create Exam
            </h3>
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Exam Title
                  </label>
                  <input
                    type="text"
                    value={examData.title}
                    onChange={(e) => setExamData({ ...examData, title: e.target.value })}
                    className="input"
                    placeholder="Enter exam title"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Course
                  </label>
                  <select
                    value={examData.courseId}
                    onChange={(e) => setExamData({ ...examData, courseId: e.target.value })}
                    className="input"
                  >
                    <option value="">Select Course</option>
                    <option value="CS101">Computer Science 101</option>
                    <option value="CS201">Data Structures</option>
                    <option value="CS301">Web Development</option>
                  </select>
                </div>
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Description
                </label>
                <textarea
                  value={examData.description}
                  onChange={(e) => setExamData({ ...examData, description: e.target.value })}
                  className="input"
                  rows={3}
                  placeholder="Enter exam description"
                />
              </div>
              
              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Exam Date
                  </label>
                  <input
                    type="date"
                    value={examData.examDate}
                    onChange={(e) => setExamData({ ...examData, examDate: e.target.value })}
                    className="input"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Start Time
                  </label>
                  <input
                    type="time"
                    value={examData.startTime}
                    onChange={(e) => setExamData({ ...examData, startTime: e.target.value })}
                    className="input"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Duration (minutes)
                  </label>
                  <input
                    type="number"
                    value={examData.duration}
                    onChange={(e) => setExamData({ ...examData, duration: e.target.value })}
                    className="input"
                    placeholder="120"
                  />
                </div>
              </div>
              
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Total Marks
                  </label>
                  <input
                    type="number"
                    value={examData.totalMarks}
                    onChange={(e) => setExamData({ ...examData, totalMarks: e.target.value })}
                    className="input"
                    placeholder="100"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Exam Type
                  </label>
                  <select
                    value={examData.examType}
                    onChange={(e) => setExamData({ ...examData, examType: e.target.value })}
                    className="input"
                  >
                    <option value="midterm">Midterm</option>
                    <option value="final">Final</option>
                    <option value="quiz">Quiz</option>
                    <option value="assignment">Assignment</option>
                  </select>
                </div>
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Instructions
                </label>
                <textarea
                  value={examData.instructions}
                  onChange={(e) => setExamData({ ...examData, instructions: e.target.value })}
                  className="input"
                  rows={4}
                  placeholder="Enter exam instructions for students"
                />
              </div>
            </div>
            <div className="flex justify-end space-x-3 mt-6">
              <button
                onClick={() => setShowExamModal(false)}
                className="btn btn-secondary btn-md"
              >
                Cancel
              </button>
              <button
                onClick={confirmCreateExam}
                className="btn btn-primary btn-md"
              >
                Create Exam
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
              Are you sure you want to delete "{selectedExam?.Title}"?
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

export default Exams;
