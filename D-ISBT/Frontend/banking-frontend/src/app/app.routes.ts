import { Routes } from '@angular/router';
import { Auth } from './pages/auth/auth';
import { Register } from './pages/register/register';
import { Dashboard } from './pages/dashboard/dashboard';
import { Admin } from './pages/admin/admin';
import { AdminAccounts } from './pages/admin-accounts/admin-accounts';
import { History } from './pages/history/history';
import { Deposit } from './pages/deposit/deposit';
import { Withdraw } from './pages/withdraw/withdraw';
import { RepeatTransaction } from './pages/repeat-transaction/repeat-transaction';
import { Accounts } from './pages/accounts/accounts';
import { CreateAccount } from './pages/create-account/create-account';
import { OwnTransfer } from './pages/own-transfer/own-transfer';
import { ExternalTransfer } from './pages/external-transfer/external-transfer';
import { ServicePayment } from './pages/service-payment/service-payment';
import { AdminUsers } from './pages/admin-users/admin-users';
import { AdminTransactions } from './pages/admin-transactions/admin-transactions';
import { MyProfile } from './pages/my-profile/my-profile';
import { Notifications } from './pages/notifications/notifications';
import { TransactionHelp } from './pages/transaction-help/transaction-help';
import { MaintenanceNotifications } from './pages/maintenance-notifications/maintenance-notifications';
import { RetentionPolicy } from './pages/retention-policy/retention-policy';
import { AdminAudit } from './pages/admin-audit/admin-audit';

export const routes: Routes = [
  { path: '', component: Auth },
  { path: 'register', component: Register },

  // CLIENT
  { path: 'dashboard', component: Dashboard },
  { path: 'accounts', component: Accounts },
  { path: 'create-account', component: CreateAccount },
  { path: 'own-transfer', component: OwnTransfer },
  { path: 'external-transfer', component: ExternalTransfer },
  { path: 'service-payment', component: ServicePayment },
  { path: 'deposit', component: Deposit },
  { path: 'withdraw', component: Withdraw },
  { path: 'history', component: History },
  { path: 'my-profile', component: MyProfile },
  { path: 'notifications', component: Notifications },
  { path: 'repeat-transaction', component: RepeatTransaction },
  { path: 'transaction-help', component: TransactionHelp },
  { path: 'retention-policy', component: RetentionPolicy },

  // ADMIN
  { path: 'admin-access', component: Admin },
  { path: 'admin/accounts', component: AdminAccounts },
  { path: 'admin/users', component: AdminUsers },
  { path: 'admin-transactions', component: AdminTransactions },
  { path: 'admin/retention-policy', component: RetentionPolicy },
  { path: 'admin/audit', component: AdminAudit },
  
  {
  path: 'maintenance-notifications',
  component: MaintenanceNotifications
},

  // fallback
  { path: '**', redirectTo: '' }
];