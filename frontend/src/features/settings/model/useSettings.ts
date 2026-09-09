import { useEffect, useRef } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/features/auth";
import { settingsApi } from "../api/settingsApi";

export const SETTINGS_KEY = "restaurant-settings";
export const BACKUPS_KEY = "backups";

export function useRestaurantSettings() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: [SETTINGS_KEY],
    queryFn: settingsApi.get,
    enabled: isAuthenticated,
  });
}

/** The restaurant's name for the sign-in screen — the one thing here that needs no session. */
export function useBranding() {
  return useQuery({
    queryKey: ["restaurant-branding"],
    queryFn: settingsApi.getBranding,
    staleTime: 5 * 60_000,
    retry: false,
  });
}

/** Every "update one section" mutation the Settings page needs, all refreshing the same query. */
export function useSettingsMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: [SETTINGS_KEY] });
    queryClient.invalidateQueries({ queryKey: ["restaurant-branding"] });
  };

  const updateProfile = useMutation({
    mutationFn: settingsApi.updateProfile,
    onSuccess: invalidate,
  });

  const uploadLogo = useMutation({
    mutationFn: (file: File) => settingsApi.uploadLogo(file),
    onSuccess: invalidate,
  });

  const removeLogo = useMutation({
    mutationFn: settingsApi.removeLogo,
    onSuccess: invalidate,
  });

  const updateBillCharges = useMutation({
    mutationFn: ({ taxRatePercent, serviceChargeRatePercent }: { taxRatePercent: number; serviceChargeRatePercent: number }) =>
      settingsApi.updateBillCharges(taxRatePercent, serviceChargeRatePercent),
    onSuccess: invalidate,
  });

  const updateReceiptFooter = useMutation({
    mutationFn: (message: string) => settingsApi.updateReceiptFooter(message),
    onSuccess: invalidate,
  });

  const updateDefaultPrinter = useMutation({
    mutationFn: (printerName: string | null) => settingsApi.updateDefaultPrinter(printerName),
    onSuccess: invalidate,
  });

  const updateApprovalPinPolicy = useMutation({
    mutationFn: ({ maxAttempts, lockoutMinutes }: { maxAttempts: number; lockoutMinutes: number }) =>
      settingsApi.updateApprovalPinPolicy(maxAttempts, lockoutMinutes),
    onSuccess: invalidate,
  });

  const updateBackupSettings = useMutation({
    mutationFn: ({ backupFolderPath, retentionCount }: { backupFolderPath: string | null; retentionCount: number }) =>
      settingsApi.updateBackupSettings(backupFolderPath, retentionCount),
    onSuccess: invalidate,
  });

  return {
    updateProfile,
    uploadLogo,
    removeLogo,
    updateBillCharges,
    updateReceiptFooter,
    updateDefaultPrinter,
    updateApprovalPinPolicy,
    updateBackupSettings,
  };
}

export function useBackups() {
  const { isAdmin } = useAuth();

  return useQuery({
    queryKey: [BACKUPS_KEY],
    queryFn: settingsApi.listBackups,
    enabled: isAdmin,
  });
}

export function useBackupMutations() {
  const queryClient = useQueryClient();

  const createBackup = useMutation({
    mutationFn: settingsApi.createBackup,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [BACKUPS_KEY] }),
  });

  const restoreBackup = useMutation({
    mutationFn: ({ fileName, pin, confirmationText }: { fileName: string; pin: string; confirmationText: string }) =>
      settingsApi.restoreBackup(fileName, pin, confirmationText),
  });

  return { createBackup, restoreBackup };
}

/**
 * Takes today's backup once per app session, right after sign-in.
 *
 * The nightly-scheduler this would normally be does not fit a till that is switched off overnight,
 * so this stands in for one the same way notification evaluation and recurring expenses do:
 * triggered by the app being opened rather than by a clock. The API itself only actually creates
 * an archive the first time this fires on a given day — calling it again here changes nothing.
 */
export function useRunDailyBackupOnce(): void {
  const { isAuthenticated, mustChangePassword } = useAuth();
  const firedRef = useRef(false);

  useEffect(() => {
    if (!isAuthenticated || mustChangePassword || firedRef.current) {
      return;
    }

    firedRef.current = true;
    settingsApi.runDailyBackup().catch(() => {
      // A missed backup on this particular sign-in is not worth interrupting anyone over — the
      // next person to sign in today, or the admin taking one manually, covers it.
      firedRef.current = false;
    });
  }, [isAuthenticated, mustChangePassword]);
}
