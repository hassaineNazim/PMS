const crypto = require('crypto');
require('dotenv').config();

const SESSION_SECRET = process.env.SESSION_SECRET || 'changeme_secret';
const COOKIE_NAME = 'pms_session';
const SESSION_MAX_AGE = 24 * 60 * 60 * 1000; // 24 hours

/**
 * Create a signed session token using HMAC-SHA256.
 * Token format: base64(payload).base64(signature)
 */
function createSessionToken(user) {
  const payload = Buffer.from(JSON.stringify({
    user,
    exp: Date.now() + SESSION_MAX_AGE,
  })).toString('base64');

  const signature = crypto
    .createHmac('sha256', SESSION_SECRET)
    .update(payload)
    .digest('base64');

  return `${payload}.${signature}`;
}

/**
 * Verify and decode a session token. Returns the payload or null if invalid.
 */
function verifySessionToken(token) {
  if (!token || typeof token !== 'string') return null;

  const parts = token.split('.');
  if (parts.length !== 2) return null;

  const [payload, signature] = parts;

  const expectedSignature = crypto
    .createHmac('sha256', SESSION_SECRET)
    .update(payload)
    .digest('base64');

  // Timing-safe comparison
  if (signature.length !== expectedSignature.length) return null;
  const sigBuf = Buffer.from(signature);
  const expBuf = Buffer.from(expectedSignature);
  if (!crypto.timingSafeEqual(sigBuf, expBuf)) return null;

  try {
    const data = JSON.parse(Buffer.from(payload, 'base64').toString());
    if (data.exp < Date.now()) return null; // expired
    return data;
  } catch {
    return null;
  }
}

/**
 * Parse cookies from the Cookie header string.
 */
function parseCookies(cookieHeader) {
  const cookies = {};
  if (!cookieHeader) return cookies;
  cookieHeader.split(';').forEach(function (pair) {
    const idx = pair.indexOf('=');
    if (idx < 0) return;
    const key = pair.substring(0, idx).trim();
    const val = pair.substring(idx + 1).trim();
    cookies[key] = decodeURIComponent(val);
  });
  return cookies;
}

/**
 * Auth middleware — protects all /api/* routes except /api/auth/login and /api/health.
 */
function requireAuth(req, res, next) {
  // Allow public routes
  if (req.path === '/api/auth/login' || req.path === '/api/health') {
    return next();
  }

  const cookies = parseCookies(req.headers.cookie);
  const token = cookies[COOKIE_NAME];
  const session = verifySessionToken(token);

  if (!session) {
    return res.status(401).json({ error: 'Unauthorized' });
  }

  req.user = session.user;
  next();
}

module.exports = {
  createSessionToken,
  verifySessionToken,
  parseCookies,
  requireAuth,
  COOKIE_NAME,
  SESSION_MAX_AGE,
};
