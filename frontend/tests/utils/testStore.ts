import { combineReducers, configureStore } from "@reduxjs/toolkit";
import authReducer from "@/shared/store/authSlice";
import uiReducer from "@/shared/store/uiSlice";
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
