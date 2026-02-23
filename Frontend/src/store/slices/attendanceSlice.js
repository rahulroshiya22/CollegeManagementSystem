import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import attendanceService from '../../services/attendanceService';

// Async thunks
export const fetchAttendanceRecords = createAsyncThunk(
  'attendance/fetchAttendanceRecords',
  async (params = {}, { rejectWithValue }) => {
    try {
      const response = await attendanceService.getAttendanceRecords(params);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch attendance records');
    }
  }
);

export const fetchAttendanceById = createAsyncThunk(
  'attendance/fetchAttendanceById',
  async (id, { rejectWithValue }) => {
    try {
      const response = await attendanceService.getAttendanceById(id);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch attendance record');
    }
  }
);

export const markAttendance = createAsyncThunk(
  'attendance/markAttendance',
  async (attendanceData, { rejectWithValue }) => {
    try {
      const response = await attendanceService.markAttendance(attendanceData);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to mark attendance');
    }
  }
);

export const updateAttendance = createAsyncThunk(
  'attendance/updateAttendance',
  async ({ id, attendanceData }, { rejectWithValue }) => {
    try {
      const response = await attendanceService.updateAttendance(id, attendanceData);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to update attendance');
    }
  }
);

export const deleteAttendance = createAsyncThunk(
  'attendance/deleteAttendance',
  async (id, { rejectWithValue }) => {
    try {
      await attendanceService.deleteAttendance(id);
      return id;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to delete attendance record');
    }
  }
);

export const getAttendanceReport = createAsyncThunk(
  'attendance/getAttendanceReport',
  async (params = {}, { rejectWithValue }) => {
    try {
      const response = await attendanceService.getAttendanceReport(params);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to generate attendance report');
    }
  }
);

const initialState = {
  attendanceRecords: [],
  currentAttendance: null,
  report: null,
  isLoading: false,
  error: null,
  pagination: {
    page: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  },
  summary: {
    totalPresent: 0,
    totalAbsent: 0,
    totalLate: 0,
    attendanceRate: 0,
  },
};

const attendanceSlice = createSlice({
  name: 'attendance',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    clearCurrentAttendance: (state) => {
      state.currentAttendance = null;
    },
    clearReport: (state) => {
      state.report = null;
    },
    setPagination: (state, action) => {
      state.pagination = { ...state.pagination, ...action.payload };
    },
    setSummary: (state, action) => {
      state.summary = { ...state.summary, ...action.payload };
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch Attendance Records
      .addCase(fetchAttendanceRecords.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchAttendanceRecords.fulfilled, (state, action) => {
        state.isLoading = false;
        state.attendanceRecords = action.payload.data || action.payload;
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
      .addCase(fetchAttendanceRecords.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Fetch Attendance by ID
      .addCase(fetchAttendanceById.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchAttendanceById.fulfilled, (state, action) => {
        state.isLoading = false;
        state.currentAttendance = action.payload;
        state.error = null;
      })
      .addCase(fetchAttendanceById.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Mark Attendance
      .addCase(markAttendance.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(markAttendance.fulfilled, (state, action) => {
        state.isLoading = false;
        state.attendanceRecords.push(action.payload);
        state.error = null;
      })
      .addCase(markAttendance.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Update Attendance
      .addCase(updateAttendance.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(updateAttendance.fulfilled, (state, action) => {
        state.isLoading = false;
        const index = state.attendanceRecords.findIndex(
          (record) => record.AttendanceId === action.payload.AttendanceId
        );
        if (index !== -1) {
          state.attendanceRecords[index] = action.payload;
        }
        if (state.currentAttendance?.AttendanceId === action.payload.AttendanceId) {
          state.currentAttendance = action.payload;
        }
        state.error = null;
      })
      .addCase(updateAttendance.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Delete Attendance
      .addCase(deleteAttendance.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(deleteAttendance.fulfilled, (state, action) => {
        state.isLoading = false;
        state.attendanceRecords = state.attendanceRecords.filter(
          (record) => record.AttendanceId !== action.payload
        );
        state.error = null;
      })
      .addCase(deleteAttendance.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Get Attendance Report
      .addCase(getAttendanceReport.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(getAttendanceReport.fulfilled, (state, action) => {
        state.isLoading = false;
        state.report = action.payload;
        state.error = null;
      })
      .addCase(getAttendanceReport.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      });
  },
});

export const { clearError, clearCurrentAttendance, clearReport, setPagination, setSummary } = attendanceSlice.actions;
export default attendanceSlice.reducer;
