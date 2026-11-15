/** Shared domain primitives. PoC placeholders — expand after architecture decisions. */

export type OwnerId = string & { readonly __brand: "OwnerId" };
export type PortfolioId = string & { readonly __brand: "PortfolioId" };
export type BrokerAccountId = string & { readonly __brand: "BrokerAccountId" };
export type ImportBatchId = string & { readonly __brand: "ImportBatchId" };
export type RawRecordId = string & { readonly __brand: "RawRecordId" };
export type EconomicEventId = string & { readonly __brand: "EconomicEventId" };
export type CycleId = string & { readonly __brand: "CycleId" };

export type Completeness =
  | "Complete"
  | "IncompleteHistory"
  | "NeedsReview";
