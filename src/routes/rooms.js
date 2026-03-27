const express = require('express');
const router = express.Router();
const { getAllRooms, createRoom, updateRoomStatus, getAvailableRooms, updateRoom } = require('../controllers/roomController');

router.get('/', getAllRooms);
router.get('/available', getAvailableRooms);
router.post('/', createRoom);
router.put('/:id', updateRoom);
router.patch('/:id/status', updateRoomStatus);

module.exports = router;
