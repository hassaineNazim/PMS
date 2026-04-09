const Staff = require('../models/Staff');

const validRoles = ['manager', 'receptionist', 'housekeeper', 'maintenance', 'security', 'other'];
const validStatuses = ['active', 'inactive', 'on_leave'];

async function getAllStaff(req, res) {
  try {
    const filters = {};
    if (req.query.role) filters.role = req.query.role;
    if (req.query.status) filters.status = req.query.status;
    if (req.query.department) filters.department = req.query.department;
    if (req.query.search) filters.search = req.query.search;

    const staff = await Staff.findAll(filters);
    res.json(staff);
  } catch (err) {
    console.error('Error fetching staff:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function getStaffById(req, res) {
  try {
    const staff = await Staff.findById(req.params.id);
    if (!staff) return res.status(404).json({ error: 'Staff member not found' });
    res.json(staff);
  } catch (err) {
    console.error('Error fetching staff member:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function createStaff(req, res) {
  try {
    const { first_name, last_name, email, phone, role, department, hire_date, status } = req.body;

    if (!first_name || !last_name) {
      return res.status(400).json({ error: 'Missing required fields: first_name, last_name' });
    }

    if (role && !validRoles.includes(role)) {
      return res.status(400).json({ error: 'Invalid role. Must be one of: ' + validRoles.join(', ') });
    }

    if (status && !validStatuses.includes(status)) {
      return res.status(400).json({ error: 'Invalid status. Must be one of: ' + validStatuses.join(', ') });
    }

    if (email) {
      const existing = await Staff.findByEmail(email);
      if (existing) {
        return res.status(409).json({ error: 'A staff member with this email already exists' });
      }
    }

    const staff = await Staff.create({ first_name, last_name, email, phone, role, department, hire_date, status });
    res.status(201).json(staff);
  } catch (err) {
    if (err.code === '23505' && err.constraint) {
      return res.status(409).json({ error: 'A staff member with this email already exists' });
    }
    console.error('Error creating staff member:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function updateStaff(req, res) {
  try {
    const { id } = req.params;
    const { first_name, last_name, email, phone, role, department, hire_date, status } = req.body;

    if (!first_name || !last_name) {
      return res.status(400).json({ error: 'Missing required fields: first_name, last_name' });
    }

    if (role && !validRoles.includes(role)) {
      return res.status(400).json({ error: 'Invalid role. Must be one of: ' + validRoles.join(', ') });
    }

    if (status && !validStatuses.includes(status)) {
      return res.status(400).json({ error: 'Invalid status. Must be one of: ' + validStatuses.join(', ') });
    }

    if (email) {
      const duplicate = await Staff.findByEmailExcluding(email, id);
      if (duplicate) {
        return res.status(409).json({ error: 'A staff member with this email already exists' });
      }
    }

    const staff = await Staff.update(id, { first_name, last_name, email, phone, role, department, hire_date, status });
    if (!staff) {
      return res.status(404).json({ error: 'Staff member not found' });
    }

    res.json(staff);
  } catch (err) {
    if (err.code === '23505' && err.constraint) {
      return res.status(409).json({ error: 'A staff member with this email already exists' });
    }
    console.error('Error updating staff member:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function deleteStaff(req, res) {
  try {
    const staff = await Staff.delete(req.params.id);
    if (!staff) return res.status(404).json({ error: 'Staff member not found' });
    res.json({ success: true, deleted: staff });
  } catch (err) {
    console.error('Error deleting staff member:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function getSchedules(req, res) {
  try {
    const staffId = req.params.id;
    const { from, to } = req.query;

    const staff = await Staff.findById(staffId);
    if (!staff) return res.status(404).json({ error: 'Staff member not found' });

    const schedules = await Staff.getSchedules(staffId, from, to);
    res.json(schedules);
  } catch (err) {
    console.error('Error fetching schedules:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function addSchedule(req, res) {
  try {
    const staff_id = req.params.id;
    const { date, shift_start, shift_end, notes } = req.body;

    if (!date || !shift_start || !shift_end) {
      return res.status(400).json({ error: 'Missing required fields: date, shift_start, shift_end' });
    }

    const staff = await Staff.findById(staff_id);
    if (!staff) return res.status(404).json({ error: 'Staff member not found' });

    const schedule = await Staff.addSchedule({ staff_id, date, shift_start, shift_end, notes });
    res.status(201).json(schedule);
  } catch (err) {
    console.error('Error adding schedule:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function deleteSchedule(req, res) {
  try {
    const schedule = await Staff.deleteSchedule(req.params.scheduleId);
    if (!schedule) return res.status(404).json({ error: 'Schedule not found' });
    res.json({ success: true, deleted: schedule });
  } catch (err) {
    console.error('Error deleting schedule:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

async function getStaffStats(req, res) {
  try {
    const [byStatus, byRole] = await Promise.all([
      Staff.countByStatus(),
      Staff.countByRole()
    ]);

    const total = byStatus.reduce(function(sum, s) { return sum + s.count; }, 0);
    const active = byStatus.find(function(s) { return s.status === 'active'; });
    const onLeave = byStatus.find(function(s) { return s.status === 'on_leave'; });
    const inactive = byStatus.find(function(s) { return s.status === 'inactive'; });

    res.json({
      total: total,
      active: active ? active.count : 0,
      on_leave: onLeave ? onLeave.count : 0,
      inactive: inactive ? inactive.count : 0,
      by_role: byRole
    });
  } catch (err) {
    console.error('Error fetching staff stats:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

module.exports = {
  getAllStaff,
  getStaffById,
  createStaff,
  updateStaff,
  deleteStaff,
  getSchedules,
  addSchedule,
  deleteSchedule,
  getStaffStats
};
