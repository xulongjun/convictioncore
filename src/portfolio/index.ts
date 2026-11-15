/** Portfolio capability — owners, portfolios, broker accounts (scaffold). */

export type OwnerKind = "Personal" | "Company";

export interface Owner {
  id: string;
  kind: OwnerKind;
  displayName: string;
}

export interface BrokerAccount {
  id: string;
  ownerId: string;
  portfolioId: string;
  broker: string;
  /** Masked reference only — never store full account numbers in git or logs. */
  accountRefMasked: string;
  baseCurrency: string;
  reportingCurrency: string;
}
