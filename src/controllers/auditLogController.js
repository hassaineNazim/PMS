const AuditLog = require('../models/AuditLog');

async function getAuditLogs(req, res) {
  try {
    const { limit, action } = req.query;
    const logs = await AuditLog.findAllWithDetails({ limit, action });
    res.json(logs);
  } catch (err) {
    console.error('Error fetching audit logs:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

module.exports = { getAuditLogs };
