import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import enrollmentService from '../../services/enrollmentService';

// Async thunks
export const fetchEnrollments = createAsyncThunk(
  'enrollment/fetchEnrollments',
  async (params = {}, { rejectWithValue }) => {
    try {
      const response = await enrollmentService.getEnrollments(params);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch enrollments');
    }
  }
);

export const fetchEnrollmentById = createAsyncThunk(
  'enrollment/fetchEnrollmentById',
  async (id, { rejectWithValue }) => {
    try {
      const response = await enrollmentService.getEnrollmentById(id);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch enrollment');
    }
  }
);

export const createEnrollment = createAsyncThunk(
  'enrollment/createEnrollment',
  async (enrollmentData, { rejectWithValue }) => {
    try {
      const response = await enrollmentService.createEnrollment(enrollmentData);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to create enrollment');
    }
  }
);

export const updateEnrollment = createAsyncThunk(
  'enrollment/updateEnrollment',
  async ({ id, enrollmentData }, { rejectWithValue }) => {
    try {
      const response = await enrollmentService.updateEnrollment(id, enrollmentData);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to update enrollment');
    }
  }
);

export const deleteEnrollment = createAsyncThunk(
  'enrollment/deleteEnrollment',
  async (id, { rejectWithValue }) => {
    try {
      await enrollmentService.deleteEnrollment(id);
      return id;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to delete enrollment');
    }
  }
);

const initialState = {
  enrollments: [],
  currentEnrollment: null,
  isLoading: false,
  error: null,
  pagination: {
    page: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  },
};

const enrollmentSlice = createSlice({
  name: 'enrollment',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    clearCurrentEnrollment: (state) => {
      state.currentEnrollment = null;
    },
    setPagination: (state, action) => {
      state.pagination = { ...state.pagination, ...action.payload };
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch Enrollments
      .addCase(fetchEnrollments.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchEnrollments.fulfilled, (state, action) => {
        state.isLoading = false;
        state.enrollments = action.payload.data || action.payload;
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
      .addCase(fetchEnrollments.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Fetch Enrollment by ID
      .addCase(fetchEnrollmentById.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchEnrollmentById.fulfilled, (state, action) => {
        state.isLoading = false;
        state.currentEnrollment = action.payload;
        state.error = null;
      })
      .addCase(fetchEnrollmentById.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Create Enrollment
      .addCase(createEnrollment.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(createEnrollment.fulfilled, (state, action) => {
        state.isLoading = false;
        state.enrollments.push(action.payload);
        state.error = null;
      })
      .addCase(createEnrollment.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Update Enrollment
      .addCase(updateEnrollment.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(updateEnrollment.fulfilled, (state, action) => {
        state.isLoading = false;
        const index = state.enrollments.findIndex(
          (enrollment) => enrollment.EnrollmentId === action.payload.EnrollmentId
        );
        if (index !== -1) {
          state.enrollments[index] = action.payload;
        }
        if (state.currentEnrollment?.EnrollmentId === action.payload.EnrollmentId) {
          state.currentEnrollment = action.payload;
        }
        state.error = null;
      })
      .addCase(updateEnrollment.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Delete Enrollment
      .addCase(deleteEnrollment.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(deleteEnrollment.fulfilled, (state, action) => {
        state.isLoading = false;
        state.enrollments = state.enrollments.filter(
          (enrollment) => enrollment.EnrollmentId !== action.payload
        );
        state.error = null;
      })
      .addCase(deleteEnrollment.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      });
  },
});

export const { clearError, clearCurrentEnrollment, setPagination } = enrollmentSlice.actions;
export default enrollmentSlice.reducer;
