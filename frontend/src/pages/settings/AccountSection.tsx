import { ArrowRight, KeyRound, ShieldCheck, User } from "lucide-react";
import { Link } from "react-router-dom";
import { useAuth } from "@/features/auth";
import { Badge, Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/shared/ui";

/**
 * A quick way into the account screen from inside Settings, alongside the top-right dropdown —
 * an administrator poking around Settings should not have to hunt for where their own password
 * or approval PIN live.
 */
export function AccountSection() {
  const { user } = useAuth();

  if (!user) return null;

  return (
    <div className="max-w-xl">
      <Card>
        <CardHeader>
          <CardTitle>My account</CardTitle>
          <CardDescription>Your own sign-in details, password and approval PIN.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center gap-3 rounded-lg border p-4">
            <span className="flex size-10 items-center justify-center rounded-full bg-primary/10 text-primary">
              <User className="size-5" />
            </span>
            <div className="min-w-0 flex-1">
              <p className="font-medium">{user.fullName}</p>
              <p className="text-sm text-muted-foreground">@{user.username}</p>
            </div>
            {user.role === "Admin" && (
              <Badge variant="default" className="gap-1">
                <ShieldCheck className="size-3" />
                Admin
              </Badge>
            )}
          </div>

          <Link
            to="/account"
            className="flex items-center justify-between rounded-lg border p-4 text-sm font-medium transition-colors hover:bg-accent"
          >
            <span className="flex items-center gap-2">
              <KeyRound className="size-4 text-muted-foreground" />
              Manage password{user.role === "Admin" ? " and approval PIN" : ""}
            </span>
            <ArrowRight className="size-4 text-muted-foreground" />
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}
