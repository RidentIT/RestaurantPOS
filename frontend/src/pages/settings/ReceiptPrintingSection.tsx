import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import type { RestaurantSettings } from "@/entities/settings";
import { useSettingsMutations } from "@/features/settings";
import { toApiError } from "@/shared/api/problem";
import {
  Alert,
  AlertDescription,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  FormField,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/shared/ui";

const SYSTEM_DEFAULT = "__system_default__";

/** The footer message printed on every receipt, and which Windows printer bills and KOTs go to. */
export function ReceiptPrintingSection({ settings }: { settings: RestaurantSettings }) {
  const { updateReceiptFooter, updateDefaultPrinter } = useSettingsMutations();

  const [footer, setFooter] = useState(settings.receiptFooterMessage);
  useEffect(() => setFooter(settings.receiptFooterMessage), [settings.receiptFooterMessage]);

  const inDesktopShell = typeof window !== "undefined" && !!window.electronAPI?.listPrinters;

  const { data: printers, isLoading: printersLoading } = useQuery({
    queryKey: ["installed-printers"],
    queryFn: () => window.electronAPI!.listPrinters(),
    enabled: inDesktopShell,
    staleTime: 60_000,
  });

  const saveFooter = async () => {
    if (!footer.trim()) {
      toast.error("The receipt footer cannot be empty.");
      return;
    }

    try {
      await updateReceiptFooter.mutateAsync(footer.trim());
      toast.success("Receipt footer updated.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const savePrinter = async (value: string) => {
    try {
      await updateDefaultPrinter.mutateAsync(value === SYSTEM_DEFAULT ? null : value);
      toast.success("Default printer updated.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Receipt footer</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <FormField htmlFor="settings-footer" label="Message printed at the bottom of every receipt" required>
            <Input id="settings-footer" value={footer} onChange={(e) => setFooter(e.target.value)} maxLength={200} />
          </FormField>
          <Button onClick={saveFooter} loading={updateReceiptFooter.isPending}>
            Save footer
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Default printer</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {!inDesktopShell ? (
            <Alert variant="info">
              <AlertDescription>
                Printer selection is only available in the desktop app, not a browser.
              </AlertDescription>
            </Alert>
          ) : (
            <FormField htmlFor="settings-printer" label="Bills and kitchen tickets print to">
              <Select
                value={settings.defaultPrinterName ?? SYSTEM_DEFAULT}
                onValueChange={savePrinter}
                disabled={printersLoading || updateDefaultPrinter.isPending}
              >
                <SelectTrigger id="settings-printer">
                  <SelectValue placeholder="System default" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={SYSTEM_DEFAULT}>System default</SelectItem>
                  {(printers ?? []).map((name) => (
                    <SelectItem key={name} value={name}>
                      {name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </FormField>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
