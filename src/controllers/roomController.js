const Room = require('../models/Room');

async function getAllRooms(req, res) {
  try {
    const rooms = await Room.findAll();
    res.json(rooms);
  } catch (err) {
    console.error('Error fetching rooms:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function createRoom(req, res) {
  try {
    const { number, type, floor, price_per_night } = req.body;

    // Validate required fields
    if (!number || !type || floor === undefined || floor === null || price_per_night === undefined || price_per_night === null) {
      return res.status(400).json({ error: 'Missing required fields: number, type, floor, price_per_night' });
    }

    // Validate room type against enum
    const validTypes = ['single', 'double', 'suite', 'deluxe'];
    if (!validTypes.includes(type)) {
      return res.status(400).json({ error: 'Invalid room type. Must be one of: ' + validTypes.join(', ') });
    }

    // Check for duplicate room number
    const existing = await Room.findByNumber(number);
    if (existing) {
      return res.status(409).json({ error: 'Room number ' + number + ' already exists' });
    }

    const room = await Room.create({ number, type, floor, price_per_night });
    res.status(201).json(room);
  } catch (err) {
    console.error('Error creating room:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function updateRoomStatus(req, res) {
  try {
    const room = await Room.updateStatus(req.params.id, req.body.status);
    if (!room) return res.status(404).json({ error: 'Room not found' });
    res.json(room);
  } catch (err) {
    console.error('Error updating room:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function getAvailableRooms(req, res) {
  try {
    const rooms = await Room.findAvailable();
    res.json(rooms);
  } catch (err) {
    console.error('Error fetching available rooms:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function updateRoom(req, res) {
  try {
    const { id } = req.params;
    const { number, type, floor, price_per_night, status } = req.body;

    // Validate required fields
    if (!number || !type || floor === undefined || floor === null || price_per_night === undefined || price_per_night === null || !status) {
      return res.status(400).json({ error: 'Missing required fields: number, type, floor, price_per_night, status' });
    }

    // Validate room type against enum
    const validTypes = ['single', 'double', 'suite', 'deluxe'];
    if (!validTypes.includes(type)) {
      return res.status(400).json({ error: 'Invalid room type. Must be one of: ' + validTypes.join(', ') });
    }

    // Validate status against enum
    const validStatuses = ['available', 'occupied', 'dirty'];
    if (!validStatuses.includes(status)) {
      return res.status(400).json({ error: 'Invalid status. Must be one of: ' + validStatuses.join(', ') });
    }

    // Check for duplicate room number (exclude current room)
    const duplicate = await Room.findByNumberExcluding(number, id);
    if (duplicate) {
      return res.status(409).json({ error: 'Room number ' + number + ' already exists' });
    }

    const room = await Room.update(id, { number, type, floor, price_per_night, status });
    if (!room) {
      return res.status(404).json({ error: 'Room not found' });
    }

    res.json(room);
  } catch (err) {
    console.error('Error updating room:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

module.exports = { getAllRooms, createRoom, updateRoomStatus, getAvailableRooms, updateRoom };
