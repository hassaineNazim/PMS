const express = require('express');
const router = express.Router();
const { checkOut } = require('../controllers/checkoutController');

router.post('/:reservationId', checkOut);

module.exports = router;
