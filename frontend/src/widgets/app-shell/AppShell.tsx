import { useEffect, useState } from "react";
import { Outlet } from "react-router-dom";
import { useModules } from "@/features/auth";
import { useRunDailyBackupOnce } from "@/features/settings";
import { LoadingState } from "@/shared/ui";
import { Sidebar } from "./Sidebar";
import { Topbar } from "./Topbar";

const SIDEBAR_COLLAPSED_KEY = "sidebar-collapsed";

/**
 * Whether the sidebar starts collapsed, remembered across sign-ins — a POS terminal is a small,
 * fixed screen, so once someone folds the sidebar away to fit more of the till on it, it should
 * stay folded rather than reclaiming that space back every time the app opens.
 */
function readStoredCollapsed(): boolean {
  try {
    return localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === "true";
  } catch {
    return false;
  }
}

/** The signed-in application frame: sidebar navigation, top bar, and the routed page. */
export function AppShell() {
  const { data: catalog, isLoading } = useModules();
  useRunDailyBackupOnce();

  const [sidebarCollapsed, setSidebarCollapsed] = useState(readStoredCollapsed);

  useEffect(() => {
    try {
      localStorage.setItem(SIDEBAR_COLLAPSED_KEY, String(sidebarCollapsed));
    } catch {
      // A private window or blocked storage just means the choice doesn't survive a restart.
    }
  }, [sidebarCollapsed]);

  if (isLoading || !catalog) {
    return <LoadingState label="Loading your workspace…" className="h-screen" />;
  }

  return (
    <div className="flex h-screen overflow-hidden">
      <Sidebar catalog={catalog} collapsed={sidebarCollapsed} />
      <div className="flex flex-1 flex-col overflow-hidden">
        <Topbar
          sidebarCollapsed={sidebarCollapsed}
          onToggleSidebar={() => setSidebarCollapsed((collapsed) => !collapsed)}
        />
        <main className="flex-1 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
