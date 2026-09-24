// Optional override: $env:DEALER_API_URL = 'https://localhost:7190'
module.exports = {
  '/api/**': {
    target: process.env.DEALER_API_URL || 'http://localhost:5296',
    secure: false, // Local ASP.NET development certificate only; production uses a reverse proxy.
    changeOrigin: true,
  },
};
