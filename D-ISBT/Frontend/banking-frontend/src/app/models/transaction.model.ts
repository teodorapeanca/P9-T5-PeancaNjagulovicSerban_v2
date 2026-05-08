export interface Transaction {
  transactionId: number;
  transactionUid: string;
  initiatedByUserId: number;
  fromAccountId: number;
  toAccountId: number;
  providerId: number;
  type: string;
  amount: number;
  currency: string;
  feeAmount: number;
  status: string;
  amlFlag: boolean;
  description: string;
  createdAt: string;
}