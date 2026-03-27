const express = require('express');
const router = express.Router();
const { getAllReservations, createReservation, cancelReservation } = require('../controllers/reservationController');

router.get('/', getAllReservations);
router.post('/', createReservation);
router.post('/:id/cancel', cancelReservation);

module.exports = router;
