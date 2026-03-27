const pool = require('../config/db');

async function getStats(req, res) {
  try {
    // Run all queries in parallel
    const [roomsResult, reservationsResult, revenueResult, guestsResult, chartsResult] = await Promise.all([
      getRoomStats(),
      getReservationStats(),
      getRevenueStats(),
      getGuestStats(),
      getChartData(),
    ]);

    res.json({
      success: true,
      data: {
        rooms: roomsResult,
        reservations: reservationsResult,
        revenue: revenueResult,
        guests: guestsResult,
        charts: chartsResult,
      },
    });
  } catch (err) {
    console.error('Error fetching stats:', err);
    res.status(500).json({ success: false, error: 'Failed to fetch statistics' });
  }
}

async function getRoomStats() {
  const { rows } = await pool.query(`
    SELECT
      COUNT(*)::int AS total,
      COUNT(*) FILTER (WHERE status = 'available')::int AS available,
      COUNT(*) FILTER (WHERE status = 'occupied')::int AS occupied,
      COUNT(*) FILTER (WHERE status = 'dirty')::int AS dirty
    FROM rooms
  `);
  const r = rows[0];
  const total = r.total || 0;
  return {
    total: r.total,
    available: r.available,
    occupied: r.occupied,
    dirty: r.dirty,
    occupancy_rate: total > 0 ? Math.round((r.occupied / total) * 1000) / 10 : 0,
    availability_rate: total > 0 ? Math.round((r.available / total) * 1000) / 10 : 0,
  };
}

async function getReservationStats() {
  const { rows } = await pool.query(`
    SELECT
      COUNT(*)::int AS total,
      COUNT(*) FILTER (WHERE status = 'confirmed')::int AS confirmed,
      COUNT(*) FILTER (WHERE status = 'checked_in')::int AS checked_in,
      COUNT(*) FILTER (WHERE status = 'checked_out')::int AS checked_out,
      COUNT(*) FILTER (WHERE status = 'cancelled')::int AS cancelled,
      COUNT(*) FILTER (
        WHERE created_at >= DATE_TRUNC('week', CURRENT_DATE)
      )::int AS this_week,
      COUNT(*) FILTER (
        WHERE created_at >= DATE_TRUNC('week', CURRENT_DATE) - INTERVAL '7 days'
          AND created_at < DATE_TRUNC('week', CURRENT_DATE)
      )::int AS last_week
    FROM reservations
  `);
  const r = rows[0];
  return {
    total: r.total,
    confirmed: r.confirmed,
    checked_in: r.checked_in,
    checked_out: r.checked_out,
    cancelled: r.cancelled,
    this_week: r.this_week,
    last_week: r.last_week,
    week_growth: calcGrowth(r.this_week, r.last_week),
  };
}

async function getRevenueStats() {
  const { rows } = await pool.query(`
    SELECT
      COALESCE(SUM(total), 0)::numeric AS total_revenue,
      COALESCE(SUM(total) FILTER (
        WHERE created_at >= DATE_TRUNC('week', CURRENT_DATE)
      ), 0)::numeric AS this_week,
      COALESCE(SUM(total) FILTER (
        WHERE created_at >= DATE_TRUNC('week', CURRENT_DATE) - INTERVAL '7 days'
          AND created_at < DATE_TRUNC('week', CURRENT_DATE)
      ), 0)::numeric AS last_week,
      COALESCE(SUM(total) FILTER (
        WHERE created_at >= DATE_TRUNC('month', CURRENT_DATE)
      ), 0)::numeric AS this_month,
      COALESCE(SUM(total) FILTER (
        WHERE created_at >= DATE_TRUNC('month', CURRENT_DATE) - INTERVAL '1 month'
          AND created_at < DATE_TRUNC('month', CURRENT_DATE)
      ), 0)::numeric AS last_month,
      COALESCE(AVG(total), 0)::numeric AS average_invoice,
      COUNT(*)::int AS invoice_count
    FROM invoices
  `);
  const r = rows[0];
  return {
    total: parseFloat(parseFloat(r.total_revenue).toFixed(2)),
    this_week: parseFloat(parseFloat(r.this_week).toFixed(2)),
    last_week: parseFloat(parseFloat(r.last_week).toFixed(2)),
    week_growth: calcGrowth(parseFloat(r.this_week), parseFloat(r.last_week)),
    this_month: parseFloat(parseFloat(r.this_month).toFixed(2)),
    last_month: parseFloat(parseFloat(r.last_month).toFixed(2)),
    month_growth: calcGrowth(parseFloat(r.this_month), parseFloat(r.last_month)),
    average_invoice: parseFloat(parseFloat(r.average_invoice).toFixed(2)),
  };
}

async function getGuestStats() {
  const { rows } = await pool.query(`
    SELECT
      COUNT(*)::int AS total,
      COUNT(*) FILTER (
        WHERE created_at >= DATE_TRUNC('week', CURRENT_DATE)
      )::int AS this_week,
      COUNT(*) FILTER (
        WHERE created_at >= DATE_TRUNC('week', CURRENT_DATE) - INTERVAL '7 days'
          AND created_at < DATE_TRUNC('week', CURRENT_DATE)
      )::int AS last_week
    FROM guests
  `);
  const r = rows[0];
  return {
    total: r.total,
    this_week: r.this_week,
    last_week: r.last_week,
    week_growth: calcGrowth(r.this_week, r.last_week),
  };
}

async function getChartData() {
  const [resByDay, revByDay, occByDay, roomsByStatus, resByStatus] = await Promise.all([
    getReservationsByDay(),
    getRevenueByDay(),
    getOccupancyByDay(),
    getRoomsByStatus(),
    getReservationsByStatus(),
  ]);
  return {
    reservations_by_day: resByDay,
    revenue_by_day: revByDay,
    occupancy_by_day: occByDay,
    rooms_by_status: roomsByStatus,
    reservations_by_status: resByStatus,
  };
}

async function getReservationsByDay() {
  const { rows } = await pool.query(`
    SELECT
      TO_CHAR(d.day, 'Mon DD') AS date,
      COALESCE(c.cnt, 0)::int AS count
    FROM generate_series(
      CURRENT_DATE - INTERVAL '13 days',
      CURRENT_DATE,
      '1 day'
    ) AS d(day)
    LEFT JOIN (
      SELECT DATE(created_at) AS day, COUNT(*)::int AS cnt
      FROM reservations
      WHERE created_at >= CURRENT_DATE - INTERVAL '13 days'
      GROUP BY DATE(created_at)
    ) c ON c.day = d.day
    ORDER BY d.day
  `);
  return rows;
}

async function getRevenueByDay() {
  const { rows } = await pool.query(`
    SELECT
      TO_CHAR(d.day, 'Mon DD') AS date,
      COALESCE(r.total, 0)::numeric AS total
    FROM generate_series(
      CURRENT_DATE - INTERVAL '13 days',
      CURRENT_DATE,
      '1 day'
    ) AS d(day)
    LEFT JOIN (
      SELECT DATE(created_at) AS day, SUM(total)::numeric AS total
      FROM invoices
      WHERE created_at >= CURRENT_DATE - INTERVAL '13 days'
      GROUP BY DATE(created_at)
    ) r ON r.day = d.day
    ORDER BY d.day
  `);
  return rows.map(r => ({ date: r.date, total: parseFloat(parseFloat(r.total).toFixed(2)) }));
}

async function getOccupancyByDay() {
  // Approximate: for each of the last 14 days, count reservations that were checked_in
  // (check_in <= day AND (check_out > day OR status = 'checked_in'))
  // as a % of total rooms
  const { rows } = await pool.query(`
    WITH total_rooms AS (
      SELECT COUNT(*)::int AS cnt FROM rooms
    ),
    days AS (
      SELECT d::date AS day
      FROM generate_series(
        CURRENT_DATE - INTERVAL '13 days',
        CURRENT_DATE,
        '1 day'
      ) d
    )
    SELECT
      TO_CHAR(days.day, 'Mon DD') AS date,
      CASE WHEN tr.cnt = 0 THEN 0
        ELSE ROUND(
          (COUNT(res.id)::numeric / tr.cnt) * 100, 1
        )
      END::numeric AS rate
    FROM days
    CROSS JOIN total_rooms tr
    LEFT JOIN reservations res
      ON res.check_in <= days.day
      AND res.check_out > days.day
      AND res.status IN ('checked_in', 'checked_out')
    GROUP BY days.day, tr.cnt
    ORDER BY days.day
  `);
  return rows.map(r => ({ date: r.date, rate: parseFloat(r.rate) }));
}

async function getRoomsByStatus() {
  const { rows } = await pool.query(`
    SELECT
      COUNT(*) FILTER (WHERE status = 'available')::int AS available,
      COUNT(*) FILTER (WHERE status = 'occupied')::int AS occupied,
      COUNT(*) FILTER (WHERE status = 'dirty')::int AS dirty
    FROM rooms
  `);
  return rows[0];
}

async function getReservationsByStatus() {
  const { rows } = await pool.query(`
    SELECT
      COUNT(*) FILTER (WHERE status = 'confirmed')::int AS confirmed,
      COUNT(*) FILTER (WHERE status = 'checked_in')::int AS checked_in,
      COUNT(*) FILTER (WHERE status = 'checked_out')::int AS checked_out,
      COUNT(*) FILTER (WHERE status = 'cancelled')::int AS cancelled
    FROM reservations
  `);
  return rows[0];
}

function calcGrowth(current, previous) {
  if (previous === 0) return 0;
  return Math.round(((current - previous) / previous) * 1000) / 10;
}

module.exports = { getStats };
