const pool = require('../config/db');

const AuditLog = {
  async create({ action, entity_type, entity_id, details, success = true, error_message }) {
    const { rows } = await pool.query(
      `INSERT INTO audit_logs (action, entity_type, entity_id, details, success, error_message)
       VALUES ($1, $2, $3, $4, $5, $6) RETURNING *`,
      [action, entity_type, entity_id, details ? JSON.stringify(details) : null, success, error_message]
    );
    return rows[0];
  },

  async findAll({ limit = 50, offset = 0 } = {}) {
    const { rows } = await pool.query(
      'SELECT * FROM audit_logs ORDER BY created_at DESC LIMIT $1 OFFSET $2',
      [limit, offset]
    );
    return rows;
  },

  async findAllWithDetails({ limit = 50, action } = {}) {
    const safeLimit = Math.min(Math.max(parseInt(limit, 10) || 50, 1), 200);

    let query = `
      SELECT a.*,
             g.first_name AS guest_first_name,
             g.last_name AS guest_last_name,
             rm.number AS room_number
      FROM audit_logs a
      LEFT JOIN reservations res ON a.entity_type = 'reservation' AND a.entity_id = res.id
      LEFT JOIN guests g ON res.guest_id = g.id
      LEFT JOIN rooms rm ON res.room_id = rm.id
    `;
    const params = [];

    if (action) {
      params.push(action);
      query += ' WHERE a.action = $' + params.length;
    }

    params.push(safeLimit);
    query += ' ORDER BY a.created_at DESC LIMIT $' + params.length;

    const { rows } = await pool.query(query, params);
    return rows;
  },
};

module.exports = AuditLog;
