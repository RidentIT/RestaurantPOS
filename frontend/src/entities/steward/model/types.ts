/**
 * A member of the waiting staff an order can be credited to. Not a login account — just a name an
 * administrator maintains so the till can attribute a table and the owner can see sales per steward.
 */
export interface Steward {
  id: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface StewardFilters {
  isActive?: boolean;
}
