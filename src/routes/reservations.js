const express = require('express');
const router = express.Router();
const { getAllReservations, createReservation, cancelReservation, modifyReservation } = require('../controllers/reservationController');

router.get('/', getAllReservations);
router.post('/', createReservation);
router.post('/:id/cancel', cancelReservation);
router.put('/:id', modifyReservation);

module.exports = router;
