const pool = require('../config/db');

const Staff = {
  async findAll(filters = {}) {
    let query = 'SELECT * FROM staff';
    const conditions = [];
    const params = [];
    let idx = 1;

    if (filters.role) {
      conditions.push('role = $' + idx++);
      params.push(filters.role);
    }
    if (filters.status) {
      conditions.push('status = $' + idx++);
      params.push(filters.status);
    }
    if (filters.department) {
      conditions.push('department ILIKE $' + idx++);
      params.push('%' + filters.department + '%');
    }
    if (filters.search) {
      conditions.push('(first_name ILIKE $' + idx + ' OR last_name ILIKE $' + idx + ')');
      params.push('%' + filters.search + '%');
      idx++;
    }

    if (conditions.length > 0) {
      query += ' WHERE ' + conditions.join(' AND ');
    }

    query += ' ORDER BY last_name, first_name';

    const { rows } = await pool.query(query, params);
    return rows;
  },

  async findById(id) {
    const { rows } = await pool.query('SELECT * FROM staff WHERE id = $1', [id]);
    return rows[0];
  },

  async findByEmail(email) {
    const { rows } = await pool.query('SELECT * FROM staff WHERE email = $1', [email]);
    return rows[0];
  },

  async findByEmailExcluding(email, excludeId) {
    const { rows } = await pool.query(
      'SELECT * FROM staff WHERE email = $1 AND id != $2',
      [email, excludeId]
    );
    return rows[0];
  },

  async create({ first_name, last_name, email, phone, role, department, hire_date, status }) {
    const { rows } = await pool.query(
      `INSERT INTO staff (first_name, last_name, email, phone, role, department, hire_date, status)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $8) RETURNING *`,
      [first_name, last_name, email || null, phone || null, role || 'other', department || null, hire_date || new Date(), status || 'active']
    );
    return rows[0];
  },

  async update(id, { first_name, last_name, email, phone, role, department, hire_date, status }) {
    const { rows } = await pool.query(
      `UPDATE staff SET first_name = $1, last_name = $2, email = $3, phone = $4,
       role = $5, department = $6, hire_date = $7, status = $8, updated_at = NOW()
       WHERE id = $9 RETURNING *`,
      [first_name, last_name, email || null, phone || null, role, department || null, hire_date, status, id]
    );
    return rows[0];
  },

  async delete(id) {
    const { rows } = await pool.query('DELETE FROM staff WHERE id = $1 RETURNING *', [id]);
    return rows[0];
  },

  async getSchedules(staffId, dateFrom, dateTo) {
    let query = `SELECT ss.*, s.first_name, s.last_name
                 FROM staff_schedules ss
                 JOIN staff s ON s.id = ss.staff_id
                 WHERE ss.staff_id = $1`;
    const params = [staffId];
    let idx = 2;

    if (dateFrom) {
      query += ' AND ss.date >= $' + idx++;
      params.push(dateFrom);
    }
    if (dateTo) {
      query += ' AND ss.date <= $' + idx++;
      params.push(dateTo);
    }

    query += ' ORDER BY ss.date, ss.shift_start';

    const { rows } = await pool.query(query, params);
    return rows;
  },

  async addSchedule({ staff_id, date, shift_start, shift_end, notes }) {
    const { rows } = await pool.query(
      `INSERT INTO staff_schedules (staff_id, date, shift_start, shift_end, notes)
       VALUES ($1, $2, $3, $4, $5) RETURNING *`,
      [staff_id, date, shift_start, shift_end, notes || null]
    );
    return rows[0];
  },

  async deleteSchedule(id) {
    const { rows } = await pool.query('DELETE FROM staff_schedules WHERE id = $1 RETURNING *', [id]);
    return rows[0];
  },

  async countByStatus() {
    const { rows } = await pool.query(
      `SELECT status, COUNT(*)::int as count FROM staff GROUP BY status`
    );
    return rows;
  },

  async countByRole() {
    const { rows } = await pool.query(
      `SELECT role, COUNT(*)::int as count FROM staff GROUP BY role`
    );
    return rows;
  }
};

module.exports = Staff;
