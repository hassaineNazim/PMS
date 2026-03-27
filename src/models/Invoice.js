const pool = require('../config/db');

const Invoice = {
  async create({ reservation_id, guest_id, room_id, check_in, check_out, nights, price_per_night, tax_rate = 10.00 }) {
    const subtotal = parseFloat((nights * price_per_night).toFixed(2));
    const tax_amount = parseFloat((subtotal * tax_rate / 100).toFixed(2));
    const total = parseFloat((subtotal + tax_amount).toFixed(2));

    const { rows } = await pool.query(
      `INSERT INTO invoices (reservation_id, guest_id, room_id, check_in, check_out, nights, price_per_night, subtotal, tax_rate, tax_amount, total)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11) RETURNING *`,
      [reservation_id, guest_id, room_id, check_in, check_out, nights, price_per_night, subtotal, tax_rate, tax_amount, total]
    );
    return rows[0];
  },

  async findAll() {
    const { rows } = await pool.query(`
      SELECT i.*,
             g.first_name, g.last_name, g.email, g.language,
             rm.number AS room_number, rm.type AS room_type, rm.floor AS room_floor
      FROM invoices i
      JOIN guests g ON i.guest_id = g.id
      JOIN rooms rm ON i.room_id = rm.id
      ORDER BY i.created_at DESC
    `);
    return rows;
  },

  async findById(id) {
    const { rows } = await pool.query(`
      SELECT i.*,
             g.first_name, g.last_name, g.email, g.language,
             rm.number AS room_number, rm.type AS room_type, rm.floor AS room_floor
      FROM invoices i
      JOIN guests g ON i.guest_id = g.id
      JOIN rooms rm ON i.room_id = rm.id
      WHERE i.id = $1
    `, [id]);
    return rows[0];
  },
};

module.exports = Invoice;
