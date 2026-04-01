require('dotenv').config();
const app = require('./app');
const { startCron } = require('./services/cronService');

const PORT = process.env.PORT || 3000;

app.listen(PORT, () => {
  console.log(`PMS server running on http://localhost:${PORT}`);
  startCron();
});
