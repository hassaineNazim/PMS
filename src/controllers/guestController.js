const Guest = require('../models/Guest');
const Room = require('../models/Room');
const Reservation = require('../models/Reservation');
const Invoice = require('../models/Invoice');
const AuditLog = require('../models/AuditLog');

async function getAllGuests(req, res) {
  try {
    const guests = await Guest.findAll();
    res.json(guests);
  } catch (err) {
    console.error('Error fetching guests:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function createGuest(req, res) {
  try {
    const { first_name, last_name, email, language, phone } = req.body;

    if (!first_name || !last_name) {
      return res.status(400).json({ error: 'Missing required fields: first_name, last_name' });
    }

    const guest = await Guest.create({ first_name, last_name, email, language, phone });
    res.status(201).json(guest);
  } catch (err) {
    // Handle unique constraint on email
    if (err.code === '23505' && err.constraint) {
      return res.status(409).json({ error: 'A guest with this email already exists' });
    }
    console.error('Error creating guest:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function quickInvoice(req, res) {
  try {
    const guest_id = req.params.id;
    const guest = await Guest.findById(guest_id);
    if (!guest) return res.status(404).json({ error: 'Guest not found' });

    // 1. Find an available room
    const availableRooms = await Room.findAvailable();
    if (!availableRooms || availableRooms.length === 0) {
      return res.status(400).json({ error: 'No available rooms to create an invoice' });
    }
    const room = availableRooms[0];

    // 2. Create 1-night reservation
    const checkInDate = new Date();
    const checkOutDate = new Date();
    checkOutDate.setDate(checkOutDate.getDate() + 1);

    const check_in = checkInDate.toISOString().split('T')[0];
    const check_out = checkOutDate.toISOString().split('T')[0];

    const reservation = await Reservation.create({
      guest_id,
      room_id: room.id,
      check_in,
      check_out
    });

    // 3. Mark as checked in
    await Reservation.updateStatus(reservation.id, 'checked_in');
    await Room.updateStatus(room.id, 'occupied');

    // 4. Create invoice
    const price_per_night = parseFloat(room.price_per_night) || 0;
    const invoice = await Invoice.create({
      reservation_id: reservation.id,
      guest_id,
      room_id: room.id,
      check_in,
      check_out,
      nights: 1,
      price_per_night,
      tax_rate: 10.00
    });

    await AuditLog.create({
      action: 'quick_invoice_created',
      entity_type: 'invoice',
      entity_id: invoice.id,
      details: { guest: `${guest.first_name} ${guest.last_name}`, room: room.number, total: invoice.total },
      success: true
    });

    res.json({ success: true, invoice });
  } catch (err) {
    console.error('Error creating quick invoice:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

module.exports = { getAllGuests, createGuest, quickInvoice };
