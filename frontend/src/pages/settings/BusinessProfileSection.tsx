import { useEffect, useRef, useState, type ChangeEvent } from "react";
import { toast } from "sonner";
import type { RestaurantSettings } from "@/entities/settings";
import { settingsApi, useSettingsMutations } from "@/features/settings";
import { toApiError } from "@/shared/api/problem";
import { BrandMark, Button, Card, CardContent, CardHeader, CardTitle, FormField, Input } from "@/shared/ui";

/** Business details printed on every receipt and KOT (POS-027), plus what the bill adds on top. */
export function BusinessProfileSection({ settings }: { settings: RestaurantSettings }) {
  const { updateProfile, updateBillCharges, uploadLogo, removeLogo } = useSettingsMutations();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [name, setName] = useState(settings.name);
  const [addressLine1, setAddressLine1] = useState(settings.addressLine1);
  const [addressLine2, setAddressLine2] = useState(settings.addressLine2 ?? "");
  const [city, setCity] = useState(settings.city ?? "");
  const [phone, setPhone] = useState(settings.phone ?? "");
  const [vatRegistrationNumber, setVatRegistrationNumber] = useState(settings.vatRegistrationNumber ?? "");

  const [taxRatePercent, setTaxRatePercent] = useState(String(settings.taxRatePercent));
  const [serviceChargeRatePercent, setServiceChargeRatePercent] = useState(
    String(settings.serviceChargeRatePercent),
  );

  useEffect(() => {
    setName(settings.name);
    setAddressLine1(settings.addressLine1);
    setAddressLine2(settings.addressLine2 ?? "");
    setCity(settings.city ?? "");
    setPhone(settings.phone ?? "");
    setVatRegistrationNumber(settings.vatRegistrationNumber ?? "");
    setTaxRatePercent(String(settings.taxRatePercent));
    setServiceChargeRatePercent(String(settings.serviceChargeRatePercent));
  }, [settings]);

  const saveProfile = async () => {
    if (!name.trim() || !addressLine1.trim()) {
      toast.error("Name and address are required.");
      return;
    }

    try {
      await updateProfile.mutateAsync({
        name: name.trim(),
        addressLine1: addressLine1.trim(),
        addressLine2: addressLine2.trim() || null,
        city: city.trim() || null,
        phone: phone.trim() || null,
        logoPath: settings.logoPath,
        vatRegistrationNumber: vatRegistrationNumber.trim() || null,
      });
      toast.success("Business profile updated.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const pickLogo = () => fileInputRef.current?.click();

  const onLogoChosen = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = ""; // lets the same file be picked again after an error
    if (!file) return;

    try {
      await uploadLogo.mutateAsync(file);
      toast.success("Logo updated.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const clearLogo = async () => {
    try {
      await removeLogo.mutateAsync();
      toast.success("Logo removed.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const saveBillCharges = async () => {
    const tax = Number(taxRatePercent);
    const service = Number(serviceChargeRatePercent);

    if (!Number.isFinite(tax) || tax < 0 || tax > 100 || !Number.isFinite(service) || service < 0 || service > 100) {
      toast.error("Enter a percentage between 0 and 100 for each.");
      return;
    }

    try {
      await updateBillCharges.mutateAsync({ taxRatePercent: tax, serviceChargeRatePercent: service });
      toast.success("Bill charges updated. This applies to new orders only.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Business details</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <FormField htmlFor="settings-logo" label="Logo" hint="JPEG, PNG or WebP, up to 2 MB. Shown on the sidebar, sign-in screen and every receipt.">
            <div className="flex items-center gap-4">
              <BrandMark
                src={settings.logoPath ? `${settingsApi.logoUrl()}?v=${encodeURIComponent(settings.logoPath)}` : undefined}
                name={settings.name}
                className="size-14 shrink-0 text-lg"
              />
              <div className="flex gap-2">
                <Button type="button" variant="outline" onClick={pickLogo} loading={uploadLogo.isPending}>
                  {settings.logoPath ? "Change logo" : "Upload logo"}
                </Button>
                {settings.logoPath && (
                  <Button type="button" variant="ghost" onClick={clearLogo} loading={removeLogo.isPending}>
                    Remove
                  </Button>
                )}
              </div>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                className="hidden"
                aria-label="Choose a logo image"
                onChange={onLogoChosen}
              />
            </div>
          </FormField>

          <div className="grid gap-4 sm:grid-cols-2">
            <FormField htmlFor="settings-name" label="Restaurant name" required>
              <Input id="settings-name" value={name} onChange={(e) => setName(e.target.value)} />
            </FormField>
            <FormField htmlFor="settings-phone" label="Phone">
              <Input id="settings-phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
            </FormField>
            <FormField htmlFor="settings-address1" label="Address line 1" required>
              <Input id="settings-address1" value={addressLine1} onChange={(e) => setAddressLine1(e.target.value)} />
            </FormField>
            <FormField htmlFor="settings-address2" label="Address line 2">
              <Input id="settings-address2" value={addressLine2} onChange={(e) => setAddressLine2(e.target.value)} />
            </FormField>
            <FormField htmlFor="settings-city" label="City">
              <Input id="settings-city" value={city} onChange={(e) => setCity(e.target.value)} />
            </FormField>
            <FormField
              htmlFor="settings-vat-number"
              label="VAT registration number"
              hint="Optional — printed on the receipt under the address when set."
            >
              <Input
                id="settings-vat-number"
                value={vatRegistrationNumber}
                onChange={(e) => setVatRegistrationNumber(e.target.value)}
              />
            </FormField>
          </div>
          <Button onClick={saveProfile} loading={updateProfile.isPending}>
            Save business details
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Tax &amp; service charge</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-sm text-muted-foreground">
            Both default to 0%, which is how this restaurant currently prices — VAT and any other
            charges are already built into each dish. Set a rate here only if that changes. A rate
            change never affects a bill already open; it only applies to orders opened afterwards.
          </p>
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField htmlFor="settings-tax" label="Tax / VAT rate">
              <div className="relative">
                <Input
                  id="settings-tax"
                  inputMode="decimal"
                  value={taxRatePercent}
                  onChange={(e) => setTaxRatePercent(e.target.value)}
                  className="pr-8"
                />
                <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-sm text-muted-foreground">
                  %
                </span>
              </div>
            </FormField>
            <FormField htmlFor="settings-service-charge" label="Service charge rate">
              <div className="relative">
                <Input
                  id="settings-service-charge"
                  inputMode="decimal"
                  value={serviceChargeRatePercent}
                  onChange={(e) => setServiceChargeRatePercent(e.target.value)}
                  className="pr-8"
                />
                <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-sm text-muted-foreground">
                  %
                </span>
              </div>
            </FormField>
          </div>
          <Button onClick={saveBillCharges} loading={updateBillCharges.isPending}>
            Save tax &amp; service charge
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
