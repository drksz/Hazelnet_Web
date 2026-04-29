# HazelNet Seeder

Standalone console tool that populates the local Postgres database with a test user and realistic FSRS review data. Useful for manual testing and running the Layer 3 optimizer smoke tests.

## What it seeds

One user with 3 decks, 5 cards each, and a full review history per card.

```
seed-user
  Deck Alpha
    Q1-1 ... Q1-5  (5 cards)
  Deck Beta
    Q2-1 ... Q2-5
  Deck Gamma
    Q3-1 ... Q3-5

Per card:
  1 ReviewHistory
  6 ReviewLogs  (elapsed days: 0, 1, 3, 7, 14, 30)
                (ratings cycle: Again, Hard, Good, Easy, Good, Easy)

Total: 15 cards, 15 histories, 90 review logs
```

This gives the optimizer 75 training samples (6 logs per history = 5 windowed pairs each).

## Credentials

```
Username:      seed-user
Email:         seed@hazelnet.dev
PasswordHash:  not-a-real-hash
```

These are not real credentials. The password hash is a placeholder and cannot be used to log in.

## Prerequisites

- Postgres running locally on port 5432
- Database `HazelNetDb` already exists and migrations have been applied
- Default connection: `Host=localhost;Port=5432;Database=HazelNetDb;Username=postgres;Password=Password`

If your setup is different, edit `appsettings.json` before running.

## How to run

From the repo root:

```
dotnet run --project HazelNet-Seeder
```

The seeder is idempotent. Running it a second time just prints a summary of what already exists and exits without touching the database.

## Connection string

Stored in `HazelNet-Seeder/appsettings.json`. Change it there if your local Postgres uses different credentials. Do not hardcode it anywhere else.

## Notes

The seeder is not part of `Hazelnet_Web.sln` and never will be. It is a dev tool only. Do not run it against any environment other than local.
