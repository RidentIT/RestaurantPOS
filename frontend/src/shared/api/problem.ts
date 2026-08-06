import { AxiosError } from "axios";

/** RFC 7807 problem body as produced by the API. */
interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  /** Stable machine-readable code, e.g. `Auth.InvalidCredentials`. */
  code?: string;
  /** Field-keyed validation messages, present on 400 responses. */
  errors?: Record<string, string[]>;
}

/**
 * A server or network failure in the shape the UI actually needs: a message safe to show, a
 * stable code to branch on, and per-field messages to attach to form inputs.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly code: string | null;
  readonly fieldErrors: Record<string, string[]>;

  constructor(message: string, status: number, code: string | null, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
    this.fieldErrors = fieldErrors;
  }

  /** True when the caller should re-authenticate. */
  get isUnauthorized(): boolean {
    return this.status === 401;
  }

  /** True when the API is refusing to serve anything until the password is changed. */
  get requiresPasswordChange(): boolean {
    return this.code === "Auth.PasswordChangeRequired";
  }

  /** First message recorded against a field, if any. */
  fieldError(field: string): string | undefined {
    const key = Object.keys(this.fieldErrors).find(
      (k) => k.toLowerCase() === field.toLowerCase(),
    );

    return key ? this.fieldErrors[key]?.[0] : undefined;
  }
}

/** Converts an axios failure into an {@link ApiError}. */
export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;

  if (error instanceof AxiosError) {
    if (!error.response) {
      return new ApiError(
        "Cannot reach the server. Check that the POS service is running.",
        0,
        "Network.Unreachable",
      );
    }

    const { status, data } = error.response;
    const problem = (data ?? {}) as ProblemDetails;

    return new ApiError(
      problem.title ?? problem.detail ?? defaultMessageFor(status),
      status,
      problem.code ?? null,
      problem.errors ?? {},
    );
  }

  return new ApiError(
    error instanceof Error ? error.message : "Something went wrong.",
    0,
    null,
  );
}

function defaultMessageFor(status: number): string {
  if (status === 401) return "Your session has expired. Please sign in again.";
  if (status === 403) return "You do not have permission to do that.";
  if (status === 404) return "That item could not be found.";
  if (status === 429) return "Too many attempts. Please wait a moment and try again.";
  if (status >= 500) return "The server ran into a problem. Please try again.";

  return "The request could not be completed.";
}
