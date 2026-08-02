import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import type { Session, User } from "@/entities/user";
import { tokenStorage } from "@/shared/api/tokenStorage";

export interface AuthState {
  user: User | null;
  /**
   * Whether the stored token has been checked against the server yet. Routing waits on this so
   * a refresh does not briefly bounce a signed-in user to the login screen.
   */
  status: "idle" | "authenticating" | "authenticated" | "unauthenticated";
}

const initialState: AuthState = {
  user: null,
  status: "idle",
};

export const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    /** Records a new session and persists its tokens. */
    sessionEstablished: (state, action: PayloadAction<Session>) => {
      tokenStorage.save(action.payload.accessToken, action.payload.refreshToken);
      state.user = action.payload.user;
      state.status = "authenticated";
    },

    /** Refreshes the cached profile without touching tokens, e.g. after `GET /auth/me`. */
    userLoaded: (state, action: PayloadAction<User>) => {
      state.user = action.payload;
      state.status = "authenticated";
    },

    authenticating: (state) => {
      state.status = "authenticating";
    },

    /** Clears all session state. Used for sign-out and for an unrecoverable 401. */
    sessionEnded: (state) => {
      tokenStorage.clear();
      state.user = null;
      state.status = "unauthenticated";
    },
  },
});

export const { sessionEstablished, userLoaded, authenticating, sessionEnded } = authSlice.actions;

export default authSlice.reducer;
