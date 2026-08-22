import { useEffect, useState } from "react";
import { FolderOpen, HardDriveDownload, RotateCcw, Save } from "lucide-react";
import { toast } from "sonner";
import type { Backup, RestaurantSettings } from "@/entities/settings";
import { useBackupMutations, useBackups, useSettingsMutations } from "@/features/settings";
import { toApiError } from "@/shared/api/problem";
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  FormField,
  Input,
  LoadingState,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/shared/ui";
import { RestoreBackupDialog } from "./RestoreBackupDialog";
import { RestoreCompleteScreen } from "./RestoreCompleteScreen";

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function formatWhen(iso: string): string {
  const date = new Date(iso);
  return `${date.toLocaleDateString()} ${date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}`;
}

/**
 * Where backups live and how many are kept, the list of what is on disk, and the restore flow.
 *
 * This is the closest thing a purely local install has to a safety net — nothing here ever talks
 * to the cloud; a folder chosen here can simply happen to be a USB drive or something a tool like
 * OneDrive Desktop is watching.
 */
export function BackupSection({ settings }: { settings: RestaurantSettings }) {
  const { updateBackupSettings } = useSettingsMutations();
  const { data: backups, isLoading } = useBackups();
  const { createBackup } = useBackupMutations();

  const [folderPath, setFolderPath] = useState(settings.backupFolderPath ?? "");
  const [retentionCount, setRetentionCount] = useState(String(settings.backupRetentionCount));
  const [restoreTarget, setRestoreTarget] = useState<Backup | null>(null);
  const [restored, setRestored] = useState(false);

  useEffect(() => {
    setFolderPath(settings.backupFolderPath ?? "");
    setRetentionCount(String(settings.backupRetentionCount));
  }, [settings]);

  const canBrowse = typeof window !== "undefined" && !!window.electronAPI?.selectFolder;

  const chooseFolder = async () => {
    const picked = await window.electronAPI!.selectFolder();
    if (picked) setFolderPath(picked);
  };

  const saveFolderSettings = async () => {
    const retention = Number(retentionCount);

    if (!Number.isInteger(retention) || retention < 1 || retention > 60) {
      toast.error("Keep between 1 and 60 backups.");
      return;
    }

    try {
      await updateBackupSettings.mutateAsync({
        backupFolderPath: folderPath.trim() || null,
        retentionCount: retention,
      });
      toast.success("Backup settings updated.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const backupNow = async () => {
    try {
      await createBackup.mutateAsync();
      toast.success("Backup created.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  if (restored) {
    return <RestoreCompleteScreen />;
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Backup location</CardTitle>
          <CardDescription>
            Leave blank to use the default <code>Backups</code> folder beside the app.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-[1fr_auto]">
            <FormField htmlFor="settings-backup-folder" label="Folder">
              <div className="flex gap-2">
                <Input
                  id="settings-backup-folder"
                  value={folderPath}
                  onChange={(e) => setFolderPath(e.target.value)}
                  placeholder="Default Backups folder"
                  readOnly={canBrowse}
                />
                {canBrowse && (
                  <Button type="button" variant="outline" onClick={chooseFolder}>
                    <FolderOpen /> Browse
                  </Button>
                )}
              </div>
            </FormField>
            <FormField htmlFor="settings-backup-retention" label="Keep this many">
              <Input
                id="settings-backup-retention"
                inputMode="numeric"
                className="w-24"
                value={retentionCount}
                onChange={(e) => setRetentionCount(e.target.value)}
              />
            </FormField>
          </div>
          <Button onClick={saveFolderSettings} loading={updateBackupSettings.isPending}>
            <Save /> Save backup settings
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex-row items-center justify-between space-y-0">
          <div>
            <CardTitle>Backups</CardTitle>
            <CardDescription>A daily backup is also taken automatically at first sign-in.</CardDescription>
          </div>
          <Button onClick={backupNow} loading={createBackup.isPending}>
            <HardDriveDownload /> Back up now
          </Button>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <LoadingState label="Loading backups…" />
          ) : !backups || backups.length === 0 ? (
            <EmptyState
              icon={<HardDriveDownload className="size-6" />}
              title="No backups yet"
              description='Click "Back up now" to take the first one.'
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>File</TableHead>
                  <TableHead>Taken</TableHead>
                  <TableHead>Size</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {backups.map((backup) => (
                  <TableRow key={backup.fileName}>
                    <TableCell className="font-mono text-xs">{backup.fileName}</TableCell>
                    <TableCell>{formatWhen(backup.createdAtUtc)}</TableCell>
                    <TableCell>{formatSize(backup.sizeBytes)}</TableCell>
                    <TableCell className="text-right">
                      <Button variant="outline" size="sm" onClick={() => setRestoreTarget(backup)}>
                        <RotateCcw /> Restore
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <RestoreBackupDialog
        backup={restoreTarget}
        onOpenChange={(open) => !open && setRestoreTarget(null)}
        onRestored={() => setRestored(true)}
      />
    </div>
  );
}
