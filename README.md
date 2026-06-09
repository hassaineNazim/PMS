# PMS — Property Management System (multi-établissements)

Système de gestion hôtelière **commercial, robuste et multi-tenant**, conçu pour être revendu à plusieurs établissements. Intègre l'affichage en chambre LG (Pro:Centric / SuperSign) derrière une abstraction matériel-agnostique.

> Le prototype Node.js d'origine reste dans `src/` (legacy). La nouvelle plateforme vit dans `backend/` (.NET) et `frontend/` (React).

## Stack

| Couche | Technologie |
|--------|-------------|
| Backend | **.NET 9 / ASP.NET Core**, architecture en couches (Domain / Application / Infrastructure / API) |
| ORM | **EF Core 9** + **Npgsql**, migrations versionnées |
| Base de données | **PostgreSQL 16** |
| Frontend | **React 18 + TypeScript + Vite**, graphiques Recharts |
| Factures PDF | **QuestPDF** (déterministe, prêt à imprimer) |
| Auth | **JWT** (bcrypt pour les mots de passe) |
| Validation | **FluentValidation** |
| Logs | **Serilog** (structuré) |
| Tests | **xUnit** + EF InMemory (unitaires) + **Testcontainers** (intégration) |
| Déploiement | **Docker Compose** (db + api + web) |

## Démarrage rapide (Docker)

```bash
cp .env.example .env          # ajustez le secret JWT pour la production
docker compose up --build
```

- Frontend : http://localhost:8080
- API + Swagger : http://localhost:5080/swagger
- Connexion démo : **admin@demo.com** / **admin123** — établissement **demo**

La base est migrée, la contrainte anti-double-booking installée et un établissement de démonstration créé automatiquement au premier démarrage.

## Piliers de robustesse

### 1. Multi-tenant dès la conception
Chaque entité opérationnelle porte un `TenantId`. Un **filtre de requête global EF Core** (`AppDbContext`) contraint automatiquement *toute* lecture/écriture au tenant courant — impossible de lire les données d'un autre établissement. Le tenant est résolu par middleware depuis le claim JWT.

### 2. Jamais de double-réservation
Trois niveaux de protection :
1. Validation applicative (chevauchement de dates).
2. Transaction EF au check-in.
3. **Contrainte PostgreSQL `EXCLUDE USING gist`** sur `(room_id, daterange(check_in, check_out))` — garantie au niveau base, même si deux réceptionnistes valident exactement au même instant. La violation `23P01` est traduite en `409 Conflict`.

### 3. Concurrence optimiste
Jeton de concurrence **`xmin`** (PostgreSQL) sur les entités : deux modifications simultanées de la même ligne lèvent une `DbUpdateConcurrencyException` au lieu d'un écrasement silencieux.

### 4. Licences / activation
Chaque tenant a une `License` (plan, limites, date d'expiration). Une connexion à un établissement sans licence valide est refusée (`402`).

### 5. Affichage en chambre découplé
`IDisplayProvider` isole le matériel. Implémentation par défaut **LG Pro:Centric/SuperSign** (REST JSON + repli HTNG XML) ; `none` pour les sites sans écrans compatibles. Brancher une autre marque = une nouvelle implémentation, **sans toucher au cœur métier**.

## Modules commerciaux & conformité Algérie

- **Formules de pension** (logement seul / petit-déjeuner / demi-pension / pension complète) attachées à la réservation, avec supplément par personne / nuit configurable — la fonctionnalité signature pilotable depuis la TV.
- **Paiements** : acomptes + soldes multiples, modes espèces / CIB / Edahabia / virement / chèque, suivi du **solde dû** par réservation (folio distinct de la facture).
- **Caisse** : ouverture avec fond, clôture avec montant compté et **calcul de l'écart** (clôture de caisse réceptionniste).
- **Conformité DGI** : mentions légales obligatoires sur la facture (**NIF, NIS, RC, Article d'imposition**), **droit de timbre** sur les paiements en espèces (1 DA / tranche de 100 DA, minimum configurable).
- **Fiche de police / registre des étrangers** générée en PDF depuis la fiche client + **main courante** (journal arrivées/départs).
- **Extras / POS** : mini-bar, restaurant, room service, blanchisserie… ajoutés à la note et reportés sur la facture finale.
- **Housekeeping** : tableau des chambres, assignation aux gouvernantes, statuts propre / sale / en cours / inspecté.
- **Tarifs saisonniers** : périodes haute/basse saison par type de chambre (priorité), appliquées automatiquement au calcul du séjour.
- **Rapports exportables** : réservations et CA en **CSV**.
- **Notifications** : abstraction `INotificationProvider` (SMS/email) — comme l'affichage TV, prête à brancher une passerelle.

## Architecture

```
backend/
  src/
    Pms.Domain/          # Entités, enums, règles métier (zéro dépendance framework)
    Pms.Application/     # DTOs, services, validators, interfaces (IDisplayProvider…)
    Pms.Infrastructure/  # EF Core, multi-tenant, JWT, QuestPDF, provider LG, seed
    Pms.Api/             # Contrôleurs, middlewares, auth, Swagger
  tests/
    Pms.UnitTests/         # Réservations, factures, isolation tenant (EF InMemory)
    Pms.IntegrationTests/  # Flux check-in + contrainte EXCLUDE (Testcontainers)
frontend/                  # React + TS + Vite
docker-compose.yml
```

## Développement local (sans Docker pour le code)

Backend :
```bash
cd backend
dotnet run --project src/Pms.Api        # http://localhost:5080
dotnet test                              # tous les tests
```
> Nécessite le SDK .NET 9 et un PostgreSQL accessible (cf. `ConnectionStrings:Default`).

Frontend :
```bash
cd frontend
npm install
npm run dev                              # http://localhost:5173 (proxy /api -> :5080)
```

## Migrations EF Core

```bash
cd backend
dotnet ef migrations add <Nom> --project src/Pms.Infrastructure --startup-project src/Pms.Api -o Persistence/Migrations
```
Les migrations sont appliquées automatiquement au démarrage (`DbInitializer`).

## Principaux endpoints

| Méthode | Route | Rôle |
|---------|-------|------|
| POST | `/api/auth/login` | Connexion (JWT) |
| GET | `/api/stats/dashboard` | KPIs + séries pour graphiques |
| GET/POST/PUT/DELETE | `/api/rooms` | Chambres |
| GET/POST/PUT/DELETE | `/api/guests` | Clients |
| GET/POST/PUT | `/api/reservations` | Réservations |
| POST | `/api/reservations/availability` | Chambres disponibles sur une période |
| POST | `/api/checkin/{id}` | Check-in (statut + chambre + IPTV + facture) |
| POST | `/api/checkout/{id}` | Check-out |
| GET | `/api/invoices`, `/api/invoices/{id}/pdf` | Factures + PDF |
| GET/POST/PUT/DELETE | `/api/staff`, `/api/staff/schedules` | Personnel & plannings |

## Sécurité production (checklist)
- [ ] Remplacer `JWT_SECRET` par une valeur aléatoire ≥ 32 caractères.
- [ ] Mots de passe forts, désactiver le compte démo.
- [ ] HTTPS / reverse-proxy devant `web` et `api`.
- [ ] Sauvegardes automatiques du volume PostgreSQL (`pms-db-data`).
