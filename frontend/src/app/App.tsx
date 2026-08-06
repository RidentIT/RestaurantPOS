import { useEffect } from "react";
import { RouterProvider } from "react-router-dom";
import { Toaster } from "sonner";
import { sessionEnded, useRestoreSession } from "@/features/auth";
import { onSessionExpired } from "@/shared/api/axiosClient";
import { useAppDispatch } from "@/shared/store";
import { router } from "./routing/router";

export default function App() {
  useRestoreSession();

  const dispatch = useAppDispatch();

  // When a refresh token can no longer be renewed, the interceptor calls this to clear the
  // session; RequireAuth then redirects to /login on its own next render.
  useEffect(() => onSessionExpired(() => dispatch(sessionEnded())), [dispatch]);

  return (
    <>
      <RouterProvider router={router} />
      <Toaster position="top-right" richColors closeButton />
    </>
  );
}
