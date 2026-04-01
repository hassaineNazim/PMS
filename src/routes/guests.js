const express = require('express');
const router = express.Router();
const { getAllGuests, createGuest, quickInvoice } = require('../controllers/guestController');

router.get('/', getAllGuests);
router.post('/', createGuest);
router.post('/:id/quick-invoice', quickInvoice);

module.exports = router;
