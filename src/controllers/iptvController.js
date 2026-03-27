const Room = require('../models/Room');
const Reservation = require('../models/Reservation');

/**
 * GET /api/iptv/room/:roomNumber
 * Pull endpoint for the LG Pro:Centric TV portal.
 * Returns current guest info for a given room.
 */
async function getGuestInfoByRoom(req, res) {
  const { roomNumber } = req.params;

  try {
    const room = await Room.findByNumber(roomNumber);
    if (!room) {
      return res.status(404).json({ error: 'Room not found' });
    }

    if (room.status !== 'occupied') {
      return res.json({
        room_number: room.number,
        status: room.status,
        guest: null,
      });
    }

    const reservation = await Reservation.findCheckedInByRoom(room.id);
    if (!reservation) {
      return res.json({
        room_number: room.number,
        status: room.status,
        guest: null,
      });
    }

    return res.json({
      room_number: room.number,
      guest_name: `${reservation.first_name} ${reservation.last_name}`,
      language: reservation.language,
      check_out_date: reservation.check_out,
    });
  } catch (err) {
    console.error('IPTV pull error:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

module.exports = { getGuestInfoByRoom };
