const Reservation = require('../models/Reservation');
const Room = require('../models/Room');
const AuditLog = require('../models/AuditLog');

async function getAllReservations(req, res) {
  try {
    const reservations = await Reservation.findAll();
    res.json(reservations);
  } catch (err) {
    console.error('Error fetching reservations:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function createReservation(req, res) {
  try {
    const { guest_id, room_id, check_in, check_out } = req.body;

    // Validate required fields
    if (!guest_id || !room_id || !check_in || !check_out) {
      return res.status(400).json({ error: 'Missing required fields: guest_id, room_id, check_in, check_out' });
    }

    // Validate dates
    if (new Date(check_out) <= new Date(check_in)) {
      return res.status(400).json({ error: 'Check-out date must be after check-in date' });
    }

    // Validate room exists
    const room = await Room.findById(room_id);
    if (!room) {
      return res.status(404).json({ error: 'Room not found' });
    }

    // Check for conflicting reservations (date-based, not room status)
    const conflict = await Reservation.findConflicting(room_id, check_in, check_out);
    if (conflict) {
      return res.status(409).json({ error: 'Conflicting reservation exists for this room and dates' });
    }

    const reservation = await Reservation.create({ guest_id, room_id, check_in, check_out });

    // Audit log
    await AuditLog.create({
      action: 'reservation_created',
      entity_type: 'reservation',
      entity_id: reservation.id,
      details: { guest_id, room_id, check_in, check_out },
      success: true,
    });

    res.status(201).json(reservation);
  } catch (err) {
    console.error('Error creating reservation:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function cancelReservation(req, res) {
  try {
    const { id } = req.params;

    const reservation = await Reservation.findById(id);
    if (!reservation) {
      return res.status(404).json({ error: 'Reservation not found' });
    }
    if (reservation.status !== 'confirmed') {
      return res.status(400).json({ error: `Cannot cancel a reservation with status: ${reservation.status}` });
    }

    await Reservation.updateStatus(id, 'cancelled');

    await AuditLog.create({
      action: 'reservation_cancelled',
      entity_type: 'reservation',
      entity_id: parseInt(id, 10),
      details: {
        guest: `${reservation.first_name} ${reservation.last_name}`,
        room: reservation.room_number,
      },
      success: true,
    });

    res.json({
      message: 'Reservation cancelled successfully',
      reservation: {
        id: reservation.id,
        guest: `${reservation.first_name} ${reservation.last_name}`,
        room: reservation.room_number,
        status: 'cancelled',
      },
    });
  } catch (err) {
    console.error('Error cancelling reservation:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function modifyReservation(req, res) {
  try {
    const { id } = req.params;
    const { guest_id, room_id, check_in, check_out } = req.body;

    if (!guest_id || !room_id || !check_in || !check_out) {
      return res.status(400).json({ error: 'Missing required fields' });
    }

    if (new Date(check_out) <= new Date(check_in)) {
      return res.status(400).json({ error: 'Check-out date must be after check-in date' });
    }

    const reservation = await Reservation.findById(id);
    if (!reservation) return res.status(404).json({ error: 'Reservation not found' });
    
    if (reservation.status === 'checked_out' || reservation.status === 'cancelled') {
        return res.status(400).json({ error: 'Cannot modify a closed reservation' });
    }

    const conflict = await Reservation.findConflicting(room_id, check_in, check_out, id);
    if (conflict) {
      return res.status(409).json({ error: 'Conflicting reservation exists for these dates' });
    }

    const updated = await Reservation.update(id, { guest_id, room_id, check_in, check_out });

    await AuditLog.create({
      action: 'reservation_modified',
      entity_type: 'reservation',
      entity_id: parseInt(id, 10),
      details: { guest_id, room_id, check_in, check_out },
      success: true,
    });

    res.json(updated);
  } catch (err) {
    console.error('Error modifying reservation:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

module.exports = { getAllReservations, createReservation, cancelReservation, modifyReservation };
