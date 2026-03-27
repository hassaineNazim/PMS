const pool = require('../config/db');

const Guest = {
  async findAll() {
    const { rows } = await pool.query('SELECT * FROM guests ORDER BY last_name');
    return rows;
  },

  async findById(id) {
    const { rows } = await pool.query('SELECT * FROM guests WHERE id = $1', [id]);
    return rows[0];
  },

  async create({ first_name, last_name, email, language, phone }) {
    const { rows } = await pool.query(
      `INSERT INTO guests (first_name, last_name, email, language, phone)
       VALUES ($1, $2, $3, $4, $5) RETURNING *`,
      [first_name, last_name, email, language || 'en', phone]
    );
    return rows[0];
  },
};

module.exports = Guest;
