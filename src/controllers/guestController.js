const Guest = require('../models/Guest');

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

module.exports = { getAllGuests, createGuest };
