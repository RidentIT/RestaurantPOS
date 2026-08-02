import { useState } from "react";
import {
  KeyRound,
  MoreHorizontal,
  Pencil,
  Plus,
  Search,
  ShieldOff,
  ShieldCheck,
  Users as UsersIcon,
} from "lucide-react";
import { toast } from "sonner";
import type { User, UserFilters, UserRole } from "@/entities/user";
import { useAuth, useModules } from "@/features/auth";
import { ResetPasswordDialog, UserFormDialog, useUserMutations, useUsers } from "@/features/users";
import { toApiError } from "@/shared/api/problem";
import {
  Badge,
  Button,
  Card,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  EmptyState,
  Input,
  LoadingState,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/shared/ui";

const ROLE_FILTER_ALL = "all";
const STATUS_FILTER_ALL = "all";

export default function UsersPage() {
  const { user: me } = useAuth();
  const { data: catalog } = useModules();

  const [search, setSearch] = useState("");
  const [roleFilter, setRoleFilter] = useState<string>(ROLE_FILTER_ALL);
  const [statusFilter, setStatusFilter] = useState<string>(STATUS_FILTER_ALL);

  const filters: UserFilters = {
    search: search || undefined,
    role: roleFilter === ROLE_FILTER_ALL ? undefined : (roleFilter as UserRole),
    isActive: statusFilter === STATUS_FILTER_ALL ? undefined : statusFilter === "active",
  };

  const { data: users, isLoading } = useUsers(filters);
  const { setActive } = useUserMutations();

  const [formUser, setFormUser] = useState<User | null | undefined>(undefined);
  const [resetTarget, setResetTarget] = useState<User | null>(null);

  const toggleActive = async (target: User) => {
    try {
      await setActive.mutateAsync({ id: target.id, isActive: !target.isActive });
      toast.success(
        target.isActive ? `${target.fullName} was deactivated.` : `${target.fullName} was reactivated.`,
      );
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <div className="space-y-6 p-8">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">User Management & Roles</h1>
          <p className="text-sm text-muted-foreground">
            Create staff accounts and control which modules they can open.
          </p>
        </div>
        <Button onClick={() => setFormUser(null)}>
          <Plus /> Add staff account
        </Button>
      </div>

      <div className="flex flex-wrap gap-3">
        <div className="relative w-full max-w-xs">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by name or username…"
            className="pl-9"
          />
        </div>

        <Select value={roleFilter} onValueChange={setRoleFilter}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder="Role" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ROLE_FILTER_ALL}>All roles</SelectItem>
            <SelectItem value="Admin">Admin</SelectItem>
            <SelectItem value="User">User</SelectItem>
          </SelectContent>
        </Select>

        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder="Status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={STATUS_FILTER_ALL}>All statuses</SelectItem>
            <SelectItem value="active">Active</SelectItem>
            <SelectItem value="inactive">Deactivated</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <Card>
        {isLoading ? (
          <LoadingState label="Loading staff accounts…" />
        ) : !users || users.length === 0 ? (
          <EmptyState
            icon={<UsersIcon className="size-6" />}
            title="No staff accounts match your filters"
            description="Try clearing the search or filters, or add a new account."
          />
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Role</TableHead>
                <TableHead>Modules</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Last sign-in</TableHead>
                <TableHead className="w-12" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {users.map((row) => (
                <TableRow key={row.id}>
                  <TableCell>
                    <p className="font-medium">{row.fullName}</p>
                    <p className="text-xs text-muted-foreground">@{row.username}</p>
                  </TableCell>
                  <TableCell>
                    <Badge variant={row.role === "Admin" ? "default" : "secondary"}>{row.role}</Badge>
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {row.role === "Admin" ? "All modules" : `${row.modules.length} granted`}
                  </TableCell>
                  <TableCell>
                    {row.isActive ? (
                      <Badge variant="success">Active</Badge>
                    ) : (
                      <Badge variant="destructive">Deactivated</Badge>
                    )}
                    {row.mustChangePassword && (
                      <Badge variant="warning" className="ml-1.5">
                        Pending reset
                      </Badge>
                    )}
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {row.lastLoginAtUtc ? new Date(row.lastLoginAtUtc).toLocaleString() : "Never"}
                  </TableCell>
                  <TableCell>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="icon">
                          <MoreHorizontal className="size-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onSelect={() => setFormUser(row)}>
                          <Pencil /> Edit
                        </DropdownMenuItem>
                        <DropdownMenuItem onSelect={() => setResetTarget(row)}>
                          <KeyRound /> Reset password
                        </DropdownMenuItem>
                        {row.id !== me?.id && !row.isSystemAdmin && (
                          <DropdownMenuItem destructive={row.isActive} onSelect={() => toggleActive(row)}>
                            {row.isActive ? (
                              <>
                                <ShieldOff /> Deactivate
                              </>
                            ) : (
                              <>
                                <ShieldCheck /> Reactivate
                              </>
                            )}
                          </DropdownMenuItem>
                        )}
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Card>

      <UserFormDialog
        open={formUser !== undefined}
        onOpenChange={(open) => !open && setFormUser(undefined)}
        user={formUser ?? undefined}
        catalog={catalog ?? []}
      />

      {resetTarget && (
        <ResetPasswordDialog
          open={!!resetTarget}
          onOpenChange={(open) => !open && setResetTarget(null)}
          user={resetTarget}
        />
      )}
    </div>
  );
}
