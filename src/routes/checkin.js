const express = require('express');
const router = express.Router();
const { checkIn } = require('../controllers/checkinController');

router.post('/:reservationId', checkIn);

module.exports = router;
