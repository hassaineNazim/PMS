const express = require('express');
const path = require('path');
const morgan = require('morgan');

const { requireAuth } = require('./middleware/auth');
const authRoutes = require('./routes/auth');
const roomRoutes = require('./routes/rooms');
const guestRoutes = require('./routes/guests');
const reservationRoutes = require('./routes/reservations');
const checkinRoutes = require('./routes/checkin');
const checkoutRoutes = require('./routes/checkout');
const iptvRoutes = require('./routes/iptv');
const auditLogRoutes = require('./routes/auditLogs');
const invoiceRoutes = require('./routes/invoices');
const statsRoutes = require('./routes/stats');

const app = express();

// Middleware
app.use(morgan('dev'));
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Serve static files from src/public
app.use(express.static(path.join(__dirname, 'public')));

// Redirect root to dashboard
app.get('/', (req, res) => {
  res.redirect('/index.html');
});

// Health check (public)
app.get('/api/health', (req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

// Auth routes (login is public, me/logout protected by their own logic)
app.use('/api/auth', authRoutes);

// Auth middleware — protects all /api/* routes below this point
app.use('/api', requireAuth);

// Protected routes
app.use('/api/rooms', roomRoutes);
app.use('/api/guests', guestRoutes);
app.use('/api/reservations', reservationRoutes);
app.use('/api/checkin', checkinRoutes);
app.use('/api/checkout', checkoutRoutes);
app.use('/api/iptv', iptvRoutes);
app.use('/api/audit-logs', auditLogRoutes);
app.use('/api/invoices', invoiceRoutes);
app.use('/api/stats', statsRoutes);

module.exports = app;
