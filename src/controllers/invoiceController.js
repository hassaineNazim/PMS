const Invoice = require('../models/Invoice');

async function getAll(req, res) {
  try {
    const invoices = await Invoice.findAll();
    return res.json(invoices);
  } catch (err) {
    console.error('Error fetching invoices:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
}

async function getById(req, res) {
  try {
    const { id } = req.params;
    const invoice = await Invoice.findById(id);
    if (!invoice) {
      return res.status(404).json({ error: 'Invoice not found' });
    }
    return res.json(invoice);
  } catch (err) {
    console.error('Error fetching invoice:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
}

module.exports = { getAll, getById };
