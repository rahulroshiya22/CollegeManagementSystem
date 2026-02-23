import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import studentService from '../../services/studentService';

// Async thunks
export const fetchStudents = createAsyncThunk(
  'student/fetchStudents',
  async (params = {}, { rejectWithValue }) => {
    try {
      const response = await studentService.getStudents(params);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch students');
    }
  }
);

export const fetchStudentById = createAsyncThunk(
  'student/fetchStudentById',
  async (id, { rejectWithValue }) => {
    try {
      const response = await studentService.getStudentById(id);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch student');
    }
  }
);

export const createStudent = createAsyncThunk(
  'student/createStudent',
  async (studentData, { rejectWithValue }) => {
    try {
      const response = await studentService.createStudent(studentData);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to create student');
    }
  }
);

export const updateStudent = createAsyncThunk(
  'student/updateStudent',
  async ({ id, studentData }, { rejectWithValue }) => {
    try {
      const response = await studentService.updateStudent(id, studentData);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to update student');
    }
  }
);

export const deleteStudent = createAsyncThunk(
  'student/deleteStudent',
  async (id, { rejectWithValue }) => {
    try {
      await studentService.deleteStudent(id);
      return id;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to delete student');
    }
  }
);

const initialState = {
  students: [],
  currentStudent: null,
  isLoading: false,
  error: null,
  pagination: {
    page: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  },
};

const studentSlice = createSlice({
  name: 'student',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    clearCurrentStudent: (state) => {
      state.currentStudent = null;
    },
    setPagination: (state, action) => {
      state.pagination = { ...state.pagination, ...action.payload };
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch Students
      .addCase(fetchStudents.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchStudents.fulfilled, (state, action) => {
        state.isLoading = false;
        state.students = action.payload.data;
        state.pagination = {
          page: action.payload.page,
          pageSize: action.payload.pageSize,
          totalCount: action.payload.totalCount,
          totalPages: action.payload.totalPages,
        };
        state.error = null;
      })
      .addCase(fetchStudents.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Fetch Student by ID
      .addCase(fetchStudentById.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchStudentById.fulfilled, (state, action) => {
        state.isLoading = false;
        state.currentStudent = action.payload;
        state.error = null;
      })
      .addCase(fetchStudentById.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Create Student
      .addCase(createStudent.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(createStudent.fulfilled, (state, action) => {
        state.isLoading = false;
        state.students.push(action.payload);
        state.error = null;
      })
      .addCase(createStudent.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Update Student
      .addCase(updateStudent.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(updateStudent.fulfilled, (state, action) => {
        state.isLoading = false;
        const index = state.students.findIndex(
          (student) => student.StudentId === action.payload.StudentId
        );
        if (index !== -1) {
          state.students[index] = action.payload;
        }
        if (state.currentStudent?.StudentId === action.payload.StudentId) {
          state.currentStudent = action.payload;
        }
        state.error = null;
      })
      .addCase(updateStudent.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Delete Student
      .addCase(deleteStudent.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(deleteStudent.fulfilled, (state, action) => {
        state.isLoading = false;
        state.students = state.students.filter(
          (student) => student.StudentId !== action.payload
        );
        state.error = null;
      })
      .addCase(deleteStudent.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      });
  },
});

export const { clearError, clearCurrentStudent, setPagination } = studentSlice.actions;
export default studentSlice.reducer;
