import { configureStore } from '@reduxjs/toolkit';
import authReducer from './slices/authSlice';
import studentReducer from './slices/studentSlice';
import courseReducer from './slices/courseSlice';
import enrollmentReducer from './slices/enrollmentSlice';
import feeReducer from './slices/feeSlice';
import attendanceReducer from './slices/attendanceSlice';
import academicReducer from './slices/academicSlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    student: studentReducer,
    course: courseReducer,
    enrollment: enrollmentReducer,
    fee: feeReducer,
    attendance: attendanceReducer,
    academic: academicReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        ignoredActions: ['persist/PERSIST'],
      },
    }),
});

export default store;
