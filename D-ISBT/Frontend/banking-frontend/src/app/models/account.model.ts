export interface Account {
  accountId: number;
  accountUid: string;
  userId: number;
  iban: string;
  currency: string;
  balance: number;
  status: string;
  dailyLimit: number;
  createdAt: string;
  updatedAt: string;
}