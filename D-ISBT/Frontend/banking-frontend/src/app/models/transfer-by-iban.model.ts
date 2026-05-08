export interface TransferByIbanRequest {
  initiatedByUserId: number;
  fromIban: string;
  toIban: string;
  amount: number;
  currency: string;
  description: string;
}