import { ShieldCheck } from "lucide-react";
import type { ModuleDescriptor, ModuleKey } from "@/entities/user";
import { assignableModules, groupModules } from "@/entities/user";
import { cn } from "@/shared/lib/utils";
import { Alert, AlertDescription, Button, Checkbox, Label } from "@/shared/ui";

export interface ModulePermissionPickerProps {
  catalog: ModuleDescriptor[];
  selected: ModuleKey[];
  onChange: (modules: ModuleKey[]) => void;
  /** Administrators hold every module implicitly, so the picker is replaced by an explanation. */
  isAdmin: boolean;
  disabled?: boolean;
}

/**
 * Grants modules to a staff account.
 *
 * Only non-administrative modules are offered: `UserManagement` and `SystemSettings` carry
 * administrative authority and come with the Admin role instead of being handed out one by one.
 */
export function ModulePermissionPicker({
  catalog,
  selected,
  onChange,
  isAdmin,
  disabled = false,
}: ModulePermissionPickerProps) {
  const grantable = assignableModules(catalog);
  const groups = groupModules(grantable);

  if (isAdmin) {
    return (
      <Alert variant="info">
        <ShieldCheck />
        <AlertDescription>
          Administrators can open every module, including user management and system settings.
          Module selection does not apply to this role.
        </AlertDescription>
      </Alert>
    );
  }

  const toggle = (module: ModuleKey, checked: boolean) => {
    onChange(checked ? [...selected, module] : selected.filter((m) => m !== module));
  };

  const toggleGroup = (modules: ModuleDescriptor[], grantAll: boolean) => {
    const keys = modules.map((m) => m.module);

    onChange(
      grantAll
        ? [...new Set([...selected, ...keys])]
        : selected.filter((m) => !keys.includes(m)),
    );
  };

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">
          {selected.length === 0
            ? "No modules selected — this user will be able to sign in but not open anything."
            : `${selected.length} of ${grantable.length} modules granted`}
        </p>

        {selected.length > 0 && !disabled && (
          <Button type="button" variant="ghost" size="sm" onClick={() => onChange([])}>
            Clear all
          </Button>
        )}
      </div>

      {groups.map(({ group, modules }) => {
        const allGranted = modules.every((m) => selected.includes(m.module));

        return (
          <fieldset key={group} className="space-y-2">
            <div className="flex items-center justify-between">
              <legend className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                {group}
              </legend>
              {!disabled && (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="h-7 text-xs"
                  onClick={() => toggleGroup(modules, !allGranted)}
                >
                  {allGranted ? "Deselect group" : "Select group"}
                </Button>
              )}
            </div>

            <div className="grid gap-2 sm:grid-cols-2">
              {modules.map((descriptor) => {
                const checked = selected.includes(descriptor.module);
                const id = `module-${descriptor.module}`;

                return (
                  <label
                    key={descriptor.module}
                    htmlFor={id}
                    className={cn(
                      "flex cursor-pointer gap-3 rounded-lg border p-3 transition-colors",
                      checked ? "border-primary/40 bg-primary/5" : "hover:bg-muted/50",
                      disabled && "cursor-not-allowed opacity-60",
                    )}
                  >
                    <Checkbox
                      id={id}
                      checked={checked}
                      disabled={disabled}
                      onCheckedChange={(value) => toggle(descriptor.module, value === true)}
                      className="mt-0.5"
                    />
                    <span className="space-y-0.5">
                      <Label htmlFor={id} className="cursor-pointer">
                        {descriptor.name}
                      </Label>
                      <p className="text-xs leading-snug text-muted-foreground">
                        {descriptor.description}
                      </p>
                    </span>
                  </label>
                );
              })}
            </div>
          </fieldset>
        );
      })}
    </div>
  );
}
