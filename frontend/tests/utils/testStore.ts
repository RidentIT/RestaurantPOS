import { combineReducers, configureStore } from "@reduxjs/toolkit";
import authReducer from "@/features/auth/model/authSlice";
import uiReducer from "@/entities/ui/model/uiSlice";
import { RootState } from "@/shared/store";

const rootReducer = combineReducers({
  auth: authReducer,
  ui: uiReducer,
});

export function createTestStore(preloadedState?: Partial<RootState>) {
  return configureStore({
    reducer: rootReducer,
    preloadedState: preloadedState as any,
  });
}
