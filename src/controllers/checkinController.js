const Reservation = require('../models/Reservation');
const Room = require('../models/Room');
const AuditLog = require('../models/AuditLog');
const Invoice = require('../models/Invoice');
const procentricService = require('../integrations/procentric/procentricService');

/**
 * POST /api/checkin/:reservationId
 * - Updates reservation status to checked_in
 * - Updates room status to occupied
 * - Sends IPTV notification via Pro:Centric
 * - Logs everything in audit_logs
 */
async function checkIn(req, res) {
  const { reservationId } = req.params;
  const { accompanying_guests } = req.body || {};

  try {
    // 1. Fetch the reservation with guest + room info
    const reservation = await Reservation.findById(reservationId);
    if (!reservation) {
      return res.status(404).json({ error: 'Reservation not found' });
    }
    if (reservation.status === 'checked_in') {
      return res.status(400).json({ error: 'Guest is already checked in' });
    }
    if (reservation.status !== 'confirmed') {
      return res.status(400).json({ error: `Cannot check in a reservation with status: ${reservation.status}` });
    }

    // 2. Verify the room is still available
    const room = await Room.findById(reservation.room_id);
    if (!room || room.status === 'occupied') {
      return res.status(409).json({ error: 'Room is already occupied' });
    }

    // 3. Update reservation status and guests
    await Reservation.updateStatus(reservationId, 'checked_in');
    if (accompanying_guests) {
      await Reservation.setAccompanyingGuests(reservationId, accompanying_guests);
    }

    // 4. Update room status to occupied
    await Room.updateStatus(reservation.room_id, 'occupied');

    // 4. Notify LG Pro:Centric IPTV middleware
    let iptvResult = null;
    try {
      iptvResult = await procentricService.notifyCheckIn(reservation);

      await AuditLog.create({
        action: 'iptv_notification',
        entity_type: 'reservation',
        entity_id: parseInt(reservationId, 10),
        details: iptvResult,
        success: true,
      });
    } catch (iptvErr) {
      // Log the IPTV failure but don't block the check-in
      console.error('IPTV notification failed:', iptvErr.message);

      await AuditLog.create({
        action: 'iptv_notification',
        entity_type: 'reservation',
        entity_id: parseInt(reservationId, 10),
        details: { guest_name: `${reservation.first_name} ${reservation.last_name}`, room_number: reservation.room_number },
        success: false,
        error_message: iptvErr.message,
      });
    }

    // 5. Log the check-in action
    await AuditLog.create({
      action: 'check_in',
      entity_type: 'reservation',
      entity_id: parseInt(reservationId, 10),
      details: {
        guest: `${reservation.first_name} ${reservation.last_name}`,
        room: reservation.room_number,
      },
      success: true,
    });

    // 6. Create invoice on check-in
    const checkInDate = new Date(reservation.check_in);
    const checkOutDate = new Date(reservation.check_out);
    const nights = Math.max(1, Math.round((checkOutDate - checkInDate) / (1000 * 60 * 60 * 24)));
    const price_per_night = parseFloat(room.price_per_night) || 0;

    const invoice = await Invoice.create({
      reservation_id: reservation.id,
      guest_id: reservation.guest_id,
      room_id: reservation.room_id,
      check_in: reservation.check_in,
      check_out: reservation.check_out,
      nights,
      price_per_night,
      tax_rate: 10.00,
    });

    await AuditLog.create({
      action: 'invoice_created_at_checkin',
      entity_type: 'invoice',
      entity_id: invoice.id,
      details: {
        guest: `${reservation.first_name} ${reservation.last_name}`,
        room: reservation.room_number,
        total: invoice.total,
      },
      success: true,
    });

    return res.json({
      message: 'Check-in successful and invoice created',
      reservation: {
        id: reservation.id,
        guest: `${reservation.first_name} ${reservation.last_name}`,
        room: reservation.room_number,
        check_out: reservation.check_out,
        status: 'checked_in',
      },
      invoice: {
        id: invoice.id,
        total: invoice.total,
      },
      iptv_notified: iptvResult !== null,
    });
  } catch (err) {
    console.error('Check-in error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
}

module.exports = { checkIn };
