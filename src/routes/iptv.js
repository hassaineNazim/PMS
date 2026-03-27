const express = require('express');
const router = express.Router();
const { getGuestInfoByRoom } = require('../controllers/iptvController');

router.get('/room/:roomNumber', getGuestInfoByRoom);

module.exports = router;
