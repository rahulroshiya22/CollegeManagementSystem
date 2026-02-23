import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import feeService from '../../services/feeService';

// Async thunks
export const fetchFees = createAsyncThunk(
  'fee/fetchFees',
  async (params = {}, { rejectWithValue }) => {
    try {
      const response = await feeService.getFees(params);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch fees');
    }
  }
);

export const fetchFeeById = createAsyncThunk(
  'fee/fetchFeeById',
  async (id, { rejectWithValue }) => {
    try {
      const response = await feeService.getFeeById(id);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch fee');
    }
  }
);

export const createFee = createAsyncThunk(
  'fee/createFee',
  async (feeData, { rejectWithValue }) => {
    try {
      const response = await feeService.createFee(feeData);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to create fee');
    }
  }
);

export const updateFee = createAsyncThunk(
  'fee/updateFee',
  async ({ id, feeData }, { rejectWithValue }) => {
    try {
      const response = await feeService.updateFee(id, feeData);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to update fee');
    }
  }
);

export const deleteFee = createAsyncThunk(
  'fee/deleteFee',
  async (id, { rejectWithValue }) => {
    try {
      await feeService.deleteFee(id);
      return id;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to delete fee');
    }
  }
);

export const processPayment = createAsyncThunk(
  'fee/processPayment',
  async (paymentData, { rejectWithValue }) => {
    try {
      const response = await feeService.processPayment(paymentData);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to process payment');
    }
  }
);

const initialState = {
  fees: [],
  currentFee: null,
  payments: [],
  isLoading: false,
  error: null,
  pagination: {
    page: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  },
  summary: {
    totalFees: 0,
    paidFees: 0,
    pendingFees: 0,
    totalRevenue: 0,
  },
};

const feeSlice = createSlice({
  name: 'fee',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    clearCurrentFee: (state) => {
      state.currentFee = null;
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
      // Fetch Fees
      .addCase(fetchFees.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchFees.fulfilled, (state, action) => {
        state.isLoading = false;
        state.fees = action.payload.data || action.payload;
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
      .addCase(fetchFees.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Fetch Fee by ID
      .addCase(fetchFeeById.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchFeeById.fulfilled, (state, action) => {
        state.isLoading = false;
        state.currentFee = action.payload;
        state.error = null;
      })
      .addCase(fetchFeeById.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Create Fee
      .addCase(createFee.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(createFee.fulfilled, (state, action) => {
        state.isLoading = false;
        state.fees.push(action.payload);
        state.error = null;
      })
      .addCase(createFee.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Update Fee
      .addCase(updateFee.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(updateFee.fulfilled, (state, action) => {
        state.isLoading = false;
        const index = state.fees.findIndex(
          (fee) => fee.FeeId === action.payload.FeeId
        );
        if (index !== -1) {
          state.fees[index] = action.payload;
        }
        if (state.currentFee?.FeeId === action.payload.FeeId) {
          state.currentFee = action.payload;
        }
        state.error = null;
      })
      .addCase(updateFee.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Delete Fee
      .addCase(deleteFee.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(deleteFee.fulfilled, (state, action) => {
        state.isLoading = false;
        state.fees = state.fees.filter(
          (fee) => fee.FeeId !== action.payload
        );
        state.error = null;
      })
      .addCase(deleteFee.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      })
      // Process Payment
      .addCase(processPayment.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(processPayment.fulfilled, (state, action) => {
        state.isLoading = false;
        state.payments.push(action.payload);
        state.error = null;
      })
      .addCase(processPayment.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      });
  },
});

export const { clearError, clearCurrentFee, setPagination, setSummary } = feeSlice.actions;
export default feeSlice.reducer;
