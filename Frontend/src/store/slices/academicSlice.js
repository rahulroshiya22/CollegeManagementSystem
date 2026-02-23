import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import academicService from '../../services/academicService';

// Async thunks
export const fetchExams = createAsyncThunk(
  'academic/fetchExams',
  async (params = {}, { rejectWithValue }) => {
    try {
      const response = await academicService.getExams(params);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch exams');
    }
  }
);

export const fetchGrades = createAsyncThunk(
  'academic/fetchGrades',
  async (params = {}, { rejectWithValue }) => {
    try {
      const response = await academicService.getGrades(params);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch grades');
    }
  }
);

export const fetchNotices = createAsyncThunk(
  'academic/fetchNotices',
  async (params = {}, { rejectWithValue }) => {
    try {
      const response = await academicService.getNotices(params);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch notices');
    }
  }
);

export const createExam = createAsyncThunk(
  'academic/createExam',
  async (examData, { rejectWithValue }) => {
    try {
      const response = await academicService.createExam(examData);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to create exam');
    }
  }
);

export const createGrade = createAsyncThunk(
  'academic/createGrade',
  async (gradeData, { rejectWithValue }) => {
    try {
      const response = await academicService.createGrade(gradeData);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to create grade');
    }
  }
);

export const createNotice = createAsyncThunk(
  'academic/createNotice',
  async (noticeData, { rejectWithValue }) => {
    try {
      const response = await academicService.createNotice(noticeData);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to create notice');
    }
  }
);

export const deleteExam = createAsyncThunk(
  'academic/deleteExam',
  async (id, { rejectWithValue }) => {
    try {
      await academicService.deleteExam(id);
      return id;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to delete exam');
    }
  }
);

export const deleteGrade = createAsyncThunk(
  'academic/deleteGrade',
  async (id, { rejectWithValue }) => {
    try {
      await academicService.deleteGrade(id);
      return id;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to delete grade');
    }
  }
);

export const deleteNotice = createAsyncThunk(
  'academic/deleteNotice',
  async (id, { rejectWithValue }) => {
    try {
      await academicService.deleteNotice(id);
      return id;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to delete notice');
    }
  }
);

const initialState = {
  exams: [],
  grades: [],
  notices: [],
  timeSlots: [],
  isLoading: false,
  error: null,
  pagination: {
    page: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  },
};

const academicSlice = createSlice({
  name: 'academic',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    setPagination: (state, action) => {
      state.pagination = { ...state.pagination, ...action.payload };
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch Exams
      .addCase(fetchExams.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchExams.fulfilled, (state, action) => {
        state.isLoading = false;
        state.exams = action.payload.data || action.payload;
        if (action.payload.totalCount) {
          state.pagination = {
            page: action.payload.page || 1,
            pageSize: action.payload.pageSize || 10,
            totalCount: action.payload.totalCount,
            totalPages: action.payload.totalPages,
          };
        }
        state.error = null;
      })
      .addCase(fetchExams.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Fetch Grades
      .addCase(fetchGrades.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchGrades.fulfilled, (state, action) => {
        state.isLoading = false;
        state.grades = action.payload.data || action.payload;
        if (action.payload.totalCount) {
          state.pagination = {
            page: action.payload.page || 1,
            pageSize: action.payload.pageSize || 10,
            totalCount: action.payload.totalCount,
            totalPages: action.payload.totalPages,
          };
        }
        state.error = null;
      })
      .addCase(fetchGrades.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Fetch Notices
      .addCase(fetchNotices.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchNotices.fulfilled, (state, action) => {
        state.isLoading = false;
        state.notices = action.payload.data || action.payload;
        if (action.payload.totalCount) {
          state.pagination = {
            page: action.payload.page || 1,
            pageSize: action.payload.pageSize || 10,
            totalCount: action.payload.totalCount,
            totalPages: action.payload.totalPages,
          };
        }
        state.error = null;
      })
      .addCase(fetchNotices.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Create Exam
      .addCase(createExam.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(createExam.fulfilled, (state, action) => {
        state.isLoading = false;
        state.exams.push(action.payload);
        state.error = null;
      })
      .addCase(createExam.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Create Grade
      .addCase(createGrade.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(createGrade.fulfilled, (state, action) => {
        state.isLoading = false;
        state.grades.push(action.payload);
        state.error = null;
      })
      .addCase(createGrade.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Create Notice
      .addCase(createNotice.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(createNotice.fulfilled, (state, action) => {
        state.isLoading = false;
        state.notices.push(action.payload);
        state.error = null;
      })
      .addCase(createNotice.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Delete Exam
      .addCase(deleteExam.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(deleteExam.fulfilled, (state, action) => {
        state.isLoading = false;
        state.exams = state.exams.filter(exam => exam.ExamId !== action.payload);
        state.error = null;
      })
      .addCase(deleteExam.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Delete Grade
      .addCase(deleteGrade.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(deleteGrade.fulfilled, (state, action) => {
        state.isLoading = false;
        state.grades = state.grades.filter(grade => grade.GradeId !== action.payload);
        state.error = null;
      })
      .addCase(deleteGrade.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Delete Notice
      .addCase(deleteNotice.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(deleteNotice.fulfilled, (state, action) => {
        state.isLoading = false;
        state.notices = state.notices.filter(notice => notice.NoticeId !== action.payload);
        state.error = null;
      })
      .addCase(deleteNotice.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
    },
});

export const { clearError, setPagination } = academicSlice.actions;
export default academicSlice.reducer;
