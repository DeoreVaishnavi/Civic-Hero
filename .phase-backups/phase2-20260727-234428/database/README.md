# CivicHero database

The production database is AWS RDS MySQL 8 and is managed only through EF Core migrations from `backend/CivicHero.Backend/Migrations`.

Do not manually change production tables. Create a migration, review it, test it in staging, and then apply it to production.
