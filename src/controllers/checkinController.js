const Reservation = require('../models/Reservation');
const Room = require('../models/Room');
const AuditLog = require('../models/AuditLog');
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

    // 3. Update reservation status
    await Reservation.updateStatus(reservationId, 'checked_in');

    // 3. Update room status to occupied
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

    return res.json({
      message: 'Check-in successful',
      reservation: {
        id: reservation.id,
        guest: `${reservation.first_name} ${reservation.last_name}`,
        room: reservation.room_number,
        check_out: reservation.check_out,
        status: 'checked_in',
      },
      iptv_notified: iptvResult !== null,
    });
  } catch (err) {
    console.error('Check-in error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
}

module.exports = { checkIn };
