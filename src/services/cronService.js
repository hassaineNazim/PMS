const Reservation = require('../models/Reservation');
const Room = require('../models/Room');
const Invoice = require('../models/Invoice');
const AuditLog = require('../models/AuditLog');

async function processAutoCheckInOut() {
  try {
    const todayStr = new Date().toISOString().split('T')[0];
    const reservations = await Reservation.findAll();

    for (const res of reservations) {
      const checkInDate = new Date(res.check_in).toISOString().split('T')[0];
      const checkOutDate = new Date(res.check_out).toISOString().split('T')[0];

      // Auto Check-in
      if (res.status === 'confirmed' && checkInDate <= todayStr) {
        // verify room is strictly available before checking in
        const room = await Room.findById(res.room_id);
        if (room && room.status === 'available') {
          await Reservation.updateStatus(res.id, 'checked_in');
          await Room.updateStatus(res.room_id, 'occupied');

          const nights = Math.max(1, Math.round((new Date(res.check_out) - new Date(res.check_in)) / (1000 * 60 * 60 * 24)));
          const invoice = await Invoice.create({
            reservation_id: res.id,
            guest_id: res.guest_id,
            room_id: res.room_id,
            check_in: res.check_in,
            check_out: res.check_out,
            nights,
            price_per_night: room.price_per_night,
            tax_rate: 10.00
          });

          await AuditLog.create({
            action: 'auto_check_in',
            entity_type: 'reservation',
            entity_id: res.id,
            details: { message: 'Automatically checked in and invoice generated' },
            success: true
          });
          console.log(`[Auto] Checked in reservation ${res.id}`);
        }
      }

      // Auto Check-out
      if (res.status === 'checked_in' && checkOutDate <= todayStr) {
        await Reservation.updateStatus(res.id, 'checked_out');
        await Room.updateStatus(res.room_id, 'dirty');

        await AuditLog.create({
          action: 'auto_check_out',
          entity_type: 'reservation',
          entity_id: res.id,
          details: { message: 'Automatically checked out' },
          success: true
        });
        console.log(`[Auto] Checked out reservation ${res.id}`);
      }
    }
  } catch (err) {
    console.error('[Auto] Error processing checkin/checkout:', err);
  }
}

function startCron() {
  console.log('[Auto] Starting auto check-in/out cron scheduler...');
  // Run once immediately
  processAutoCheckInOut();
  // Run every 10 minutes (600000 ms)
  setInterval(processAutoCheckInOut, 600000);
}

module.exports = { startCron, processAutoCheckInOut };
