const pool = require('../config/db');

const Reservation = {
  async findAll() {
    const { rows } = await pool.query(`
      SELECT r.*, g.first_name, g.last_name, g.language, rm.number AS room_number
      FROM reservations r
      JOIN guests g ON r.guest_id = g.id
      JOIN rooms rm ON r.room_id = rm.id
      ORDER BY r.check_in
    `);
    return rows;
  },

  async findById(id) {
    const { rows } = await pool.query(`
      SELECT r.*, g.first_name, g.last_name, g.email, g.language,
             rm.number AS room_number
      FROM reservations r
      JOIN guests g ON r.guest_id = g.id
      JOIN rooms rm ON r.room_id = rm.id
      WHERE r.id = $1
    `, [id]);
    return rows[0];
  },

  async updateStatus(id, status) {
    const { rows } = await pool.query(
      'UPDATE reservations SET status = $1, updated_at = NOW() WHERE id = $2 RETURNING *',
      [status, id]
    );
    return rows[0];
  },

  async setAccompanyingGuests(id, text) {
    const { rows } = await pool.query(
      'UPDATE reservations SET accompanying_guests = $1, updated_at = NOW() WHERE id = $2 RETURNING *',
      [text, id]
    );
    return rows[0];
  },

  async findConflicting(room_id, check_in, check_out, exclude_id = null) {
    let query = `
      SELECT * FROM reservations
      WHERE room_id = $1
        AND status IN ('confirmed', 'checked_in')
        AND check_in < $3
        AND check_out > $2
    `;
    const params = [room_id, check_in, check_out];
    if (exclude_id) {
      params.push(exclude_id);
      query += ` AND id != $4`;
    }
    query += ` LIMIT 1`;
    const { rows } = await pool.query(query, params);
    return rows[0];
  },

  async create({ guest_id, room_id, check_in, check_out }) {
    const { rows } = await pool.query(
      `INSERT INTO reservations (guest_id, room_id, check_in, check_out)
       VALUES ($1, $2, $3, $4) RETURNING *`,
      [guest_id, room_id, check_in, check_out]
    );
    return rows[0];
  },

  async update(id, { guest_id, room_id, check_in, check_out }) {
    const { rows } = await pool.query(
      `UPDATE reservations
       SET guest_id = $1, room_id = $2, check_in = $3, check_out = $4, updated_at = NOW()
       WHERE id = $5 RETURNING *`,
      [guest_id, room_id, check_in, check_out, id]
    );
    return rows[0];
  },

  /** Find the current checked-in reservation for a given room */
  async findCheckedInByRoom(roomId) {
    const { rows } = await pool.query(`
      SELECT r.*, g.first_name, g.last_name, g.email, g.language,
             rm.number AS room_number
      FROM reservations r
      JOIN guests g ON r.guest_id = g.id
      JOIN rooms rm ON r.room_id = rm.id
      WHERE r.room_id = $1 AND r.status = 'checked_in'
      LIMIT 1
    `, [roomId]);
    return rows[0];
  },
};

module.exports = Reservation;
