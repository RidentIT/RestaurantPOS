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

/** The footer message printed on every receipt, and which Windows printer receipts and KOTs go to. */
export function ReceiptPrintingSection({ settings }: { settings: RestaurantSettings }) {
  const { updateReceiptFooter, updatePrinters } = useSettingsMutations();

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

  const fromSelect = (value: string) => (value === SYSTEM_DEFAULT ? null : value);

  const saveReceiptPrinter = async (value: string) => {
    try {
      await updatePrinters.mutateAsync({
        printerName: fromSelect(value),
        kitchenPrinterName: settings.kitchenPrinterName,
      });
      toast.success("Receipt printer updated.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const saveKitchenPrinter = async (value: string) => {
    try {
      await updatePrinters.mutateAsync({
        printerName: settings.defaultPrinterName,
        kitchenPrinterName: fromSelect(value),
      });
      toast.success("Kitchen printer updated.");
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
          <CardTitle>Printers</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {!inDesktopShell ? (
            <Alert variant="info">
              <AlertDescription>
                Printer selection is only available in the desktop app, not a browser.
              </AlertDescription>
            </Alert>
          ) : (
            <>
              <FormField htmlFor="settings-receipt-printer" label="Customer receipts print to">
                <Select
                  value={settings.defaultPrinterName ?? SYSTEM_DEFAULT}
                  onValueChange={saveReceiptPrinter}
                  disabled={printersLoading || updatePrinters.isPending}
                >
                  <SelectTrigger id="settings-receipt-printer">
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

              <FormField
                htmlFor="settings-kitchen-printer"
                label="Kitchen tickets (KOTs) print to"
                hint="Leave on the receipt printer if the kitchen shares one printer with the counter."
              >
                <Select
                  value={settings.kitchenPrinterName ?? SYSTEM_DEFAULT}
                  onValueChange={saveKitchenPrinter}
                  disabled={printersLoading || updatePrinters.isPending}
                >
                  <SelectTrigger id="settings-kitchen-printer">
                    <SelectValue placeholder="Same as receipt printer" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={SYSTEM_DEFAULT}>Same as receipt printer</SelectItem>
                    {(printers ?? []).map((name) => (
                      <SelectItem key={name} value={name}>
                        {name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </FormField>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
