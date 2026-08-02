import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { CreateUserPayload, UpdateUserPayload, User, UserFilters } from "@/entities/user";
import { usersApi } from "../api/usersApi";

const USERS_KEY = "users";

/** Staff accounts matching the given filters. */
export function useUsers(filters: UserFilters = {}) {
  return useQuery({
    queryKey: [USERS_KEY, filters],
    queryFn: () => usersApi.list(filters),
    // Keeps the previous page visible while a new search runs, avoiding a flash of empty table.
    placeholderData: (previous) => previous,
  });
}

/**
 * Commands that change staff accounts. Each invalidates the list so the table reflects the
 * server's view rather than an optimistic guess — correctness matters more than instant
 * feedback for an administration screen.
 */
export function useUserMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: [USERS_KEY] });

  const create = useMutation<User, Error, CreateUserPayload>({
    mutationFn: usersApi.create,
    onSuccess: invalidate,
  });

  const update = useMutation<User, Error, { id: string; payload: UpdateUserPayload }>({
    mutationFn: ({ id, payload }) => usersApi.update(id, payload),
    onSuccess: invalidate,
  });

  const setActive = useMutation<User, Error, { id: string; isActive: boolean }>({
    mutationFn: ({ id, isActive }) => usersApi.setActive(id, isActive),
    onSuccess: invalidate,
  });

  const resetPassword = useMutation<void, Error, { id: string; newPassword: string }>({
    mutationFn: ({ id, newPassword }) => usersApi.resetPassword(id, newPassword),
    onSuccess: invalidate,
  });

  return { create, update, setActive, resetPassword };
}
