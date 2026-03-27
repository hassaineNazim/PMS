const { create } = require('xmlbuilder2');
require('dotenv').config();

const PROCENTRIC_URL = process.env.PROCENTRIC_URL || 'http://localhost:8080/procentric/api';

/**
 * LG Pro:Centric integration service.
 * Provides two methods of sending guest data to the IPTV middleware:
 * - JSON/REST (modern approach)
 * - XML/HTNG (hospitality industry standard)
 */

/**
 * Build the guest notification payload.
 */
function buildPayload(reservation) {
  return {
    guest_name: `${reservation.first_name} ${reservation.last_name}`,
    room_number: reservation.room_number,
    check_out_date: reservation.check_out,
    language: reservation.language || 'en',
  };
}

/**
 * Send guest check-in notification via JSON/REST.
 */
async function sendJsonNotification(reservation) {
  const payload = buildPayload(reservation);

  const response = await fetch(`${PROCENTRIC_URL}/checkin`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    throw new Error(`Pro:Centric JSON API error: ${response.status} ${response.statusText}`);
  }

  return { method: 'json', status: response.status, payload };
}

/**
 * Send guest check-in notification via XML (HTNG standard).
 */
async function sendXmlNotification(reservation) {
  const payload = buildPayload(reservation);

  const xml = create({ version: '1.0', encoding: 'UTF-8' })
    .ele('HTNG_CheckInNotification', { xmlns: 'http://htng.org/2014B' })
      .ele('GuestName').txt(payload.guest_name).up()
      .ele('RoomNumber').txt(payload.room_number).up()
      .ele('CheckOutDate').txt(String(payload.check_out_date)).up()
      .ele('Language').txt(payload.language).up()
    .end({ prettyPrint: true });

  const response = await fetch(`${PROCENTRIC_URL}/checkin`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/xml' },
    body: xml,
  });

  if (!response.ok) {
    throw new Error(`Pro:Centric XML API error: ${response.status} ${response.statusText}`);
  }

  return { method: 'xml', status: response.status, payload };
}

/**
 * Try JSON first; fall back to XML if it fails.
 * Returns the result and logs the outcome.
 */
async function notifyCheckIn(reservation) {
  try {
    return await sendJsonNotification(reservation);
  } catch (jsonErr) {
    console.warn(`JSON notification failed (${jsonErr.message}), trying XML...`);
    try {
      return await sendXmlNotification(reservation);
    } catch (xmlErr) {
      throw new Error(
        `Both Pro:Centric notification methods failed. JSON: ${jsonErr.message} | XML: ${xmlErr.message}`
      );
    }
  }
}

/**
 * Send guest check-out notification via JSON/REST.
 */
async function sendJsonCheckOutNotification(reservation) {
  const payload = buildPayload(reservation);

  const response = await fetch(`${PROCENTRIC_URL}/checkout`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    throw new Error(`Pro:Centric JSON API error: ${response.status} ${response.statusText}`);
  }

  return { method: 'json', status: response.status, payload };
}

/**
 * Send guest check-out notification via XML (HTNG standard).
 */
async function sendXmlCheckOutNotification(reservation) {
  const payload = buildPayload(reservation);

  const xml = create({ version: '1.0', encoding: 'UTF-8' })
    .ele('HTNG_CheckOutNotification', { xmlns: 'http://htng.org/2014B' })
      .ele('GuestName').txt(payload.guest_name).up()
      .ele('RoomNumber').txt(payload.room_number).up()
      .ele('CheckOutDate').txt(String(payload.check_out_date)).up()
      .ele('Language').txt(payload.language).up()
    .end({ prettyPrint: true });

  const response = await fetch(`${PROCENTRIC_URL}/checkout`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/xml' },
    body: xml,
  });

  if (!response.ok) {
    throw new Error(`Pro:Centric XML API error: ${response.status} ${response.statusText}`);
  }

  return { method: 'xml', status: response.status, payload };
}

/**
 * Try JSON first; fall back to XML if it fails (check-out).
 */
async function notifyCheckOut(reservation) {
  try {
    return await sendJsonCheckOutNotification(reservation);
  } catch (jsonErr) {
    console.warn(`JSON checkout notification failed (${jsonErr.message}), trying XML...`);
    try {
      return await sendXmlCheckOutNotification(reservation);
    } catch (xmlErr) {
      throw new Error(
        `Both Pro:Centric checkout notification methods failed. JSON: ${jsonErr.message} | XML: ${xmlErr.message}`
      );
    }
  }
}

module.exports = {
  notifyCheckIn,
  notifyCheckOut,
  sendJsonNotification,
  sendXmlNotification,
  sendJsonCheckOutNotification,
  sendXmlCheckOutNotification,
  buildPayload,
};
