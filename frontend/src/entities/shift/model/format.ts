/**
 * The API sends UTC instants, but a value that has been through the database can arrive without a
 * trailing "Z" — which `Date` would then read as local time. Anything lacking a zone is UTC.
 */
export function utcDate(iso: string): Date {
  return new Date(/[zZ]|[+-]\d{2}:\d{2}$/.test(iso) ? iso : `${iso}Z`);
}

export function formatShiftTime(iso: string): string {
  return utcDate(iso).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" });
}

export function formatClock(iso: string): string {
  return utcDate(iso).toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" });
}

/** "7h 42m" — how long a shift has run, or ran. */
export function formatDuration(fromIso: string, toIso: string | null): string {
  const end = toIso ? utcDate(toIso).getTime() : Date.now();
  const minutes = Math.max(0, Math.round((end - utcDate(fromIso).getTime()) / 60000));

  return `${Math.floor(minutes / 60)}h ${String(minutes % 60).padStart(2, "0")}m`;
}

export function formatMoney(value: number): string {
  return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
