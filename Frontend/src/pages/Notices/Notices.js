import React, { useState, useEffect } from 'react';
import { useSelector, useDispatch } from 'react-redux';
import {
  fetchNotices,
  createNotice,
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
  Bell,
  Calendar,
  AlertTriangle,
  Info,
  CheckCircle,
  XCircle,
  ChevronLeft,
  ChevronRight,
  Megaphone,
  Users,
  Pin,
} from 'lucide-react';
import toast from 'react-hot-toast';

const Notices = () => {
  const dispatch = useDispatch();
  const { notices, isLoading, error, pagination } = useSelector((state) => state.academic);
  
  const [searchTerm, setSearchTerm] = useState('');
  const [showNoticeModal, setShowNoticeModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [selectedNotice, setSelectedNotice] = useState(null);
  const [filters, setFilters] = useState({
    priority: '',
    audience: '',
    status: '',
  });

  const [noticeData, setNoticeData] = useState({
    title: '',
    content: '',
    priority: 'medium',
    audience: 'all',
    startDate: new Date().toISOString().split('T')[0],
    endDate: '',
    attachments: [],
  });

  useEffect(() => {
    loadNotices();
  }, [pagination.page, pagination.pageSize, searchTerm, filters]);

  useEffect(() => {
    if (error) {
      toast.error(error);
      dispatch(clearError());
    }
  }, [error, dispatch]);

  const loadNotices = () => {
    const params = {
      page: pagination.page,
      pageSize: pagination.pageSize,
      search: searchTerm,
      ...filters,
    };
    dispatch(fetchNotices(params));
  };

  const handleCreateNotice = () => {
    setShowNoticeModal(true);
  };

  const handleDelete = (notice) => {
    setSelectedNotice(notice);
    setShowDeleteModal(true);
  };

  const confirmCreateNotice = async () => {
    try {
      await dispatch(createNotice(noticeData)).unwrap();
      toast.success('Notice created successfully');
      setShowNoticeModal(false);
      setNoticeData({
        title: '',
        content: '',
        priority: 'medium',
        audience: 'all',
        startDate: new Date().toISOString().split('T')[0],
        endDate: '',
        attachments: [],
      });
      loadNotices();
    } catch (error) {
      toast.error(error || 'Failed to create notice');
    }
  };

  const confirmDelete = async () => {
    try {
      await dispatch(deleteNotice(selectedNotice.NoticeId)).unwrap();
      toast.success('Notice deleted successfully');
      setShowDeleteModal(false);
      setSelectedNotice(null);
      loadNotices();
    } catch (error) {
      toast.error(error || 'Failed to delete notice');
    }
  };

  const handlePageChange = (newPage) => {
    dispatch(setPagination({ page: newPage }));
  };

  const handlePageSizeChange = (newPageSize) => {
    dispatch(setPagination({ pageSize: newPageSize, page: 1 }));
  };

  const getPriorityBadge = (priority) => {
    const priorityColors = {
      high: 'bg-red-100 text-red-800 border-red-200',
      medium: 'bg-yellow-100 text-yellow-800 border-yellow-200',
      low: 'bg-green-100 text-green-800 border-green-200',
    };
    return (
      <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium border ${priorityColors[priority] || 'bg-gray-100 text-gray-800 border-gray-200'}`}>
        {priority.charAt(0).toUpperCase() + priority.slice(1)}
      </span>
    );
  };

  const getPriorityIcon = (priority) => {
    const icons = {
      high: <AlertTriangle className="w-4 h-4 text-red-600" />,
      medium: <Info className="w-4 h-4 text-yellow-600" />,
      low: <CheckCircle className="w-4 h-4 text-green-600" />,
    };
    return icons[priority] || <Bell className="w-4 h-4 text-gray-600" />;
  };

  const getAudienceBadge = (audience) => {
    const audienceColors = {
      all: 'bg-blue-100 text-blue-800',
      students: 'bg-green-100 text-green-800',
      teachers: 'bg-purple-100 text-purple-800',
      admin: 'bg-red-100 text-red-800',
    };
    return (
      <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${audienceColors[audience] || 'bg-gray-100 text-gray-800'}`}>
        {audience.charAt(0).toUpperCase() + audience.slice(1)}
      </span>
    );
  };

  const getStatusBadge = (status) => {
    const statusColors = {
      active: 'bg-green-100 text-green-800',
      expired: 'bg-gray-100 text-gray-800',
      scheduled: 'bg-blue-100 text-blue-800',
    };
    return (
      <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${statusColors[status] || 'bg-gray-100 text-gray-800'}`}>
        {status.charAt(0).toUpperCase() + status.slice(1)}
      </span>
    );
  };

  const isNoticeExpired = (endDate) => {
    return new Date(endDate) < new Date();
  };

  const isNoticeScheduled = (startDate) => {
    return new Date(startDate) > new Date();
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Notices</h1>
          <p className="text-gray-600">Manage announcements and notifications</p>
        </div>
        <button onClick={handleCreateNotice} className="btn btn-primary btn-md">
          <Plus className="w-4 h-4 mr-2" />
          Create Notice
        </button>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Total Notices</p>
              <p className="text-2xl font-bold text-gray-900">{pagination.totalCount}</p>
            </div>
            <div className="p-3 rounded-lg bg-blue-500">
              <Megaphone className="w-6 h-6 text-white" />
            </div>
          </div>
        </div>
        
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Active</p>
              <p className="text-2xl font-bold text-gray-900">
                {notices.filter(n => !isNoticeExpired(n.EndDate) && !isNoticeScheduled(n.StartDate)).length}
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
              <p className="text-sm font-medium text-gray-600">High Priority</p>
              <p className="text-2xl font-bold text-gray-900">
                {notices.filter(n => n.Priority === 'high').length}
              </p>
            </div>
            <div className="p-3 rounded-lg bg-red-500">
              <AlertTriangle className="w-6 h-6 text-white" />
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Scheduled</p>
              <p className="text-2xl font-bold text-gray-900">
                {notices.filter(n => isNoticeScheduled(n.StartDate)).length}
              </p>
            </div>
            <div className="p-3 rounded-lg bg-purple-500">
              <Calendar className="w-6 h-6 text-white" />
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
                placeholder="Search by title, content, or author..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="input pl-10"
              />
            </div>
          </div>
          
          <div className="flex gap-2">
            <select
              value={filters.priority}
              onChange={(e) => setFilters({ ...filters, priority: e.target.value })}
              className="input"
            >
              <option value="">All Priorities</option>
              <option value="high">High</option>
              <option value="medium">Medium</option>
              <option value="low">Low</option>
            </select>
            
            <select
              value={filters.audience}
              onChange={(e) => setFilters({ ...filters, audience: e.target.value })}
              className="input"
            >
              <option value="">All Audiences</option>
              <option value="all">All</option>
              <option value="students">Students</option>
              <option value="teachers">Teachers</option>
              <option value="admin">Admin</option>
            </select>
            
            <select
              value={filters.status}
              onChange={(e) => setFilters({ ...filters, status: e.target.value })}
              className="input"
            >
              <option value="">All Status</option>
              <option value="active">Active</option>
              <option value="scheduled">Scheduled</option>
              <option value="expired">Expired</option>
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

      {/* Notices Table */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Notice
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Author
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Audience
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Duration
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Priority
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
              ) : notices.length === 0 ? (
                <tr>
                  <td colSpan="7" className="px-6 py-12 text-center text-gray-500">
                    No notices found
                  </td>
                </tr>
              ) : (
                notices.map((notice) => (
                  <tr key={notice.NoticeId} className="hover:bg-gray-50">
                    <td className="px-6 py-4">
                      <div className="flex items-start space-x-3">
                        <div className="flex-shrink-0 mt-1">
                          {getPriorityIcon(notice.Priority)}
                        </div>
                        <div className="flex-1">
                          <div className="text-sm font-medium text-gray-900">
                            {notice.Title}
                            {notice.IsPinned && <Pin className="w-4 h-4 text-red-500 inline ml-2" />}
                          </div>
                          <div className="text-sm text-gray-500 mt-1 line-clamp-2">
                            {notice.Content}
                          </div>
                          {notice.Attachments && notice.Attachments.length > 0 && (
                            <div className="flex items-center mt-2 text-xs text-gray-500">
                              <Download className="w-3 h-3 mr-1" />
                              {notice.Attachments.length} attachment(s)
                            </div>
                          )}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-900">
                        {notice.AuthorName}
                      </div>
                      <div className="text-sm text-gray-500">
                        {new Date(notice.CreatedAt).toLocaleDateString()}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {getAudienceBadge(notice.Audience)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-900">
                        {new Date(notice.StartDate).toLocaleDateString()}
                      </div>
                      <div className="text-sm text-gray-500">
                        to {new Date(notice.EndDate).toLocaleDateString()}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {getPriorityBadge(notice.Priority)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {getStatusBadge(
                        isNoticeExpired(notice.EndDate) ? 'expired' :
                        isNoticeScheduled(notice.StartDate) ? 'scheduled' : 'active'
                      )}
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
                          onClick={() => handleDelete(notice)}
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

      {/* Create Notice Modal */}
      {showNoticeModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-2xl max-h-[80vh] overflow-y-auto">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">
              Create Notice
            </h3>
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Title
                  </label>
                  <input
                    type="text"
                    value={noticeData.title}
                    onChange={(e) => setNoticeData({ ...noticeData, title: e.target.value })}
                    className="input"
                    placeholder="Enter notice title"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Priority
                  </label>
                  <select
                    value={noticeData.priority}
                    onChange={(e) => setNoticeData({ ...noticeData, priority: e.target.value })}
                    className="input"
                  >
                    <option value="low">Low</option>
                    <option value="medium">Medium</option>
                    <option value="high">High</option>
                  </select>
                </div>
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Content
                </label>
                <textarea
                  value={noticeData.content}
                  onChange={(e) => setNoticeData({ ...noticeData, content: e.target.value })}
                  className="input"
                  rows={6}
                  placeholder="Enter notice content..."
                />
              </div>
              
              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Audience
                  </label>
                  <select
                    value={noticeData.audience}
                    onChange={(e) => setNoticeData({ ...noticeData, audience: e.target.value })}
                    className="input"
                  >
                    <option value="all">All</option>
                    <option value="students">Students</option>
                    <option value="teachers">Teachers</option>
                    <option value="admin">Admin</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Start Date
                  </label>
                  <input
                    type="date"
                    value={noticeData.startDate}
                    onChange={(e) => setNoticeData({ ...noticeData, startDate: e.target.value })}
                    className="input"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    End Date
                  </label>
                  <input
                    type="date"
                    value={noticeData.endDate}
                    onChange={(e) => setNoticeData({ ...noticeData, endDate: e.target.value })}
                    className="input"
                  />
                </div>
              </div>
              
              <div className="flex items-center space-x-2">
                <input
                  type="checkbox"
                  id="pinNotice"
                  className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
                />
                <label htmlFor="pinNotice" className="text-sm text-gray-700">
                  Pin this notice
                </label>
              </div>
            </div>
            <div className="flex justify-end space-x-3 mt-6">
              <button
                onClick={() => setShowNoticeModal(false)}
                className="btn btn-secondary btn-md"
              >
                Cancel
              </button>
              <button
                onClick={confirmCreateNotice}
                className="btn btn-primary btn-md"
              >
                Create Notice
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
              Are you sure you want to delete "{selectedNotice?.Title}"?
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

export default Notices;
