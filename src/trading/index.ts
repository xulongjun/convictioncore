/** Trading / ledger capability — import → normalize → reconstruct (scaffold). */

export type ImportStatus = "Accepted" | "AlreadyImported" | "Rejected";

export interface ImportBatch {
  id: string;
  brokerAccountId: string;
  contentSha256: string;
  parserId: string;
  parserVersion: number;
  status: ImportStatus;
}

export interface RawRecord {
  id: string;
  batchId: string;
  rowIndex: number;
  /** Opaque source payload preserved as ingested. */
  payload: Record<string, unknown>;
}

export interface EconomicEvent {
  id: string;
  batchId: string;
  rawRecordIds: string[];
  normalizerVersion: number;
  kind: string;
}
