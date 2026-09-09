import { Outlet } from "react-router-dom";
import { useModules } from "@/features/auth";
import { useRunDailyBackupOnce } from "@/features/settings";
import { LoadingState } from "@/shared/ui";
import { Sidebar } from "./Sidebar";
import { Topbar } from "./Topbar";

/** The signed-in application frame: sidebar navigation, top bar, and the routed page. */
export function AppShell() {
  const { data: catalog, isLoading } = useModules();
  useRunDailyBackupOnce();

  if (isLoading || !catalog) {
    return <LoadingState label="Loading your workspace…" className="h-screen" />;
  }

  return (
    <div className="flex h-screen overflow-hidden">
      <Sidebar catalog={catalog} />
      <div className="flex flex-1 flex-col overflow-hidden">
        <Topbar />
        <main className="flex-1 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
