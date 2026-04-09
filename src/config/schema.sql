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

-- Staff enums
CREATE TYPE staff_role AS ENUM ('manager', 'receptionist', 'housekeeper', 'maintenance', 'security', 'other');
CREATE TYPE staff_status AS ENUM ('active', 'inactive', 'on_leave');

CREATE TABLE IF NOT EXISTS staff (
    id SERIAL PRIMARY KEY,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(255) UNIQUE,
    phone VARCHAR(30),
    role staff_role NOT NULL DEFAULT 'other',
    department VARCHAR(100),
    hire_date DATE NOT NULL DEFAULT CURRENT_DATE,
    status staff_status NOT NULL DEFAULT 'active',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS staff_schedules (
    id SERIAL PRIMARY KEY,
    staff_id INTEGER NOT NULL REFERENCES staff(id) ON DELETE CASCADE,
    date DATE NOT NULL,
    shift_start TIME NOT NULL,
    shift_end TIME NOT NULL,
    notes TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_staff_schedules_staff_id ON staff_schedules(staff_id);
CREATE INDEX IF NOT EXISTS idx_staff_schedules_date ON staff_schedules(date);
CREATE INDEX IF NOT EXISTS idx_staff_role ON staff(role);
CREATE INDEX IF NOT EXISTS idx_staff_status ON staff(status);

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

INSERT INTO staff (first_name, last_name, email, phone, role, department, hire_date, status) VALUES
    ('Marie', 'Laurent', 'marie.laurent@hotel.com', '+33 6 12 34 56 78', 'manager', 'Direction', '2020-03-15', 'active'),
    ('Pierre', 'Martin', 'pierre.martin@hotel.com', '+33 6 23 45 67 89', 'receptionist', 'Reception', '2021-06-01', 'active'),
    ('Sophie', 'Bernard', 'sophie.bernard@hotel.com', '+33 6 34 56 78 90', 'housekeeper', 'Housekeeping', '2022-01-10', 'active'),
    ('Lucas', 'Petit', 'lucas.petit@hotel.com', '+33 6 45 67 89 01', 'maintenance', 'Maintenance', '2021-09-20', 'active'),
    ('Emma', 'Dubois', 'emma.dubois@hotel.com', '+33 6 56 78 90 12', 'receptionist', 'Reception', '2023-02-14', 'on_leave'),
    ('Thomas', 'Moreau', 'thomas.moreau@hotel.com', '+33 6 67 89 01 23', 'security', 'Security', '2022-07-05', 'active')
ON CONFLICT (email) DO NOTHING;

INSERT INTO staff_schedules (staff_id, date, shift_start, shift_end, notes) VALUES
    (1, CURRENT_DATE, '08:00', '16:00', 'Morning shift'),
    (2, CURRENT_DATE, '06:00', '14:00', 'Early reception'),
    (3, CURRENT_DATE, '07:00', '15:00', 'Floor cleaning'),
    (4, CURRENT_DATE, '09:00', '17:00', 'Routine maintenance'),
    (2, CURRENT_DATE + INTERVAL '1 day', '14:00', '22:00', 'Afternoon reception'),
    (5, CURRENT_DATE + INTERVAL '1 day', '06:00', '14:00', 'Morning reception'),
    (6, CURRENT_DATE, '22:00', '06:00', 'Night security');
