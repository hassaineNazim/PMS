-- Migration: Add staff management tables
-- Run: psql -U postgres -d pms_hotel -f src/config/migrate_staff.sql

DO $$ BEGIN
  CREATE TYPE staff_role AS ENUM ('manager', 'receptionist', 'housekeeper', 'maintenance', 'security', 'other');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
  CREATE TYPE staff_status AS ENUM ('active', 'inactive', 'on_leave');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

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

-- Seed sample staff
INSERT INTO staff (first_name, last_name, email, phone, role, department, hire_date, status) VALUES
    ('Marie', 'Laurent', 'marie.laurent@hotel.com', '+33 6 12 34 56 78', 'manager', 'Direction', '2020-03-15', 'active'),
    ('Pierre', 'Martin', 'pierre.martin@hotel.com', '+33 6 23 45 67 89', 'receptionist', 'Reception', '2021-06-01', 'active'),
    ('Sophie', 'Bernard', 'sophie.bernard@hotel.com', '+33 6 34 56 78 90', 'housekeeper', 'Housekeeping', '2022-01-10', 'active'),
    ('Lucas', 'Petit', 'lucas.petit@hotel.com', '+33 6 45 67 89 01', 'maintenance', 'Maintenance', '2021-09-20', 'active'),
    ('Emma', 'Dubois', 'emma.dubois@hotel.com', '+33 6 56 78 90 12', 'receptionist', 'Reception', '2023-02-14', 'on_leave'),
    ('Thomas', 'Moreau', 'thomas.moreau@hotel.com', '+33 6 67 89 01 23', 'security', 'Security', '2022-07-05', 'active')
ON CONFLICT (email) DO NOTHING;
