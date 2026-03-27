-- PMS Hotel Database Schema

CREATE TYPE room_status AS ENUM ('available', 'occupied', 'dirty');
CREATE TYPE room_type AS ENUM ('single', 'double', 'suite', 'deluxe');
CREATE TYPE reservation_status AS ENUM ('confirmed', 'checked_in', 'checked_out', 'cancelled');

CREATE TABLE IF NOT EXISTS rooms (
    id SERIAL PRIMARY KEY,
    number VARCHAR(10) UNIQUE NOT NULL,
    type room_type NOT NULL DEFAULT 'single',
    status room_status NOT NULL DEFAULT 'available',
    floor INTEGER,
    price_per_night NUMERIC(10,2) NOT NULL DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS guests (
    id SERIAL PRIMARY KEY,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(255) UNIQUE,
    language VARCHAR(5) DEFAULT 'en',
    phone VARCHAR(30),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS reservations (
    id SERIAL PRIMARY KEY,
    guest_id INTEGER NOT NULL REFERENCES guests(id),
    room_id INTEGER NOT NULL REFERENCES rooms(id),
    check_in DATE NOT NULL,
    check_out DATE NOT NULL,
    status reservation_status NOT NULL DEFAULT 'confirmed',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS audit_logs (
    id SERIAL PRIMARY KEY,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(50),
    entity_id INTEGER,
    details JSONB,
    success BOOLEAN DEFAULT TRUE,
    error_message TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS invoices (
    id SERIAL PRIMARY KEY,
    reservation_id INTEGER NOT NULL REFERENCES reservations(id),
    guest_id INTEGER NOT NULL REFERENCES guests(id),
    room_id INTEGER NOT NULL REFERENCES rooms(id),
    check_in DATE NOT NULL,
    check_out DATE NOT NULL,
    nights INTEGER NOT NULL,
    price_per_night NUMERIC(10,2) NOT NULL,
    subtotal NUMERIC(10,2) NOT NULL,
    tax_rate NUMERIC(5,2) NOT NULL DEFAULT 10.00,
    tax_amount NUMERIC(10,2) NOT NULL,
    total NUMERIC(10,2) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'paid',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Seed data for testing
INSERT INTO rooms (number, type, status, floor, price_per_night) VALUES
    ('101', 'single', 'available', 1, 89.00),
    ('102', 'double', 'available', 1, 129.00),
    ('201', 'suite', 'available', 2, 299.00),
    ('202', 'deluxe', 'available', 2, 199.00)
ON CONFLICT (number) DO NOTHING;

INSERT INTO guests (first_name, last_name, email, language) VALUES
    ('Jean', 'Dupont', 'jean.dupont@email.com', 'fr'),
    ('John', 'Smith', 'john.smith@email.com', 'en')
ON CONFLICT (email) DO NOTHING;

INSERT INTO reservations (guest_id, room_id, check_in, check_out, status) VALUES
    (1, 1, CURRENT_DATE, CURRENT_DATE + INTERVAL '3 days', 'confirmed'),
    (2, 3, CURRENT_DATE, CURRENT_DATE + INTERVAL '5 days', 'confirmed');
