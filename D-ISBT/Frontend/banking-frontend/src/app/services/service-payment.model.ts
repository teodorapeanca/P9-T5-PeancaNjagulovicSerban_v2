
export interface ServicePaymentRequest {
  initiatedByUserId: number;
  fromIban: string;
  providerId: number;
  amount: number;
  currency: string;
  description: string;
}