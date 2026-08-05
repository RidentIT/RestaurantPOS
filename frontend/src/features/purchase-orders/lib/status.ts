import type { PurchaseOrderStatus } from "@/entities/supplier";
import type { BadgeProps } from "@/shared/ui";

export const PURCHASE_ORDER_STATUS_BADGE: Record<PurchaseOrderStatus, NonNullable<BadgeProps["variant"]>> = {
  Draft: "secondary",
  Submitted: "default",
  Confirmed: "warning",
  Delivered: "success",
  Cancelled: "destructive",
};
