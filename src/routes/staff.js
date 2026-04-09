const express = require('express');
const router = express.Router();
const {
  getAllStaff,
  getStaffById,
  createStaff,
  updateStaff,
  deleteStaff,
  getSchedules,
  addSchedule,
  deleteSchedule,
  getStaffStats
} = require('../controllers/staffController');

router.get('/stats', getStaffStats);
router.get('/', getAllStaff);
router.get('/:id', getStaffById);
router.post('/', createStaff);
router.put('/:id', updateStaff);
router.delete('/:id', deleteStaff);
router.get('/:id/schedules', getSchedules);
router.post('/:id/schedules', addSchedule);
router.delete('/schedules/:scheduleId', deleteSchedule);

module.exports = router;
