# PMS - Property Management System

MVP d'un PMS hôtelier avec intégration IPTV LG Pro:Centric.

## Stack technique

- **Backend** : Node.js + Express
- **Base de données** : PostgreSQL
- **IPTV** : LG Pro:Centric (JSON/REST + XML/HTNG)

## Installation

```bash
# 1. Installer les dépendances
npm install

# 2. Configurer la base de données
cp .env.example .env
# Éditer .env avec vos identifiants PostgreSQL

# 3. Créer la base de données et les tables
psql -U postgres -c "CREATE DATABASE pms_hotel;"
psql -U postgres -d pms_hotel -f src/config/schema.sql

# 4. Lancer le serveur
npm start
# ou en mode développement (auto-reload)
npm run dev
```

Le serveur démarre sur `http://localhost:3000`.

## Endpoints API

| Méthode | URL | Description |
|---------|-----|-------------|
| GET | `/api/health` | Health check |
| GET | `/api/rooms` | Liste des chambres |
| POST | `/api/rooms` | Créer une chambre |
| PATCH | `/api/rooms/:id/status` | Modifier le statut d'une chambre |
| GET | `/api/guests` | Liste des clients |
| POST | `/api/guests` | Créer un client |
| GET | `/api/reservations` | Liste des réservations |
| POST | `/api/reservations` | Créer une réservation |
| POST | `/api/checkin/:reservationId` | Check-in d'une réservation |
| GET | `/api/iptv/room/:roomNumber` | Info client pour le portail TV (Pull) |

## Tester le flux complet : Check-in → Notification IPTV

### 1. Vérifier que le serveur fonctionne

```bash
curl http://localhost:3000/api/health
```

### 2. Consulter les réservations existantes (données seed)

```bash
curl http://localhost:3000/api/reservations
```

Vous verrez deux réservations pré-configurées (ID 1 et 2).

### 3. Effectuer un check-in

```bash
curl -X POST http://localhost:3000/api/checkin/1
```

Réponse attendue :
```json
{
  "message": "Check-in successful",
  "reservation": {
    "id": 1,
    "guest": "Jean Dupont",
    "room": "101",
    "check_out": "2026-03-30",
    "status": "checked_in"
  },
  "iptv_notified": false
}
```

> `iptv_notified` sera `false` si le middleware Pro:Centric n'est pas en cours d'exécution. Le check-in reste valide, et l'erreur est tracée dans `audit_logs`.

### 4. Vérifier le statut de la chambre (doit être "occupied")

```bash
curl http://localhost:3000/api/rooms
```

### 5. Endpoint Pull IPTV — le portail TV récupère les infos client

```bash
curl http://localhost:3000/api/iptv/room/101
```

Réponse :
```json
{
  "room_number": "101",
  "guest_name": "Jean Dupont",
  "language": "fr",
  "check_out_date": "2026-03-30"
}
```

## Intégration Pro:Centric

Le service `procentricService.js` envoie les données du client au middleware LG lors du check-in. Deux méthodes sont implémentées :

1. **JSON/REST** — envoi `POST` avec payload JSON (méthode par défaut)
2. **XML/HTNG** — envoi `POST` avec payload XML au standard HTNG (fallback)

Le payload envoyé contient : `guest_name`, `room_number`, `check_out_date`, `language`.

Configurer l'URL du middleware dans `.env` :
```
PROCENTRIC_URL=http://localhost:8080/procentric/api
```

## Audit

Chaque action de check-in et chaque tentative de notification IPTV (succès ou échec) est enregistrée dans la table `audit_logs`.

## Structure du projet

```
src/
├── config/
│   ├── db.js              # Pool de connexions PostgreSQL
│   └── schema.sql         # Schéma SQL + données de test
├── controllers/
│   ├── checkinController.js
│   ├── guestController.js
│   ├── iptvController.js
│   ├── reservationController.js
│   └── roomController.js
├── integrations/
│   └── procentric/
│       └── procentricService.js  # Service JSON + XML
├── models/
│   ├── AuditLog.js
│   ├── Guest.js
│   ├── Reservation.js
│   └── Room.js
├── routes/
│   ├── checkin.js
│   ├── guests.js
│   ├── iptv.js
│   ├── reservations.js
│   └── rooms.js
├── app.js                 # Configuration Express
└── server.js              # Point d'entrée
```
