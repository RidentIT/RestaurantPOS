import { useState } from "react";
import { Building2, HardDriveDownload, Info, Printer, ShieldCheck, User } from "lucide-react";
import { useRestaurantSettings } from "@/features/settings";
import { LoadingState, SegmentedTabs } from "@/shared/ui";
import { AboutSection } from "./AboutSection";
import { AccountSection } from "./AccountSection";
import { BackupSection } from "./BackupSection";
import { BusinessProfileSection } from "./BusinessProfileSection";
import { ReceiptPrintingSection } from "./ReceiptPrintingSection";
import { SecuritySection } from "./SecuritySection";

const TABS = [
  { value: "profile", label: "Business Profile", icon: <Building2 className="size-4" /> },
  { value: "account", label: "My Account", icon: <User className="size-4" /> },
  { value: "receipt", label: "Receipt & Printing", icon: <Printer className="size-4" /> },
  { value: "security", label: "Security", icon: <ShieldCheck className="size-4" /> },
  { value: "backup", label: "Backup & Restore", icon: <HardDriveDownload className="size-4" /> },
  { value: "about", label: "About", icon: <Info className="size-4" /> },
] as const;

type TabValue = (typeof TABS)[number]["value"];

/**
 * Everything about running this specific restaurant on this specific machine: who it is on a
 * receipt, what it charges, how forgiving a mistyped PIN is, and where its one safety net — a
 * local backup — lives. Nothing here talks to the cloud, because this install does not either.
 */
export default function SettingsPage() {
  const [tab, setTab] = useState<TabValue>("profile");
  const { data: settings, isLoading } = useRestaurantSettings();

  if (isLoading || !settings) {
    return <LoadingState label="Loading settings…" className="h-96" />;
  }

  return (
    <div className="space-y-6 p-8">
      <div>
        <h1 className="text-2xl font-semibold">Settings &amp; Backup</h1>
        <p className="text-sm text-muted-foreground">
          Restaurant details, tax and printer settings, and database backups.
        </p>
      </div>

      <SegmentedTabs tabs={TABS} value={tab} onValueChange={(value) => setTab(value as TabValue)} />

      {tab === "profile" && <BusinessProfileSection settings={settings} />}
      {tab === "account" && <AccountSection />}
      {tab === "receipt" && <ReceiptPrintingSection settings={settings} />}
      {tab === "security" && <SecuritySection settings={settings} />}
      {tab === "backup" && <BackupSection settings={settings} />}
      {tab === "about" && <AboutSection settings={settings} />}
    </div>
  );
}
