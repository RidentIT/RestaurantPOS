import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import type { MenuItem } from "@/entities/menu-item";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  FormField,
  Input,
} from "@/shared/ui";
import { useMenuItemMutations } from "../model/useMenuItems";
import { MenuItemForm, menuItemSchema, toPriceNumber } from "../model/menuItemSchema";

export interface MenuItemFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** The item being edited, or omitted to create a new one. */
  item?: MenuItem;
}

/** Creates or edits a menu item's name, category and price. */
export function MenuItemFormDialog({ open, onOpenChange, item }: MenuItemFormDialogProps) {
  const isEditing = !!item;
  const { create, update } = useMenuItemMutations();
  const pending = create.isPending || update.isPending;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<MenuItemForm>({
    resolver: zodResolver(menuItemSchema),
    defaultValues: {
      name: item?.name ?? "",
      category: item?.category ?? "",
      price: item ? String(item.price) : "",
    },
  });

  const close = (isOpen: boolean) => {
    if (!isOpen) {
      reset({
        name: item?.name ?? "",
        category: item?.category ?? "",
        price: item ? String(item.price) : "",
      });
    }
    onOpenChange(isOpen);
  };

  const onSubmit = handleSubmit(async (values) => {
    const payload = { name: values.name, category: values.category, price: toPriceNumber(values.price) };

    try {
      if (isEditing) {
        await update.mutateAsync({ id: item.id, payload });
        toast.success(`${values.name} was updated.`);
      } else {
        await create.mutateAsync(payload);
        toast.success(`${values.name} was added to the menu.`);
      }
      close(false);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Something went wrong.");
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEditing ? "Edit menu item" : "Add menu item"}</DialogTitle>
          <DialogDescription>
            {isEditing
              ? "Changing the price only affects new orders."
              : "You can attach a recipe once it's been added to the menu."}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="space-y-4">
          <FormField htmlFor="name" label="Name" required error={errors.name?.message}>
            <Input {...register("name")} placeholder="Chicken Fried Rice" autoFocus />
          </FormField>

          <FormField htmlFor="category" label="Category" required error={errors.category?.message}>
            <Input {...register("category")} placeholder="Rice & Curry" />
          </FormField>

          <FormField htmlFor="price" label="Price" required error={errors.price?.message}>
            <Input {...register("price")} inputMode="decimal" placeholder="850" />
          </FormField>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => close(false)} disabled={pending}>
              Cancel
            </Button>
            <Button type="submit" loading={pending}>
              {isEditing ? "Save changes" : "Add item"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
