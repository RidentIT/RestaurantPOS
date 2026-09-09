import { useState } from "react";
import { Plus, Tag, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { toApiError } from "@/shared/api/problem";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  EmptyState,
  Input,
  LoadingState,
} from "@/shared/ui";
import { useCreateMenuCategory, useDeleteMenuCategory, useMenuCategories } from "../model/useMenuItems";

export interface ManageCategoriesDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

/**
 * Add or remove menu categories directly, without going through an item's own form.
 *
 * Deleting one only ever touches this suggestion list, never a menu item — the server refuses
 * the request while any item still carries the category, so a dish can never be left pointing at
 * a category that's quietly vanished from the picker.
 */
export function ManageCategoriesDialog({ open, onOpenChange }: ManageCategoriesDialogProps) {
  const { data: categories, isLoading } = useMenuCategories();
  const createCategory = useCreateMenuCategory();
  const deleteCategory = useDeleteMenuCategory();

  const [newName, setNewName] = useState("");
  const [pendingDelete, setPendingDelete] = useState<string | null>(null);

  const addCategory = async () => {
    const name = newName.trim();
    if (!name) return;

    try {
      await createCategory.mutateAsync(name);
      setNewName("");
      toast.success(`"${name}" added.`);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const removeCategory = async (name: string) => {
    setPendingDelete(name);

    try {
      await deleteCategory.mutateAsync(name);
      toast.success(`"${name}" removed.`);
    } catch (error) {
      toast.error(toApiError(error).message);
    } finally {
      setPendingDelete(null);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Manage categories</DialogTitle>
          <DialogDescription>
            A category still used by a menu item can't be removed from here.
          </DialogDescription>
        </DialogHeader>

        <div className="flex items-end gap-2">
          <Input
            value={newName}
            onChange={(event) => setNewName(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                event.preventDefault();
                void addCategory();
              }
            }}
            placeholder="New category name"
            aria-label="New category name"
          />
          <Button
            type="button"
            onClick={addCategory}
            loading={createCategory.isPending}
            disabled={!newName.trim()}
          >
            <Plus /> Add
          </Button>
        </div>

        {isLoading ? (
          <LoadingState label="Loading categories…" />
        ) : !categories || categories.length === 0 ? (
          <EmptyState
            icon={<Tag className="size-6" />}
            title="No categories yet"
            description="Add one above to get started."
          />
        ) : (
          <ul className="max-h-72 space-y-1 overflow-y-auto">
            {categories.map((category) => (
              <li
                key={category}
                className="flex items-center justify-between gap-2 rounded-md px-2 py-1.5 text-sm hover:bg-muted/50"
              >
                <span className="truncate">{category}</span>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  aria-label={`Remove ${category}`}
                  loading={pendingDelete === category && deleteCategory.isPending}
                  disabled={deleteCategory.isPending && pendingDelete !== category}
                  onClick={() => removeCategory(category)}
                >
                  <Trash2 className="size-4 text-destructive" />
                </Button>
              </li>
            ))}
          </ul>
        )}
      </DialogContent>
    </Dialog>
  );
}
