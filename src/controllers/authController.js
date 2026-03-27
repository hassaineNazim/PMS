const { createSessionToken, verifySessionToken, parseCookies, COOKIE_NAME, SESSION_MAX_AGE } = require('../middleware/auth');
require('dotenv').config();

const ADMIN_USER = process.env.ADMIN_USER || 'admin';
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD || 'admin123';

/**
 * POST /api/auth/login
 */
async function login(req, res) {
  try {
    const { username, password } = req.body;

    if (!username || !password) {
      return res.status(400).json({ error: 'Username and password are required' });
    }

    if (username !== ADMIN_USER || password !== ADMIN_PASSWORD) {
      return res.status(401).json({ error: 'Invalid credentials' });
    }

    const token = createSessionToken(username);

    res.setHeader('Set-Cookie',
      `${COOKIE_NAME}=${encodeURIComponent(token)}; Path=/; HttpOnly; SameSite=Strict; Max-Age=${Math.floor(SESSION_MAX_AGE / 1000)}`
    );

    res.json({ success: true, user: username });
  } catch (err) {
    console.error('Login error:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

/**
 * POST /api/auth/logout
 */
async function logout(req, res) {
  try {
    res.setHeader('Set-Cookie',
      `${COOKIE_NAME}=; Path=/; HttpOnly; SameSite=Strict; Max-Age=0`
    );
    res.json({ success: true, message: 'Logged out' });
  } catch (err) {
    console.error('Logout error:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

/**
 * GET /api/auth/me
 */
async function me(req, res) {
  try {
    const cookies = parseCookies(req.headers.cookie);
    const token = cookies[COOKIE_NAME];
    const session = verifySessionToken(token);

    if (!session) {
      return res.status(401).json({ error: 'Unauthorized' });
    }

    res.json({ user: session.user });
  } catch (err) {
    console.error('Auth check error:', err);
    res.status(500).json({ error: 'Internal server error' });
  }
}

module.exports = { login, logout, me };
