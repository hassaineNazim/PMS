const Reservation = require('../models/Reservation');
const Room = require('../models/Room');
const Invoice = require('../models/Invoice');
const AuditLog = require('../models/AuditLog');
const procentricService = require('../integrations/procentric/procentricService');

/**
 * POST /api/checkout/:reservationId
 * - Validates reservation exists and is checked_in
 * - Updates reservation status to checked_out
 * - Updates room status to dirty
 * - Sends IPTV checkout notification via Pro:Centric (non-blocking)
 * - Logs everything in audit_logs
 */
async function checkOut(req, res) {
  const { reservationId } = req.params;

  try {
    // 1. Fetch the reservation with guest + room info
    const reservation = await Reservation.findById(reservationId);
    if (!reservation) {
      return res.status(404).json({ error: 'Reservation not found' });
    }
    if (reservation.status === 'checked_out') {
      return res.status(400).json({ error: 'Guest is already checked out' });
    }
    if (reservation.status !== 'checked_in') {
      return res.status(400).json({ error: `Cannot check out a reservation with status: ${reservation.status}` });
    }

    // 2. Update reservation status
    await Reservation.updateStatus(reservationId, 'checked_out');

    // 3. Update room status to dirty
    await Room.updateStatus(reservation.room_id, 'dirty');

    // 4. Notify LG Pro:Centric IPTV middleware (non-blocking)
    try {
      await procentricService.notifyCheckOut(reservation);

      await AuditLog.create({
        action: 'iptv_checkout_notification',
        entity_type: 'reservation',
        entity_id: parseInt(reservationId, 10),
        details: {
          guest_name: `${reservation.first_name} ${reservation.last_name}`,
          room_number: reservation.room_number,
        },
        success: true,
      });
    } catch (iptvErr) {
      // Log the IPTV failure but don't block the check-out
      console.error('IPTV checkout notification failed:', iptvErr.message);

      await AuditLog.create({
        action: 'iptv_checkout_notification',
        entity_type: 'reservation',
        entity_id: parseInt(reservationId, 10),
        details: {
          guest_name: `${reservation.first_name} ${reservation.last_name}`,
          room_number: reservation.room_number,
        },
        success: false,
        error_message: iptvErr.message,
      });
    }

    // 5. Log the check-out action
    await AuditLog.create({
      action: 'check_out',
      entity_type: 'reservation',
      entity_id: parseInt(reservationId, 10),
      details: {
        guest: `${reservation.first_name} ${reservation.last_name}`,
        room: reservation.room_number,
      },
      success: true,
    });

    // 6. Log the room status change
    await AuditLog.create({
      action: 'room_status_change',
      entity_type: 'room',
      entity_id: reservation.room_id,
      details: {
        room: reservation.room_number,
        from: 'occupied',
        to: 'dirty',
      },
      success: true,
    });

    // 7. Create invoice
    const room = await Room.findById(reservation.room_id);
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

    // 8. Log invoice creation
    await AuditLog.create({
      action: 'invoice_created',
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
      message: 'Check-out successful',
      reservation: {
        id: reservation.id,
        guest: `${reservation.first_name} ${reservation.last_name}`,
        room: reservation.room_number,
        status: 'checked_out',
      },
      invoice: {
        id: invoice.id,
        nights: invoice.nights,
        price_per_night: invoice.price_per_night,
        subtotal: invoice.subtotal,
        tax_amount: invoice.tax_amount,
        total: invoice.total,
      },
    });
  } catch (err) {
    console.error('Check-out error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
}

module.exports = { checkOut };
