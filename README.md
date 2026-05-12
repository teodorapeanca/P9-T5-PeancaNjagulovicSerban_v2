Sistem Informatic Distribuit pentru Tranzactii Bancare(Peanca Teodora, Njagulovic Tamara, Serban Andrada )

Tehnologii utilizate:
- Angular
- TypeScript
- ASP.NET Core Web API
- PostgreSQL
- JWT Authentication

Frontend:
1. Deschideti folderul banking-frontend
2. Rulati:
   npm install
   ng serve -o

Backend:
1. Deschideti BankingAPI.sln in Visual Studio
2. Verificati connection string-ul din appsettings.json
3. Rulati proiectul

Baza de date:
- PostgreSQL
- Fisier export inclus: banking_db.sql

Functionalitati implementate:
- autentificare JWT + 2FA
- management utilizatori
- management conturi
- tranzactii bancare
- notificari
- jurnal audit
- politici de retentie
- detectie AML
  
	login administrator (userId=10):  (+ auditor + operator banca + IT Support)

{
  "clientIdentifier": "CLT-20260414164827149", 
  "password": "Admin123!"
}
e-mail: admin@test.com
	login user 1  (userId=7): 
{
  "clientIdentifier": "CLT-20260413123623478",
  "password": "Client123!"
}

e-mail: client1@test.com
conturi de test:
"RO49AAAA1B31007593840021" - RON
"RO49AAAA1B31007593840030" – RON

	login user 2 (userId=15): 
{
  "clientIdentifier": "CLT-20260416180651146",
  "password": "Client456!"
}
e-mail: client.etapa2@test.com
cont de test:
"RO49DIBT9208329886125522" - RON
