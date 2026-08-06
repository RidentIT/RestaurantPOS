import * as React from "react";
import { cn } from "@/shared/lib/utils";
import { Label } from "./label";

export interface FormFieldProps {
  /** Ties the label, hint and error to the control for screen readers. */
  htmlFor: string;
  label: string;
  /** Explanatory text shown under the control while it is valid. */
  hint?: string;
  /** Validation message. When present it replaces the hint and marks the field invalid. */
  error?: string;
  required?: boolean;
  className?: string;
  children: React.ReactNode;
}

/**
 * Labelled wrapper for a single form control. Owns the id conventions for the description and
 * error elements so every form in the app announces errors the same way.
 */
export function FormField({
  htmlFor,
  label,
  hint,
  error,
  required,
  className,
  children,
}: FormFieldProps) {
  const describedBy = error ? `${htmlFor}-error` : hint ? `${htmlFor}-hint` : undefined;

  return (
    <div className={cn("space-y-2", className)}>
      <Label htmlFor={htmlFor}>
        {label}
        {required && (
          <span className="ml-0.5 text-destructive" aria-hidden="true">
            *
          </span>
        )}
      </Label>

      {React.isValidElement(children)
        ? React.cloneElement(children as React.ReactElement<Record<string, unknown>>, {
            id: htmlFor,
            "aria-invalid": error ? true : undefined,
            "aria-describedby": describedBy,
          })
        : children}

      {error ? (
        <p id={`${htmlFor}-error`} role="alert" className="text-sm font-medium text-destructive">
          {error}
        </p>
      ) : hint ? (
        <p id={`${htmlFor}-hint`} className="text-sm text-muted-foreground">
          {hint}
        </p>
      ) : null}
    </div>
  );
}
