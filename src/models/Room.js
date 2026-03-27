const pool = require('../config/db');

const Room = {
  async findAll() {
    const { rows } = await pool.query('SELECT * FROM rooms ORDER BY number');
    return rows;
  },

  async findById(id) {
    const { rows } = await pool.query('SELECT * FROM rooms WHERE id = $1', [id]);
    return rows[0];
  },

  async findByNumber(number) {
    const { rows } = await pool.query('SELECT * FROM rooms WHERE number = $1', [number]);
    return rows[0];
  },

  async updateStatus(id, status) {
    const { rows } = await pool.query(
      'UPDATE rooms SET status = $1, updated_at = NOW() WHERE id = $2 RETURNING *',
      [status, id]
    );
    return rows[0];
  },

  async findAvailable() {
    const { rows } = await pool.query(
      "SELECT * FROM rooms WHERE status = 'available' ORDER BY number"
    );
    return rows;
  },

  async create({ number, type, floor, price_per_night }) {
    const { rows } = await pool.query(
      'INSERT INTO rooms (number, type, floor, price_per_night) VALUES ($1, $2, $3, $4) RETURNING *',
      [number, type, floor, price_per_night || 0]
    );
    return rows[0];
  },

  async update(id, { number, type, floor, price_per_night, status }) {
    const { rows } = await pool.query(
      'UPDATE rooms SET number = $1, type = $2, floor = $3, price_per_night = $4, status = $5, updated_at = NOW() WHERE id = $6 RETURNING *',
      [number, type, floor, price_per_night, status, id]
    );
    return rows[0];
  },

  async findByNumberExcluding(number, excludeId) {
    const { rows } = await pool.query(
      'SELECT * FROM rooms WHERE number = $1 AND id != $2',
      [number, excludeId]
    );
    return rows[0];
  },
};

module.exports = Room;
