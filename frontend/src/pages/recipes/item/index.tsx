import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { zodResolver } from "@hookform/resolvers/zod";
import { Controller, useFieldArray, useForm } from "react-hook-form";
import { ArrowLeft, CheckCircle2, Plus, Tags, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { MenuItem } from "@/entities/menu-item";
import {
  CategoryCombobox,
  ManageCategoriesDialog,
  MenuItemForm,
  menuItemSchema,
  toPriceNumber,
  useMenuItem,
  useMenuItemMutations,
} from "@/features/menu-items";
import { RecipeEditor } from "@/features/recipes";
import { toApiError } from "@/shared/api/problem";
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  FormField,
  Input,
  LoadingState,
  SegmentedTabs,
} from "@/shared/ui";

/**
 * Add or edit a menu item, and build the recipe for each of its sizes right below — one page
 * instead of the old "add the dish, close the dialog, hunt down the row, open a second dialog to
 * attach ingredients" round trip. The recipe section only turns on once the item actually has an
 * id: a fresh dish being added needs saving first, the same way a recipe could never be attached
 * to one before it existed under the old two-dialog flow either.
 */
export default function MenuItemDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: existing, isLoading } = useMenuItem(id);
  const { create, update } = useMenuItemMutations();

  const [savedItem, setSavedItem] = useState<MenuItem | null>(null);
  const [manageCategoriesOpen, setManageCategoriesOpen] = useState(false);
  const [activeVariantId, setActiveVariantId] = useState<string | null>(null);
  const pending = create.isPending || update.isPending;

  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<MenuItemForm>({
    resolver: zodResolver(menuItemSchema),
    defaultValues: { name: "", category: "", variants: [{ name: "", price: "" }] },
  });

  const { fields, append, remove } = useFieldArray({ control, name: "variants" });

  useEffect(() => {
    if (existing) {
      setSavedItem(existing);
      reset({
        name: existing.name,
        category: existing.category,
        variants: existing.variants.map((v) => ({ id: v.id, name: v.name ?? "", price: String(v.price) })),
      });
      setActiveVariantId((current) =>
        current && existing.variants.some((v) => v.id === current) ? current : (existing.variants[0]?.id ?? null),
      );
    }
  }, [existing, reset]);

  const onSubmit = handleSubmit(async (values) => {
    const variants = values.variants.map((v) => ({
      id: v.id ?? null,
      name: v.name.trim() || null,
      price: toPriceNumber(v.price),
    }));

    try {
      if (savedItem) {
        const updated = await update.mutateAsync({
          id: savedItem.id,
          payload: { name: values.name, category: values.category, variants },
        });
        setSavedItem(updated);
        setActiveVariantId((current) =>
          current && updated.variants.some((v) => v.id === current) ? current : (updated.variants[0]?.id ?? null),
        );
        toast.success(`${updated.name} was updated.`);
      } else {
        const created = await create.mutateAsync({
          name: values.name,
          category: values.category,
          variants: variants.map(({ name, price }) => ({ name, price })),
        });
        setSavedItem(created);
        setActiveVariantId(created.variants[0]?.id ?? null);
        toast.success(`${created.name} was added to the menu. Now attach its recipe below.`);
        navigate(`/recipes/${created.id}`, { replace: true });
      }
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  });

  if (id && (isLoading || !existing)) {
    return <LoadingState label="Loading menu item…" className="h-96" />;
  }

  const activeVariant = savedItem?.variants.find((v) => v.id === activeVariantId) ?? savedItem?.variants[0] ?? null;

  return (
    <div className="mx-auto max-w-2xl space-y-6 p-8">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="space-y-1">
          <Button variant="ghost" size="sm" asChild className="-ml-2">
            <Link to="/recipes">
              <ArrowLeft /> Recipe Management
            </Link>
          </Button>
          <h1 className="text-2xl font-semibold">{savedItem ? `Edit ${savedItem.name}` : "Add menu item"}</h1>
          <p className="text-sm text-muted-foreground">
            {savedItem
              ? "Changing a price only affects new orders."
              : "Save the details below, then build a recipe for each size on this same page."}
          </p>
        </div>

        <Button type="button" variant="outline" onClick={() => setManageCategoriesOpen(true)}>
          <Tags /> Manage categories
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Details</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={onSubmit} className="space-y-4">
            <FormField htmlFor="name" label="Name" required error={errors.name?.message}>
              <Input {...register("name")} placeholder="Chicken Fried Rice" autoFocus />
            </FormField>

            <FormField htmlFor="category" label="Category" required error={errors.category?.message}>
              <Controller
                name="category"
                control={control}
                render={({ field }) => (
                  <CategoryCombobox
                    id="category"
                    value={field.value}
                    onChange={field.onChange}
                    placeholder="Rice & Curry"
                  />
                )}
              />
            </FormField>

            <FormField
              htmlFor="variants"
              label="Sizes & prices"
              required
              hint={fields.length > 1 ? "Every size needs its own name to tell them apart." : undefined}
              error={errors.variants?.message}
            >
              <div className="space-y-2">
                {fields.map((field, index) => (
                  <div key={field.id} className="space-y-1">
                    <div className="flex items-center gap-2">
                      {fields.length > 1 && (
                        <Input
                          {...register(`variants.${index}.name`)}
                          placeholder="Normal, Full, Large…"
                          aria-label={`Size ${index + 1} name`}
                          className="flex-1"
                        />
                      )}
                      <Input
                        {...register(`variants.${index}.price`)}
                        inputMode="decimal"
                        placeholder="850"
                        aria-label={fields.length > 1 ? `Size ${index + 1} price` : "Price"}
                        className={fields.length > 1 ? "w-28" : "flex-1"}
                      />
                      {fields.length > 1 && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          onClick={() => remove(index)}
                          aria-label={`Remove size ${index + 1}`}
                        >
                          <Trash2 className="size-4 text-destructive" />
                        </Button>
                      )}
                    </div>
                    {(errors.variants?.[index]?.name?.message || errors.variants?.[index]?.price?.message) && (
                      <p className="text-xs text-destructive">
                        {errors.variants[index]?.name?.message ?? errors.variants[index]?.price?.message}
                      </p>
                    )}
                  </div>
                ))}
              </div>

              <Button
                type="button"
                variant="outline"
                size="sm"
                className="mt-2"
                onClick={() => append({ name: "", price: "" })}
              >
                <Plus /> Add another size
              </Button>
            </FormField>

            <Button type="submit" loading={pending}>
              {savedItem ? "Save changes" : "Save & continue to recipes"}
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Recipe</CardTitle>
          <CardDescription>
            Ingredients here are deducted from Kitchen stock automatically whenever this size is sold. Each
            size keeps its own recipe — a Full isn't assumed to use exactly proportionally more of everything.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {!savedItem ? (
            <p className="text-sm text-muted-foreground">
              Save the item's details above first — a recipe can be attached once it exists.
            </p>
          ) : (
            <>
              {savedItem.variants.length > 1 && (
                <SegmentedTabs
                  value={activeVariantId ?? savedItem.variants[0].id}
                  onValueChange={setActiveVariantId}
                  tabs={savedItem.variants.map((v) => ({
                    value: v.id,
                    label: v.name ?? "Unnamed",
                    icon: v.hasRecipe ? <CheckCircle2 className="size-3.5 text-emerald-500" /> : undefined,
                  }))}
                />
              )}

              {activeVariant && (
                <RecipeEditor
                  key={activeVariant.id}
                  menuItemVariantId={activeVariant.id}
                  menuItemName={activeVariant.name ? `${savedItem.name} (${activeVariant.name})` : savedItem.name}
                />
              )}
            </>
          )}
        </CardContent>
      </Card>

      <ManageCategoriesDialog open={manageCategoriesOpen} onOpenChange={setManageCategoriesOpen} />
    </div>
  );
}
