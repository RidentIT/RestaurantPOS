import { Link } from "react-router-dom";
import { groupModules } from "@/entities/user";
import { useAuth, useModules } from "@/features/auth";
import { MODULE_ROUTES, DEFAULT_MODULE_ICON } from "@/shared/config/moduleRoutes";
import { Card, CardContent, LoadingState } from "@/shared/ui";

export default function DashboardPage() {
  const { user, can } = useAuth();
  const { data: catalog, isLoading } = useModules();

  if (isLoading || !catalog) {
    return <LoadingState label="Loading your dashboard…" />;
  }

  const accessible = groupModules(catalog)
    .map((group) => ({ ...group, modules: group.modules.filter((m) => can(m.module)) }))
    .filter((group) => group.modules.length > 0);

  return (
    <div className="space-y-8 p-8">
      <div>
        <h1 className="text-2xl font-semibold">Welcome back, {user?.fullName?.split(" ")[0]}</h1>
        <p className="text-sm text-muted-foreground">Here's what you can open today.</p>
      </div>

      {accessible.map(({ group, modules }) => (
        <section key={group}>
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
            {group}
          </h2>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {modules.map((descriptor) => {
              const route = MODULE_ROUTES[descriptor.module];
              const Icon = route?.icon ?? DEFAULT_MODULE_ICON;
              const content = (
                <Card className="h-full transition-shadow hover:shadow-md">
                  <CardContent className="flex items-start gap-4 pt-6">
                    <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
                      <Icon className="size-5" />
                    </span>
                    <div>
                      <p className="font-medium">{descriptor.name}</p>
                      <p className="mt-0.5 text-sm text-muted-foreground">{descriptor.description}</p>
                      {!route?.path && (
                        <p className="mt-2 text-xs font-medium uppercase tracking-wide text-muted-foreground/70">
                          Coming soon
                        </p>
                      )}
                    </div>
                  </CardContent>
                </Card>
              );

              return route?.path ? (
                <Link key={descriptor.module} to={route.path}>
                  {content}
                </Link>
              ) : (
                <div key={descriptor.module} className="cursor-not-allowed opacity-70">
                  {content}
                </div>
              );
            })}
          </div>
        </section>
      ))}
    </div>
  );
}
