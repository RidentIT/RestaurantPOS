import { useState } from "react";
import { BarChart3 } from "lucide-react";
import type { Supplier } from "@/entities/supplier";
import { useSupplierPerformance } from "@/features/suppliers";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  LoadingState,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/shared/ui";

function StatCard({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardDescription>{label}</CardDescription>
        <CardTitle className="text-2xl">{value}</CardTitle>
      </CardHeader>
      {hint && (
        <CardContent className="pt-0 text-xs text-muted-foreground">{hint}</CardContent>
      )}
    </Card>
  );
}

export function SupplierPerformancePanel({ suppliers }: { suppliers: Supplier[] }) {
  const [supplierId, setSupplierId] = useState<string>(suppliers[0]?.id ?? "");
  const { data: performance, isLoading } = useSupplierPerformance(supplierId, !!supplierId);

  if (suppliers.length === 0) {
    return (
      <EmptyState icon={<BarChart3 className="size-6" />} title="Add a supplier first" description="Performance is derived from their orders and deliveries." />
    );
  }

  return (
    <div className="space-y-4 p-4">
      <div className="w-64 space-y-1.5">
        <Select value={supplierId} onValueChange={setSupplierId}>
          <SelectTrigger>
            <SelectValue placeholder="Choose a supplier" />
          </SelectTrigger>
          <SelectContent>
            {suppliers.map((s) => (
              <SelectItem key={s.id} value={s.id}>
                {s.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {isLoading ? (
        <LoadingState label="Loading performance…" />
      ) : !performance ? (
        <EmptyState icon={<BarChart3 className="size-6" />} title="No data yet" />
      ) : performance.totalOrders === 0 ? (
        <EmptyState
          icon={<BarChart3 className="size-6" />}
          title="No orders yet"
          description="Performance builds up once purchase orders have been placed and delivered."
        />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard label="Total orders" value={String(performance.totalOrders)} hint={`${performance.deliveredOrders} delivered`} />
          <StatCard
            label="On-time delivery"
            value={performance.onTimeDeliveryRate != null ? `${performance.onTimeDeliveryRate.toFixed(0)}%` : "—"}
            hint={performance.onTimeDeliveryRate != null ? `${performance.onTimeDeliveries} of ${performance.deliveredOrders} on time` : "No expected dates recorded"}
          />
          <StatCard
            label="Avg. delivery time"
            value={performance.averageDeliveryDays != null ? `${performance.averageDeliveryDays.toFixed(1)}d` : "—"}
            hint="From submission to receipt"
          />
          <StatCard
            label="Avg. quality rating"
            value={performance.averageQualityRating != null ? `${performance.averageQualityRating.toFixed(1)} / 5` : "—"}
            hint={`${performance.issueCount} deliver${performance.issueCount === 1 ? "y" : "ies"} flagged with an issue`}
          />
        </div>
      )}
    </div>
  );
}
